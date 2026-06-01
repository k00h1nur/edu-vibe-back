using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Books;

public sealed record BookDto(
    Guid Id,
    string Title,
    string? Author,
    string? Subject,
    string? Level,
    string? Description,
    string? CoverImageUrl,
    string? FileUrl,
    DateTime CreatedAt);

public sealed record GetBooksQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    string? Subject = null,
    string? Level = null)
    : IRequest<Result<PagedResult<BookDto>>>;

public sealed record GetBookByIdQuery(Guid BookId) : IRequest<Result<BookDto>>;

public sealed record CreateBookCommand(
    string Title,
    string? Author,
    string? Subject,
    string? Level,
    string? Description,
    string? CoverImageUrl,
    string? FileUrl) : IRequest<Result<BookDto>>;

public sealed record UpdateBookCommand(
    Guid BookId,
    string Title,
    string? Author,
    string? Subject,
    string? Level,
    string? Description,
    string? CoverImageUrl,
    string? FileUrl) : IRequest<Result<BookDto>>;

public sealed record DeleteBookCommand(Guid BookId) : IRequest<Result>;
