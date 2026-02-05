using Api.Dtos;
using Api.Modules.Errors;
using Application.Users.Commands;
using Application.Users.Exceptions;
using Application.Users.Queries;
using Domain.Users;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Api.Controllers;

[ApiController]
public class UsersController(IMessageBus messageBus) : ControllerBase
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
        var res = await messageBus.InvokeAsync<Either<UserException, (string AccessToken, string RefreshToken)>>(cmd, cancellationToken);

        return res.Match<IResult>(
            tokens =>
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7)
                };

                Response.Cookies.Append("RefreshToken", tokens.RefreshToken, cookieOptions);
                return Results.Ok(new TokenResponseDto(tokens.AccessToken, tokens.RefreshToken));
            },
            ex => ex.ToIResult());
    }

    [Authorize]
    [HttpPost("api/users/restore")]
    public async Task<IResult> Restore(CancellationToken cancellationToken)
    {
        var cmd = new RestoreUserCommand();
        var res = await messageBus.InvokeAsync<Either<UserException, User>>(cmd, cancellationToken);

        return res.Match<IResult>(
            u => Results.Ok(UserDto.FromDomainModel(u)),
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
            msg => Results.Ok(new { message = msg }),
            ex => ex.ToIResult());
    }

    [Authorize]
    [HttpGet("api/users/me")]
    public async Task<IResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var query = new GetCurrentUserQuery();
        var res = await messageBus.InvokeAsync<Either<UserException, User>>(query, cancellationToken);

        return res.Match<IResult>(
            u => Results.Ok(UserDto.FromDomainModel(u)),
            ex => ex.ToIResult());
    }
}
