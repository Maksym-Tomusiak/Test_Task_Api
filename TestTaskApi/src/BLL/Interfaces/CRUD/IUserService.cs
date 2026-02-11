using BLL.Modules.Exceptions;
using Domain.Users;
using LanguageExt;

namespace BLL.Interfaces.CRUD;

public interface IUserService
{
    Task<Either<UserException, User>> RegisterAsync(
        Guid inviteCode,
        string email,
        string password,
        string username,
        string captchaId,
        string captchaCode,
        CancellationToken cancellationToken);

    Task<Either<UserException, (string AccessToken, string RefreshToken, User User)>> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken);

    Task<Either<UserException, string>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<Either<UserException, User>> RestoreAsync(CancellationToken cancellationToken);

    Task<Either<UserException, string>> DeleteAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Either<UserException, User>> GetCurrentUserAsync(CancellationToken cancellationToken);

    Task<Either<UserException, string>> InviteUserAsync(
        string email,
        CancellationToken cancellationToken);

    Task PurgeDeletedUsersAsync(CancellationToken cancellationToken);
}
