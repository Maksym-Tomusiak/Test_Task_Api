using System.Security.Claims;
using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using BLL.Interfaces.Emails;
using BLL.Models;
using BLL.Modules.Exceptions;
using DAL.Repositories.Interfaces.Queries;
using DAL.Repositories.Interfaces.Repositories;
using Domain.Invites;
using Domain.RefreshTokens;
using Domain.Roles;
using Domain.Users;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace BLL.Services.CRUD;

public class UserService(
    IMemoryCache cache,
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IInviteQueries inviteQueries,
    IInviteRepository inviteRepository,
    IRefreshTokenQueries refreshTokenQueries,
    IRefreshTokenRepository refreshTokenRepository,
    IDiaryEntryQueries diaryEntryQueries,
    IDiaryEntryRepository diaryEntryRepository,
    IEntryImageQueries entryImageQueries,
    IEntryImageRepository entryImageRepository,
    IJwtProvider jwtProvider,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    IBackgroundEmailQueue emailQueue) : IUserService
{
    private const string UserRoleName = "User";
    private const string InvitesResetTokenKey = "Invites_Reset_Token";

    public async Task<Either<UserException, User>> RegisterAsync(
        Guid inviteCode,
        string email,
        string password,
        string username,
        string captchaId,
        string captchaCode,
        CancellationToken cancellationToken)
    {
        if (!cache.TryGetValue(captchaId, out string? correctCode))
        {
            return new CaptchaExpiredException();
        }

        cache.Remove(captchaId);

        if (!string.Equals(correctCode, captchaCode, StringComparison.OrdinalIgnoreCase))
        {
            return new CaptchaIncorrectException();
        }

        var inviteOption = await inviteQueries.GetByCode(inviteCode, cancellationToken);

        var inviteValidationResult = inviteOption.Match<Either<UserException, bool>>(
            invite =>
            {
                if (invite.IsUsed)
                    return new InviteAlreadyUsedException(inviteCode);

                if (invite.ExpiresAt < DateTime.UtcNow)
                    return new InviteExpiredException(inviteCode);

                if (!string.Equals(invite.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    return new InviteEmailMismatchException();
                }

                return true;
            },
            () => new InviteNotFoundException(inviteCode)
        );

        if (inviteValidationResult.IsLeft)
        {
            return inviteValidationResult.LeftToList()[0];
        }

        var existingUser = await userManager.FindByNameAsync(username);
        if (existingUser is not null)
        {
            return new UserWithNameAlreadyExistsException(existingUser.Id);
        }

        var existingEmail = await userManager.FindByEmailAsync(email);
        if (existingEmail is not null)
        {
            return new UserWithEmailAlreadyExistsException(email);
        }

        var user = new User
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return new UserUnknownException(Guid.Empty,
                new Exception(string.Join("; ", result.Errors.Select(e => e.Description))));
        }

        await userManager.AddToRoleAsync(user, UserRoleName);

        await inviteRepository.Delete(inviteOption.First(), cancellationToken);

        // Invalidate invites cache after deleting an invite
        InvalidateInvitesCache();

        return user;
    }

    public async Task<Either<UserException, (string AccessToken, string RefreshToken, User User)>> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user == null)
            return new InvalidCredentialsException();

        if (!await userManager.CheckPasswordAsync(user, password))
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

        return (tokens.AccessToken, tokens.RefreshToken, user);
    }

    public async Task<Either<UserException, string>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var existingToken = await refreshTokenQueries.GetByValue(refreshToken, cancellationToken);
        if (existingToken.IsNone ||
            existingToken.First().Token != refreshToken ||
            existingToken.First().Expires < DateTime.UtcNow)
        {
            return new InvalidCredentialsException();
        }

        var user = await userManager.FindByIdAsync(existingToken.First().UserId.ToString());

        var roles = await userManager.GetRolesAsync(user!);
        var roleName = roles.FirstOrDefault() ?? "Master";
        var role = new Role { Name = roleName };

        var tokens = jwtProvider.GenerateTokens(user!, role);

        return tokens.AccessToken;
    }

    public async Task<Either<UserException, User>> RestoreAsync(CancellationToken cancellationToken)
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

    public async Task<Either<UserException, string>> DeleteAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var sessionUserId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sessionUserId))
        {
            return new UserIdNotFoundException();
        }

        var userToDelete = await userManager.FindByIdAsync(userId.ToString());
        if (userToDelete == null)
        {
            return new UserNotFoundException(userId);
        }

        var sessionUserIsAdmin = httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
        var targetUserIsAdmin = await userManager.IsInRoleAsync(userToDelete, "Admin");

        if (sessionUserIsAdmin && targetUserIsAdmin)
        {
            return new UserUnauthorizedAccessException("Admins cannot delete other admin accounts.");
        }

        if (!sessionUserIsAdmin && sessionUserId != userToDelete.Id.ToString())
        {
            return new UserUnauthorizedAccessException("Only admins can delete other users' accounts.");
        }

        try
        {
            var user = await userManager.FindByIdAsync(sessionUserId);
            if (user != null && user.Id == userToDelete.Id)
            {
                user.Delete();
                await userManager.UpdateAsync(user);
                return
                    "User account deleted successfully. It can be restored not later than 48 hours from this moment.";
            }

            var result = await userManager.DeleteAsync(userToDelete);
            if (!result.Succeeded)
            {
                return new UserUnknownException(userToDelete.Id, new Exception("Failed to delete user account."));
            }

            return "User account deleted successfully.";
        }
        catch (Exception ex)
        {
            return new UserUnknownException(userToDelete.Id, ex);
        }
    }

    public async Task<Either<UserException, User>> GetCurrentUserAsync(CancellationToken cancellationToken)
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

    public async Task<Either<UserException, string>> InviteUserAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var existingInvite = await inviteQueries.GetByTargetEmail(email, cancellationToken);
        if (existingInvite.IsSome)
        {
            return new UserAlreadyInvitedException(email);
        }

        var invite = Invite.New(email, DateTime.Now + TimeSpan.FromDays(7));
        await inviteRepository.Add(invite, cancellationToken);

        var frontendUrl = configuration.GetValue<string>("FrontendUrl") ?? "https://localhost";

        var message = new EmailMessage(
            ToEmail: email,
            Subject: $"Invitation to join test task",
            Body:
            $"""<br/><a href="{frontendUrl}register?invite={invite.Code}" target="_blank">Перейти до реєстрації</a>""",
            IsHtml: true);

        await emailQueue.QueueEmail(message);

        return "User invited successfully.";
    }

    public async Task PurgeDeletedUsersAsync(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-2);

        var usersToDelete = await userManager.Users
            .Where(u => u.IsDeleted && u.DeleteRequestedAt <= cutoffDate)
            .ToListAsync(cancellationToken);

        foreach (var user in usersToDelete)
        {
            var userEntries = await diaryEntryQueries.GetAllByUserId(user.Id, cancellationToken);

            foreach (var entry in userEntries)
            {
                var entryImage = await entryImageQueries.GetByEntryId(entry.Id, cancellationToken);

                await entryImage.IfSomeAsync(async img =>
                    await entryImageRepository.Delete(img, cancellationToken));

                await diaryEntryRepository.Delete(entry, cancellationToken);
            }

            await userManager.DeleteAsync(user);
        }
    }

    private void InvalidateInvitesCache()
    {
        if (cache.TryGetValue(InvitesResetTokenKey, out CancellationTokenSource? cts))
        {
            cts?.Cancel();
            cache.Remove(InvitesResetTokenKey);
        }
    }
}
