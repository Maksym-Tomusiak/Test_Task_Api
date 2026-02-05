using Application.Common.Interfaces.Queries;
using Application.Common.Models;
using Domain.Invites;

namespace Application.Invites.Queries;

public record GetAllInvitesQuery(
    int PageNumber = 1,
    int PageSize = 10);

public static class GetAllInvitesQueryHandler
{
    public static async Task<PaginatedResult<Invite>> Handle(
        GetAllInvitesQuery query,
        IInviteQueries inviteQueries,
        CancellationToken cancellationToken)
    {
        var paginationParams = new PaginationParameters(
            query.PageNumber,
            query.PageSize,
            null,
            null,
            false);

        return await inviteQueries.GetAllPaginated(paginationParams, cancellationToken);
    }
}
