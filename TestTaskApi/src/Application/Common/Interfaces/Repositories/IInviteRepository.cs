using Domain.Invites;

namespace Application.Common.Interfaces.Repositories;

public interface IInviteRepository
{
    Task<Invite> Add(Invite invite, CancellationToken cancellationToken);
    Task<Invite> Update(Invite invite, CancellationToken cancellationToken);
    Task<Invite> Delete(Invite invite, CancellationToken cancellationToken);
}