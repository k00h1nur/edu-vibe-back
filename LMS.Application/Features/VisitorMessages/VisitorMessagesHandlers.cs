using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Application.Features.VisitorMessages;

public sealed class CreateVisitorMessageCommandHandler(
    IApplicationDbContext db,
    ITelegramNotifier telegram,
    ILogger<CreateVisitorMessageCommandHandler> logger)
    : IRequestHandler<CreateVisitorMessageCommand, Result<VisitorMessageDto>>
{
    public async Task<Result<VisitorMessageDto>> Handle(
        CreateVisitorMessageCommand request, CancellationToken cancellationToken)
    {
        // Honeypot: if the bot filled the hidden field, pretend success without
        // persisting. Same response shape so the bot gets no signal.
        if (!string.IsNullOrWhiteSpace(request.HoneypotField))
        {
            logger.LogInformation(
                "Honeypot triggered — dropping visitor message from source {Source}.", request.Source);
            return Result<VisitorMessageDto>.Ok(
                new VisitorMessageDto(
                    Guid.Empty, request.Name, request.Phone, request.Email, request.Message,
                    request.Source, request.Course, request.PreferredTime, request.Language,
                    false, null, DateTime.UtcNow),
                "Message received.");
        }

        var entity = new VisitorMessage(
            request.Name, request.Phone, request.Email, request.Message,
            request.Source, request.Course, request.PreferredTime, request.Language);
        await db.VisitorMessages.AddAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // Hand off to the Telegram queue — non-blocking, never throws. The
        // background sender handles retries, backoff and Telegram 429 throttling.
        await telegram.SendAsync(BuildTelegramMessage(entity), CancellationToken.None);

        return Result<VisitorMessageDto>.Ok(Map(entity), "Message received.");
    }

    private static string BuildTelegramMessage(VisitorMessage m)
    {
        var sourceLabel = m.Source switch
        {
            VisitorMessageSource.DemoLesson => "🎓 Demo Lesson Request",
            VisitorMessageSource.MockTest   => "📝 Mock Test Registration",
            VisitorMessageSource.LevelCheck => "🎯 Level Check Request",
            VisitorMessageSource.Contact    => "✉️ Contact Form",
            _                               => "Visitor Message",
        };

        var lines = new List<string>
        {
            $"*{sourceLabel}*",
            $"",
            $"*Name:* {Escape(m.Name)}",
            $"*Phone:* `{Escape(m.Phone)}`",
        };
        if (!string.IsNullOrWhiteSpace(m.Email)) lines.Add($"*Email:* {Escape(m.Email)}");
        if (!string.IsNullOrWhiteSpace(m.Course)) lines.Add($"*Course:* {Escape(m.Course)}");
        if (!string.IsNullOrWhiteSpace(m.PreferredTime)) lines.Add($"*Preferred time:* {Escape(m.PreferredTime)}");
        if (!string.IsNullOrWhiteSpace(m.Language)) lines.Add($"*Lang:* {m.Language}");
        lines.Add("");
        lines.Add(Escape(m.Message));
        return string.Join("\n", lines);
    }

    /// <summary>Escape MarkdownV2 reserved characters per Telegram spec.</summary>
    private static string Escape(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        const string reserved = "_*[]()~`>#+-=|{}.!\\";
        var sb = new System.Text.StringBuilder(text.Length + 8);
        foreach (var c in text)
        {
            if (reserved.IndexOf(c) >= 0) sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }

    internal static VisitorMessageDto Map(VisitorMessage m) => new(
        m.Id, m.Name, m.Phone, m.Email, m.Message, m.Source, m.Course, m.PreferredTime,
        m.Language, m.IsRead, m.ReadAt, m.CreatedAt);
}

public sealed class GetVisitorMessagesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVisitorMessagesQuery, Result<VisitorMessagePage>>
{
    public async Task<Result<VisitorMessagePage>> Handle(
        GetVisitorMessagesQuery request, CancellationToken cancellationToken)
    {
        var query = db.VisitorMessages.AsQueryable();
        if (request.IsRead is { } readFilter) query = query.Where(x => x.IsRead == readFilter);
        if (request.Source is { } source) query = query.Where(x => x.Source == source);

        var total = await query.CountAsync(cancellationToken);
        var unread = await db.VisitorMessages.CountAsync(x => !x.IsRead, cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new VisitorMessageDto(
                m.Id, m.Name, m.Phone, m.Email, m.Message, m.Source, m.Course, m.PreferredTime,
                m.Language, m.IsRead, m.ReadAt, m.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<VisitorMessagePage>.Ok(
            new VisitorMessagePage(items, total, request.Page, request.PageSize, unread));
    }
}

public sealed class MarkVisitorMessageReadCommandHandler(IApplicationDbContext db)
    : IRequestHandler<MarkVisitorMessageReadCommand, Result<VisitorMessageDto>>
{
    public async Task<Result<VisitorMessageDto>> Handle(
        MarkVisitorMessageReadCommand request, CancellationToken cancellationToken)
    {
        var m = await db.VisitorMessages.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (m is null) return Result<VisitorMessageDto>.Fail("NOT_FOUND", "Visitor message not found.");
        if (request.Read) m.MarkRead(); else m.MarkUnread();
        await db.SaveChangesAsync(cancellationToken);
        return Result<VisitorMessageDto>.Ok(CreateVisitorMessageCommandHandler.Map(m));
    }
}

public sealed class GetUnreadVisitorMessageCountQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUnreadVisitorMessageCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(GetUnreadVisitorMessageCountQuery request, CancellationToken cancellationToken)
    {
        var count = await db.VisitorMessages.CountAsync(x => !x.IsRead, cancellationToken);
        return Result<int>.Ok(count);
    }
}
