using BLL.Dtos;
using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using BLL.Modules.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Controllers;

[ApiController]
public class UsersController(IUserService userService, IMemoryCache cache) : ControllerBase
{
    [HttpPost("api/users/register")]
    public async Task<IResult> Register(RegisterUserDto request, CancellationToken cancellationToken)
    {
        var res = await userService.RegisterAsync(
            request.InviteCode,
            request.Email,
            request.Password,
            request.Username,
            request.CaptchaId,
            request.CaptchaCode,
            cancellationToken);

        return res.Match<IResult>(
            u => Results.Created($"/api/users/{u.Id}", UserDto.FromDomainModel(u)),
            ex => ex.ToIResult());
    }

    [HttpPost("api/users/login")]
    public async Task<IResult> Login(LoginUserDto request, CancellationToken cancellationToken)
    {
        var res = await userService.LoginAsync(request.Username, request.Password, cancellationToken);

        return res.Match<IResult>(
            result =>
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7)
                };

                Response.Cookies.Append("RefreshToken", result.RefreshToken, cookieOptions);
                return Results.Ok(new TokenResponseDto(result.AccessToken, result.RefreshToken, UserDto.FromDomainModel(result.User)));
            },
            ex => ex.ToIResult());
    }

    [HttpPost("api/users/refresh")]
    public async Task<IResult> RefreshToken(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["RefreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Results.Unauthorized();
        }

        var res = await userService.RefreshTokenAsync(refreshToken, cancellationToken);

        return res.Match<IResult>(
            accessToken => Results.Ok(new { accessToken }),
            ex => ex.ToIResult());
    }

    [Authorize]
    [HttpPost("api/users/restore")]
    public async Task<IResult> Restore(CancellationToken cancellationToken)
    {
        var res = await userService.RestoreAsync(cancellationToken);

        var userId = HttpContext.User.FindFirst("id")?.Value;

        if (userId == null)
        {
            return Results.Unauthorized();
        }

        return res.Match<IResult>(
            u =>
            {
                cache.Remove($"user_{userId}");
                return Results.Ok(UserDto.FromDomainModel(u));
            },
            ex => ex.ToIResult());
    }

    [Authorize]
    [HttpDelete("api/users/me")]
    public async Task<IResult> DeleteCurrentUser(CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.FindFirst("id")?.Value;

        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var res = await userService.DeleteAsync(Guid.Parse(userId), cancellationToken);

        return res.Match<IResult>(
            msg =>
            {
                cache.Remove($"user_{userId}");
                return Results.Ok(new { message = msg });
            },
            ex => ex.ToIResult());
    }

    [Authorize]
    [HttpGet("api/users/me")]
    public async Task<IResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.FindFirst("id")?.Value;
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var cacheKey = $"user_{userId}";

        if (!cache.TryGetValue(cacheKey, out UserDto? cachedUser))
        {
            var res = await userService.GetCurrentUserAsync(cancellationToken);

            return res.Match<IResult>(
                u =>
                {
                    var userDto = UserDto.FromDomainModel(u);
                    cache.Set(cacheKey, userDto, TimeSpan.FromMinutes(5));
                    return Results.Ok(userDto);
                },
                ex => ex.ToIResult());
        }

        return Results.Ok(cachedUser);
    }
}
