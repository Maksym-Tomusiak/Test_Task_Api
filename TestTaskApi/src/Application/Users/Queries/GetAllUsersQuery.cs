using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Users.Queries;

public record GetAllUsersQuery;

public static class GetAllUsersHandler
{
    public static Task<IEnumerable<(User, IReadOnlyList<string>)>> Handle(GetAllUsersQuery request, UserManager<User> userManager)
    {
        var result = new List<(User, IReadOnlyList<string>)>();
        var users = userManager.Users.ToList();
        users.ForEach(x => result.Add((x, userManager.GetRolesAsync(x).Result.ToList())));
        return Task.FromResult<IEnumerable<(User, IReadOnlyList<string>)>>(result);
    }
}