using DAL.Repositories.Models;
using Domain.Invites;
using LanguageExt;

namespace DAL.Repositories.Interfaces.Queries;

public interface IInviteQueries
{
    Task<Option<Invite>> GetByCode(Guid code, CancellationToken cancellationToken);

    Task<Option<Invite>> GetByTargetEmail(string email, CancellationToken cancellationToken);

    Task<PaginatedResult<Invite>> GetAllPaginated(
        PaginationParameters paginationParameters, 
        CancellationToken cancellationToken = default);
}