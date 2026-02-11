using BLL.Modules.Exceptions;
using Microsoft.AspNetCore.Http;

namespace BLL.Modules.Errors;

public static class DiaryEntryExceptionExtensions
{
    public static IResult ToIResult(this DiaryEntryException exception)
    {
        return exception switch
        {
            UnauthorizedDiaryEntryAccessException => Results.Unauthorized(),
            DiaryEntryNotFoundException => Results.NotFound(new { error = exception.Message }),
            DiaryEntryEntryCannotBeDeletedException => Results.BadRequest(new { error = exception.Message }),
            DiaryEntryEntryUnknownException => Results.Problem(exception.Message),
            _ => Results.Problem(exception.Message)
        };
    }
}
