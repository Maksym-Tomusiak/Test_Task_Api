using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Services;
using Application.Users.Exceptions;
using Domain.RefreshTokens;
using Domain.Roles;
using Domain.Users;
using LanguageExt;
using Microsoft.AspNetCore.Identity;

namespace Application.Users.Commands;

public record LoginUserCommand(string Username, string Password);
    
public class LoginUserCommandHandler
{
    private const string UserRoleName = "User";

    public static async Task<Either<UserException, (string AccessToken, string RefreshToken)>> Handle(
        LoginUserCommand command, 
        IJwtProvider jwtProvider,
        UserManager<User> userManager,
        RoleManager<Role> roleManager, 
        IRefreshTokenQueries refreshTokenQueries,
        IRefreshTokenRepository refreshTokenRepository,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(command.Username);
        if (user == null)
            return new InvalidCredentialsException();

        if (!await userManager.CheckPasswordAsync(user, command.Password))
            return new InvalidCredentialsException();

        var roles = await userManager.GetRolesAsync(user);
        
        if (roles.Count == 0)
        {
            var existingUserRole = await roleManager.FindByNameAsync(UserRoleName);
            if (existingUserRole == null)
            {
                await roleManager.CreateAsync(new Role { Name = UserRoleName });
            }
            await userManager.AddToRoleAsync(user, UserRoleName);
        }
        
        var roleName = roles.FirstOrDefault() ?? UserRoleName;
        var role = new Role { Name = roleName };

        var tokens = jwtProvider.GenerateTokens(user, role);

        try
        {
            var existingRefreshToken = await refreshTokenQueries.GetByUserId(user.Id, cancellationToken);
            if (existingRefreshToken.IsSome)
            {
                await refreshTokenRepository.Delete(existingRefreshToken.First(), cancellationToken);
            }
            
            await refreshTokenRepository.Add(RefreshToken
                .New(tokens.RefreshToken, DateTime.UtcNow + TimeSpan.FromDays(1), user.Id), cancellationToken);
        }
        catch (Exception e)
        {
            return new UserUnknownException(user.Id, e);
        }

        return tokens;
    }
}
