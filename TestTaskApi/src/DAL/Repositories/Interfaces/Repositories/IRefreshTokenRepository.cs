using Domain.RefreshTokens;

namespace DAL.Repositories.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken> Add(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task<RefreshToken> Delete(RefreshToken refreshToken, CancellationToken cancellationToken);
}