using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Users.Commands;

public record PurgeDeletedUsersCommand;

public static class PurgeDeletedUsersHandler
{
    public static async Task Handle(
        PurgeDeletedUsersCommand command,
        UserManager<User> userManager,
        IDiaryEntryRepository diaryEntryRepository,
        IEntryImageRepository imageRepository,
        IDiaryEntryQueries diaryEntryQueries,
        IEntryImageQueries imageQueries,
        CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-2);

        var users = userManager.Users;
        var usersToDelete = await users
            .Where(u => u.IsDeleted && u.DeleteRequestedAt <= cutoffDate)
            .ToListAsync(cancellationToken);

        foreach (var user in usersToDelete)
        {
            var userEntries = await diaryEntryQueries.GetAllByUserId(user.Id, cancellationToken);

            foreach (var entry in userEntries)
            {
                var entryImages = await imageQueries.GetByEntryId(entry.Id, cancellationToken);
               
                await entryImages.IfSomeAsync(async img => 
                    await imageRepository.Delete(img, cancellationToken));

                await diaryEntryRepository.Delete(entry, cancellationToken);
            }

            await userManager.DeleteAsync(user);
        }
    }
}