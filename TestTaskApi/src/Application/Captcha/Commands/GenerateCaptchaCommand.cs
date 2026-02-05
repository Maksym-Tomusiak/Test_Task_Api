using Application.Common.Interfaces;
using Application.Common.Interfaces.Services; // Ensure this namespace matches where ICaptchaService is
using Microsoft.Extensions.Caching.Memory;

namespace Application.Captcha.Commands;

public record GenerateCaptchaCommand();

public static class GenerateCaptchaCommandHandler
{
    public static Task<(string CaptchaId, string CaptchaImageBase64)> Handle(
        GenerateCaptchaCommand command,
        IMemoryCache cache,
        ICaptchaService captchaService,
        CancellationToken cancellationToken)
    {
        var (code, imageBytes) = captchaService.GenerateCaptcha();

        var captchaId = Guid.NewGuid().ToString();

        cache.Set(captchaId, code, TimeSpan.FromMinutes(5));

        var imageBase64 = Convert.ToBase64String(imageBytes);

        return Task.FromResult((captchaId, imageBase64));
    }
}