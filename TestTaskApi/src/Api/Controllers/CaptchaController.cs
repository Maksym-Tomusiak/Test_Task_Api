using Api.Dtos;
using Application.Captcha.Commands;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Api.Controllers;

[ApiController]
public class CaptchaController(IMessageBus messageBus) : ControllerBase
{
    [HttpGet("api/captcha")]
    public async Task<IResult> GetCaptcha(CancellationToken cancellationToken)
    {
        var cmd = new GenerateCaptchaCommand();
        var result = await messageBus.InvokeAsync<GenerateCaptchaResult>(cmd, cancellationToken);

        return Results.Ok(new CaptchaDto(result.CaptchaId, result.CaptchaImageBase64));
    }
}
