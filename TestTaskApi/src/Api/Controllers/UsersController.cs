using Api.Dtos;
using Api.Modules.Errors;
using Application.Users.Commands;
using Application.Users.Exceptions;
using Application.Users.Queries;
using Domain.Users;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Wolverine;

namespace Api.Controllers;

[ApiController]
public class UsersController(IMessageBus messageBus, IMemoryCache cache) : ControllerBase
{
    [HttpPost("api/users/register")]
    public async Task<IResult> Register(RegisterUserDto request, CancellationToken cancellationToken)
    {
        var cmd = new RegisterUserCommand(
            request.InviteCode,
            request.Email,
            request.Password,
            request.Username,
            request.CaptchaId,
            request.CaptchaCode);
        
        var res = await messageBus.InvokeAsync<Either<UserException, User>>(cmd, cancellationToken);

        return res.Match<IResult>(
            u => Results.Created($"/api/users/{u.Id}", UserDto.FromDomainModel(u)),
            ex => ex.ToIResult());
    }

    [HttpPost("api/users/login")]
    public async Task<IResult> Login(LoginUserDto request, CancellationToken cancellationToken)
    {
        var cmd = new LoginUserCommand(request.Username, request.Password);
        var res = await messageBus.InvokeAsync<Either<UserException, (string AccessToken, string RefreshToken, User User)>>(cmd, cancellationToken);

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

        var cmd = new RefreshUserCommand(refreshToken);
        var res = await messageBus.InvokeAsync<Either<UserException, string>>(cmd, cancellationToken);

        return res.Match<IResult>(
            accessToken => Results.Ok(new { accessToken }),
            ex => ex.ToIResult());
    }

    [Authorize]
    [HttpPost("api/users/restore")]
    public async Task<IResult> Restore(CancellationToken cancellationToken)
    {
        var cmd = new RestoreUserCommand();
        var res = await messageBus.InvokeAsync<Either<UserException, User>>(cmd, cancellationToken);

        var httpContext = HttpContext;
        var userId = httpContext.User.FindFirst("id")?.Value;
        
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
        var httpContext = HttpContext;
        var userId = httpContext.User.FindFirst("id")?.Value;
        
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var cmd = new DeleteUserCommand(Guid.Parse(userId));
        var res = await messageBus.InvokeAsync<Either<UserException, string>>(cmd, cancellationToken);

        return res.Match<IResult>(
            msg => 
            {
                // Invalidate user cache
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
            var query = new GetCurrentUserQuery();
            var res = await messageBus.InvokeAsync<Either<UserException, User>>(query, cancellationToken);

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
