using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using DAL.Repositories.Interfaces.Queries;
using DAL.Repositories.Models;
using Domain.Invites;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;

namespace BLL.Services.CRUD;

public class InviteService(IInviteQueries inviteQueries, IMemoryCache cache) : IInviteService
{
    public async Task<PaginatedResult<Invite>> GetAllPaginatedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var paginationParams = new PaginationParameters(
            pageNumber,
            pageSize,
            null,
            null,
            false);

        return await inviteQueries.GetAllPaginated(paginationParams, cancellationToken);
    }

    public async Task<Option<Invite>> GetByCodeAsync(Guid code, CancellationToken cancellationToken)
    {
        return await inviteQueries.GetByCode(code, cancellationToken);
    }

    public void InvalidateInvitesCache()
    {
        const string invitesResetTokenKey = "Invites_Reset_Token";
        if (cache.TryGetValue(invitesResetTokenKey, out CancellationTokenSource? cts))
        {
            cts?.Cancel();
            cache.Remove(invitesResetTokenKey);
        }
    }
}
