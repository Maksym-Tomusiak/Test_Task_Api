using Domain.RefreshTokens;
using LanguageExt;

namespace DAL.Repositories.Interfaces.Queries;

public interface IRefreshTokenQueries
{
    Task<Option<RefreshToken>> GetByUserId(Guid userId, CancellationToken cancellationToken);
    Task<Option<RefreshToken>> GetByValue(string token, CancellationToken cancellationToken);
}