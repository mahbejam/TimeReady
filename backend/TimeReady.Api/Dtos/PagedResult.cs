namespace TimeReady.Api.Dtos;

/// <summary>One page of results plus what a client needs to ask for the next one.</summary>
/// <typeparam name="T">Type of the items.</typeparam>
/// <param name="Items">The items on this page.</param>
/// <param name="Page">One-based page number.</param>
/// <param name="PageSize">Requested page size.</param>
/// <param name="TotalCount">Number of items matching the filter.</param>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>Number of pages available for the current filter.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>True when another page can be requested.</summary>
    public bool HasNextPage => (long)Page * PageSize < TotalCount;
}
