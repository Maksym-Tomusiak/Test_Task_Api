using DAL.Repositories.Models;
using Domain.DiaryEntries;
using LanguageExt;

namespace DAL.Repositories.Interfaces.Queries;

public interface IDiaryEntryQueries
{
    Task<Option<DiaryEntry>> GetById(DiaryEntryId id, CancellationToken cancellationToken);

    Task<PaginatedResult<DiaryEntry>> GetByUserPaginated(
        Guid userId,
        PaginationParameters paginationParameters,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<DiaryEntry>> GetByUserAndDateRangePaginated(
        Guid userId,
        DateTime startDate,
        DateTime endDate,
        PaginationParameters paginationParameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiaryEntry>> GetRecentEntries(
        Guid userId,
        int count,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DiaryEntry>> GetAllByUserId(
        Guid userId, 
        CancellationToken cancellationToken);
}