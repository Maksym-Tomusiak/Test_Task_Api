using System.Security.Claims;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Services;
using Application.Users.Exceptions;
using Domain.Roles;
using Domain.Users;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Application.Users.Commands;

public record RefreshUserCommand(string RefreshToken);

public class RefreshUserCommandHandler
{
    public static async Task<Either<UserException, string>> Handle(
        RefreshUserCommand command,
        IJwtProvider jwtProvider,
        UserManager<User> userManager,
        IRefreshTokenQueries refreshTokenQueries,
        CancellationToken cancellationToken)
    {
        var existingToken = await refreshTokenQueries.GetByValue(command.RefreshToken, cancellationToken);
        if (existingToken.IsNone ||
            existingToken.First().Token != command.RefreshToken ||
            existingToken.First().Expires < DateTime.UtcNow)
        {
            return new InvalidCredentialsException();
        }
        
        var user = await userManager.FindByIdAsync(existingToken.First().UserId.ToString());

        var roles = await userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault() ?? "Master";
        var role = new Role { Name = roleName };

        var tokens = jwtProvider.GenerateTokens(user, role);
        
        return tokens.AccessToken;
    }
}
