using Application.Common.Interfaces;
using Application.Common.Interfaces.Services; // Ensure this namespace matches where ICaptchaService is
using Microsoft.Extensions.Caching.Memory;

namespace Application.Captcha.Commands;

public record GenerateCaptchaCommand();

public static class GenerateCaptchaCommandHandler
{
    public static async Task<GenerateCaptchaResult> Handle(
        GenerateCaptchaCommand command,
        IMemoryCache cache,
        ICaptchaService captchaService,
        CancellationToken cancellationToken)
    {
        var (code, imageBytes) = captchaService.GenerateCaptcha();

        var captchaId = Guid.NewGuid().ToString();

        cache.Set(captchaId, code, TimeSpan.FromMinutes(5));

        var captchaImageBase64 = Convert.ToBase64String(imageBytes);

        return await Task.FromResult(new GenerateCaptchaResult(captchaId, captchaImageBase64));
    }
}