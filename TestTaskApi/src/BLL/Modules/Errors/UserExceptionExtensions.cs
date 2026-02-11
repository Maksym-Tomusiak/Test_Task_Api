using BLL.Modules.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BLL.Modules.Errors;

public static class UserExceptionExtensions
{
    public static IResult ToIResult(this UserException exception)
    {
        return exception switch
        {
            UserNotFoundException => Results.NotFound(new { error = exception.Message }),
            UserIdNotFoundException => Results.Unauthorized(),
            UserRoleNotFoundException => Results.NotFound(new { error = exception.Message }),
            UserWithNameAlreadyExistsException => Results.Conflict(new { error = exception.Message }),
            InvalidCredentialsException => Results.Unauthorized(),
            UserUnauthorizedAccessException => Results.Forbid(),
            UserAlreadyInvitedException => Results.Conflict(new { error = exception.Message }),
            CaptchaExpiredException => Results.BadRequest(new { error = exception.Message }),
            CaptchaIncorrectException => Results.BadRequest(new { error = exception.Message }),
            InviteNotFoundException => Results.NotFound(new { error = exception.Message }),
            InviteAlreadyUsedException => Results.BadRequest(new { error = exception.Message }),
            InviteExpiredException => Results.BadRequest(new { error = exception.Message }),
            InviteEmailMismatchException => Results.BadRequest(new { error = exception.Message }),
            UserWithEmailAlreadyExistsException => Results.Conflict(new { error = exception.Message }),
            UserRestoreFailedException => Results.BadRequest(new { error = exception.Message }),
            UserUnknownException => Results.Problem(exception.Message),
            _ => Results.Problem(exception.Message)
        };
    }
}
