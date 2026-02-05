using System.Security.Claims;
using Application.Users.Exceptions;
using Domain.Users;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Application.Users.Commands;

public record RestoreUserCommand();

public static class RestoreUserCommandHandler
{
    public static async Task<Either<UserException, User>> Handle(
        RestoreUserCommand command,
        UserManager<User> userManager,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var sessionUserId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sessionUserId))
        {
            return new UserIdNotFoundException();
        }
        
        var user = await userManager.FindByIdAsync(sessionUserId);
        if (user == null)
        {
            return new UserNotFoundException(Guid.Parse(sessionUserId));
        }
        user.Restore();
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return new UserRestoreFailedException(user.Id, result.Errors.Select(e => e.Description).ToList());
        }
        
        return user;
    }
}