using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Users.Exceptions;
using Domain.Users;
using LanguageExt;
using LanguageExt.SomeHelp;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Users.Commands;

public record RegisterUserCommand(
    Guid InviteCode,
    string Email,
    string Password,
    string Username,
    string CaptchaId,
    string CaptchaCode);

public static class RegisterUserCommandHandler
{
    const string UserRoleName = "User";

    public static async Task<Either<UserException, User>> Handle(
        RegisterUserCommand command,
        IMemoryCache cache,
        UserManager<User> userManager,
        IInviteQueries inviteQueries,
        IInviteRepository inviteRepository,
        CancellationToken cancellationToken)
    {
        if (!cache.TryGetValue(command.CaptchaId, out string? correctCode))
        {
            return new CaptchaExpiredException();
        }

        cache.Remove(command.CaptchaId);

        if (!string.Equals(correctCode, command.CaptchaCode, StringComparison.OrdinalIgnoreCase))
        {
            return new CaptchaIncorrectException();
        }

        var inviteOption = await inviteQueries.GetByCode(command.InviteCode, cancellationToken);
        
        var inviteValidationResult = inviteOption.Match<Either<UserException, bool>>(
            invite =>
            {
                if (invite.IsUsed)
                    return new InviteAlreadyUsedException(command.InviteCode);
                
                if (invite.ExpiresAt < DateTime.UtcNow)
                    return new InviteExpiredException(command.InviteCode);

                if (!string.Equals(invite.Email, command.Email, StringComparison.OrdinalIgnoreCase))
                {
                   return new InviteEmailMismatchException();
                }

                return true;
            },
            () => new InviteNotFoundException(command.InviteCode)
        );

        if (inviteValidationResult.IsLeft)
        {
            return inviteValidationResult.LeftToList()[0];
        }


        var existingUser = await userManager.FindByNameAsync(command.Username);
        if (existingUser is not null)
        {
            return new UserWithNameAlreadyExistsException(existingUser.Id);
        }

        var existingEmail = await userManager.FindByEmailAsync(command.Email);
        if (existingEmail is not null)
        {
            return new UserWithEmailAlreadyExistsException(command.Email);
        }

        var user = new User
        {
            UserName = command.Username,
            Email = command.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            return new UserUnknownException(Guid.Empty, new Exception(string.Join("; ", result.Errors.Select(e => e.Description))));
        }

        await userManager.AddToRoleAsync(user, UserRoleName);

        await inviteRepository.Delete(inviteOption.First(), cancellationToken);

        return user;
    }
}