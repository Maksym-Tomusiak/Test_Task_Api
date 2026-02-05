using Domain.Users;

namespace Domain.RefreshTokens;

public class RefreshToken
{
    public RefreshTokenId Id { get; private set; }
    public string Token { get; set; }
    public DateTime Expires { get; set; }
    public bool IsRevoked { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; private set; }
    private RefreshToken(RefreshTokenId id, string token, DateTime expires, Guid userId)
    {
        Id = id;
        Token = token;
        Expires = expires;
        IsRevoked = false;
        UserId = userId;
    }

    public static RefreshToken New(string token, DateTime expires, Guid userId) =>
        new(RefreshTokenId.New(), token, expires, userId);

    public void Revoke(bool isRevoked) => IsRevoked = true;
}
