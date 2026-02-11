using DAL.DbContext;
using DAL.Repositories.Interfaces.Queries;
using DAL.Repositories.Interfaces.Repositories;
using DAL.Repositories.Models;
using Domain.DiaryEntries;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Implementations;

public class DiaryEntryRepository(ApplicationDbContext context) : IDiaryEntryRepository, IDiaryEntryQueries
{
    public async Task<DiaryEntry> Add(DiaryEntry diaryEntry, CancellationToken cancellationToken)
    {
        await context.DiaryEntries.AddAsync(diaryEntry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return diaryEntry;
    }

    public async Task<DiaryEntry> Update(DiaryEntry diaryEntry, CancellationToken cancellationToken)
    {
        context.DiaryEntries.Update(diaryEntry);
        await context.SaveChangesAsync(cancellationToken);
        return diaryEntry;
    }

    public async Task<DiaryEntry> Delete(DiaryEntry diaryEntry, CancellationToken cancellationToken)
    {
        context.DiaryEntries.Remove(diaryEntry);
        await context.SaveChangesAsync(cancellationToken);
        return diaryEntry;
    }

    public async Task<Option<DiaryEntry>> GetById(DiaryEntryId id, CancellationToken cancellationToken)
    {
        var entity = await context.DiaryEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity == null ? Option<DiaryEntry>.None : Option<DiaryEntry>.Some(entity);
    }

    public async Task<PaginatedResult<DiaryEntry>> GetByUserPaginated(Guid userId, PaginationParameters paginationParameters, CancellationToken cancellationToken = default)
    {
        var query = context.DiaryEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.EntryDate)
            .AsQueryable();

        return await GetPaginatedResult(paginationParameters, query, cancellationToken);
    }

    public async Task<PaginatedResult<DiaryEntry>> GetByUserAndDateRangePaginated(Guid userId, DateTime startDate, DateTime endDate, PaginationParameters paginationParameters, CancellationToken cancellationToken = default)
    {
        var query = context.DiaryEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && 
                        x.EntryDate >= startDate && 
                        x.EntryDate <= endDate)
            .OrderByDescending(x => x.EntryDate)
            .AsQueryable();

        return await GetPaginatedResult(paginationParameters, query, cancellationToken);
    }

    public async Task<IReadOnlyList<DiaryEntry>> GetRecentEntries(Guid userId, int count, CancellationToken cancellationToken)
    {
        return await context.DiaryEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.EntryDate)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DiaryEntry>> GetAllByUserId(Guid userId, CancellationToken cancellationToken)
    {
        return await context.DiaryEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.EntryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByUser(Guid userId, CancellationToken cancellationToken)
    {
        return await context.DiaryEntries
            .CountAsync(x => x.UserId == userId, cancellationToken);
    }

    private static async Task<PaginatedResult<DiaryEntry>> GetPaginatedResult(PaginationParameters paginationParameters, IQueryable<DiaryEntry> query, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(paginationParameters.SortBy))
        {
            query = paginationParameters.SortBy.ToLower() switch
            {
                "createdat" => paginationParameters.SortDescending
                    ? query.OrderByDescending(u => u.EntryDate)
                    : query.OrderBy(u => u.EntryDate),
                _ => query.OrderByDescending(u => u.EntryDate)
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((paginationParameters.PageNumber - 1) * paginationParameters.PageSize)
            .Take(paginationParameters.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)paginationParameters.PageSize);

        return new PaginatedResult<DiaryEntry>(
            items,
            totalCount,
            paginationParameters.PageNumber,
            paginationParameters.PageSize,
            totalPages);
    }
}