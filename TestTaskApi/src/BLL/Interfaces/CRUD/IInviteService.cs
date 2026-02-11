using DAL.Repositories.Models;
using Domain.Invites;
using LanguageExt;

namespace BLL.Interfaces.CRUD;

public interface IInviteService
{
    Task<PaginatedResult<Invite>> GetAllPaginatedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Option<Invite>> GetByCodeAsync(Guid code, CancellationToken cancellationToken);

    void InvalidateInvitesCache();
}
