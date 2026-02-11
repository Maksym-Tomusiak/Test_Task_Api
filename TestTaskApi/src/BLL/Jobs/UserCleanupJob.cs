using BLL.Interfaces;
using BLL.Interfaces.CRUD;

namespace BLL.Jobs;

public class UserCleanupJob(IUserService userService)
{
    public async Task Execute(CancellationToken cancellationToken)
    {
        await userService.PurgeDeletedUsersAsync(cancellationToken);
    }
}