namespace LMS.Application.Common.Models;

/// <summary>
/// Standard query envelope for paginated list endpoints. Clamped server-side
/// so a hostile client can't request <c>pageSize=100000</c>.
/// </summary>
public sealed record PageRequest(int Page = 1, int PageSize = 25, string? Search = null)
{
    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedPageSize => PageSize switch
    {
        < 1 => 25,
        > 100 => 100,
        _ => PageSize,
    };
    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;

    /// <summary>Trim + lowercase the search term for case-insensitive matches.</summary>
    public string? NormalizedSearch =>
        string.IsNullOrWhiteSpace(Search) ? null : Search.Trim().ToLowerInvariant();
}

/// <summary>
/// Standard list response envelope. Wraps the page items with the total row
/// count + the request parameters echoed back so clients can render pagers
/// without duplicating state.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Total,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> From(IReadOnlyCollection<T> items, int total, PageRequest request) =>
        new(items, total, request.NormalizedPage, request.NormalizedPageSize);
}
