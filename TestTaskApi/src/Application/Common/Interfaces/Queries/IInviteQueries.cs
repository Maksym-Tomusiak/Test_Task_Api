using Application.Common.Models;
using Domain.Invites;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IInviteQueries
{
    Task<Option<Invite>> GetByCode(Guid code, CancellationToken cancellationToken);

    Task<Option<Invite>> GetByTargetEmail(string email, CancellationToken cancellationToken);

    Task<PaginatedResult<Invite>> GetAllPaginated(
        PaginationParameters paginationParameters, 
        CancellationToken cancellationToken = default);
}