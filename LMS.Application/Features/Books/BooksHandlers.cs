using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Books;

public sealed class BooksHandlers(IApplicationDbContext db) :
    IRequestHandler<GetBooksQuery, Result<PagedResult<BookDto>>>,
    IRequestHandler<GetBookByIdQuery, Result<BookDto>>,
    IRequestHandler<CreateBookCommand, Result<BookDto>>,
    IRequestHandler<UpdateBookCommand, Result<BookDto>>,
    IRequestHandler<DeleteBookCommand, Result>
{
    public async Task<Result<PagedResult<BookDto>>> Handle(GetBooksQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search);

        // Read-only list — AsNoTracking saves the change-tracker snapshot
        // EF would otherwise create for every row in the page.
        var query = db.Books.AsNoTracking();

        if (page.NormalizedSearch is { } search)
        {
            query = query.Where(b =>
                b.Title.ToLower().Contains(search) ||
                (b.Author != null && b.Author.ToLower().Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(request.Subject))
            query = query.Where(b => b.Subject == request.Subject);
        if (!string.IsNullOrWhiteSpace(request.Level))
            query = query.Where(b => b.Level == request.Level);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(b => b.Title)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(b => new BookDto(b.Id, b.Title, b.Author, b.Subject, b.Level,
                b.Description, b.CoverImageUrl, b.FileUrl, b.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<BookDto>>.Ok(PagedResult<BookDto>.From(items, total, page));
    }

    public async Task<Result<BookDto>> Handle(GetBookByIdQuery request, CancellationToken cancellationToken)
    {
        // Read-only lookup — AsNoTracking is meaningful here because the
        // detail page often gets hit repeatedly while editing other entities.
        var b = await db.Books.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.BookId, cancellationToken);
        if (b is null) return Result<BookDto>.Fail("NOT_FOUND", "Book not found.");
        return Result<BookDto>.Ok(Map(b));
    }

    public async Task<Result<BookDto>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        var book = new Book(request.Title, request.Author, request.Subject, request.Level,
            request.Description, request.CoverImageUrl, request.FileUrl);
        await db.Books.AddAsync(book, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<BookDto>.Ok(Map(book));
    }

    public async Task<Result<BookDto>> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        var book = await db.Books.FirstOrDefaultAsync(x => x.Id == request.BookId, cancellationToken);
        if (book is null) return Result<BookDto>.Fail("NOT_FOUND", "Book not found.");
        book.Update(request.Title, request.Author, request.Subject, request.Level,
            request.Description, request.CoverImageUrl, request.FileUrl);
        await db.SaveChangesAsync(cancellationToken);
        return Result<BookDto>.Ok(Map(book));
    }

    public async Task<Result> Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        var book = await db.Books.FirstOrDefaultAsync(x => x.Id == request.BookId, cancellationToken);
        if (book is null) return Result.Fail("NOT_FOUND", "Book not found.");
        // Refuse if currently linked to an assignment — preserves the audit trail.
        var inUse = await db.AssignmentBooks.AnyAsync(x => x.BookId == book.Id, cancellationToken);
        if (inUse) return Result.Fail("IN_USE", "Book is referenced by an assignment.");
        db.Books.Remove(book);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Book deleted.");
    }

    internal static BookDto Map(Book b) => new(b.Id, b.Title, b.Author, b.Subject, b.Level,
        b.Description, b.CoverImageUrl, b.FileUrl, b.CreatedAt);
}
