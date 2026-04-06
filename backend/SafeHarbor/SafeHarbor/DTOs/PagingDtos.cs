namespace SafeHarbor.DTOs;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record PagingQuery(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    bool Desc = false,
    string? Search = null)
{
    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedPageSize => Math.Clamp(PageSize, 1, 200);
}
