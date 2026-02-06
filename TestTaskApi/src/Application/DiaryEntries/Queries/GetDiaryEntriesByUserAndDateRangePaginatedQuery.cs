using Application.Common.Interfaces.Queries;
using Application.Common.Models;
using Domain.DiaryEntries;

namespace Application.DiaryEntries.Queries;

public record GetDiaryEntriesByUserAndDateRangePaginatedQuery(
    Guid UserId,
    DateTime StartDate,
    DateTime EndDate,
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? SortBy = null,
    bool SortDescending = false);

public static class GetDiaryEntriesByUserAndDateRangePaginatedHandler
{
    public static async Task<PaginatedResult<DiaryEntry>> Handle(
        GetDiaryEntriesByUserAndDateRangePaginatedQuery request, 
        IDiaryEntryQueries queries, 
        CancellationToken ct)
    {
        var paginationParams = new PaginationParameters(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending);
        
        return await queries.GetByUserAndDateRangePaginated(
            request.UserId, 
            request.StartDate, 
            request.EndDate, 
            paginationParams, 
            ct);
    }
}
