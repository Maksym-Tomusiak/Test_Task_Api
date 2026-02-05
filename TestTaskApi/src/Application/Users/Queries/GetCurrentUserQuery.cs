using System.Security.Claims;
using Application.Users.Exceptions;
using Domain.Users;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Application.Users.Queries;

public record GetCurrentUserQuery();

public static class GetCurrentUserQueryHandler
{
    public static async Task<Either<UserException, User>> Handle(
        GetCurrentUserQuery query,
        UserManager<User> userManager,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return new UserIdNotFoundException();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new UserNotFoundException(Guid.Parse(userId));
        }

        return user;
    }
}
