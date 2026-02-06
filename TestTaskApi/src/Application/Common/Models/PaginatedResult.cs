namespace Application.Common.Models;

public class PaginatedResult<T>(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize, int totalPages)
{
    public IEnumerable<T> Items { get; } = items;
    public int TotalCount { get; } = totalCount;
    public int PageNumber { get; } = pageNumber;
    public int PageSize { get; } = pageSize;
    public int TotalPages { get; } = totalPages;

    public static PaginatedResult<TResult> MapFrom<TSource, TResult>(PaginatedResult<TSource> source, Func<TSource, TResult> selector)
    {
        return new PaginatedResult<TResult>(
            source.Items.Select(selector),
            source.TotalCount,
            source.PageNumber,
            source.PageSize,
            source.TotalPages
        );
    }
}