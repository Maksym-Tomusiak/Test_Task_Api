using Application.Common.Interfaces.Queries;
using Domain.Invites;
using LanguageExt;

namespace Application.Invites.Queries;

public record GetInviteByCodeQuery(Guid Code);

public static class GetInviteByCodeQueryHandler
{
    public static async Task<Option<Invite>> Handle(
        GetInviteByCodeQuery query,
        IInviteQueries inviteQueries,
        CancellationToken cancellationToken)
    {
        return await inviteQueries.GetByCode(query.Code, cancellationToken);
    }
}
