using DAL.DbContext;
using DAL.Repositories.Interfaces.Queries;
using DAL.Repositories.Interfaces.Repositories;
using DAL.Repositories.Models;
using Domain.Invites;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Implementations;

public class InviteRepository(ApplicationDbContext context) : IInviteRepository, IInviteQueries
{
    public async Task<Invite> Add(Invite invite, CancellationToken cancellationToken)
    {
        await context.Invites.AddAsync(invite, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return invite;
    }

    public async Task<Invite> Update(Invite invite, CancellationToken cancellationToken)
    {
        context.Invites.Update(invite);
        await context.SaveChangesAsync(cancellationToken);
        return invite;
    }

    public async Task<Invite> Delete(Invite invite, CancellationToken cancellationToken)
    {
        context.Invites.Remove(invite);
        await context.SaveChangesAsync(cancellationToken);
        return invite;
    }


    public async Task<Option<Invite>> GetByCode(Guid code, CancellationToken cancellationToken)
    {
        var entity = await context.Invites
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

        return entity == null ? Option<Invite>.None : Option<Invite>.Some(entity);
    }

    public async Task<Option<Invite>> GetByTargetEmail(string email, CancellationToken cancellationToken)
    {
        var entity = await context.Invites
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        return entity == null ? Option<Invite>.None : Option<Invite>.Some(entity);
    }

    public async Task<PaginatedResult<Invite>> GetAllPaginated(PaginationParameters paginationParameters, CancellationToken cancellationToken = default)
    {
        var query = context.Invites
            .AsNoTracking()
            .OrderByDescending(x => x.ExpiresAt)
            .AsQueryable();

        return await GetPaginatedResult(paginationParameters, query, cancellationToken);
    }

    private static async Task<PaginatedResult<Invite>> GetPaginatedResult(PaginationParameters paginationParameters, IQueryable<Invite> query, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(paginationParameters.SearchTerm))
        {
            var searchTerm = paginationParameters.SearchTerm.ToLower();
            query = query.Where(i =>
                i.Email.ToLower().Contains(searchTerm) ||
                i.Code.ToString().ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((paginationParameters.PageNumber - 1) * paginationParameters.PageSize)
            .Take(paginationParameters.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)paginationParameters.PageSize);

        return new PaginatedResult<Invite>(items, totalCount, paginationParameters.PageNumber, paginationParameters.PageSize, totalPages);
    }
}