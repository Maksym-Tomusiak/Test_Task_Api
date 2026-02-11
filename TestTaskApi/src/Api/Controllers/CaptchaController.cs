using BLL.Dtos;
using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class CaptchaController(ICaptchaBusinessService captchaBusinessService) : ControllerBase
{
    [HttpGet("api/captcha")]
    public IResult GetCaptcha()
    {
        var result = captchaBusinessService.GenerateCaptcha();

        return Results.Ok(new CaptchaDto(result.CaptchaId, result.CaptchaImageBase64));
    }
}
