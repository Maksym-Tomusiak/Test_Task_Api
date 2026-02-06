using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.RefreshTokens;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(ApplicationDbContext context) : IRefreshTokenRepository, IRefreshTokenQueries
{
    public async Task<RefreshToken> Add(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        await context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return refreshToken;
    }
    
    public async Task<RefreshToken> Delete(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        context.RefreshTokens.Remove(refreshToken);
        
        await context.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    public async Task<Option<RefreshToken>> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        var entity = await context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        
        return entity == null ? Option<RefreshToken>.None: Option<RefreshToken>.Some(entity);
    }

    public async Task<Option<RefreshToken>> GetByValue(string token, CancellationToken cancellationToken)
    {
        var entity = await context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
        
        return entity == null ? Option<RefreshToken>.None: Option<RefreshToken>.Some(entity);
    }
}