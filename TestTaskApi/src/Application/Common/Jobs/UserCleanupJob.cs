using Application.Users.Commands;
using Wolverine;

namespace Application.Common.Jobs;

public class UserCleanupJob(IMessageBus bus)
{
    public async Task Execute(CancellationToken cancellationToken)
    {
        await bus.InvokeAsync(new PurgeDeletedUsersCommand(), cancellationToken);
    }
}