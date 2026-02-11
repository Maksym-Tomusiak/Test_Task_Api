using Domain.Roles;
using Domain.Users;

namespace BLL.Interfaces;

public interface IJwtProvider
{
    (string AccessToken, string RefreshToken) GenerateTokens(User user, Role role);
    string GenerateAccessToken(User user, Role role);
}
