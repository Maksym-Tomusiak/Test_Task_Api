using Domain.Users;

namespace BLL.Dtos;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    bool IsDeleted)
{
    public static UserDto FromDomainModel(User user) 
        => new(user.Id, user.UserName ?? "", user.Email ?? "", user.IsDeleted);
}

public record RegisterUserDto(
    Guid InviteCode,
    string Email,
    string Password,
    string Username,
    string CaptchaId,
    string CaptchaCode);

public record LoginUserDto(
    string Username,
    string Password);

public record TokenResponseDto(
    string AccessToken,
    string RefreshToken,
    UserDto User);
