using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using Microsoft.Extensions.Caching.Memory;

namespace BLL.Services.CRUD;

public class CaptchaBusinessService(
    IMemoryCache cache,
    ICaptchaService captchaService) : ICaptchaBusinessService
{
    public CaptchaResult GenerateCaptcha()
    {
        var (code, imageBytes) = captchaService.GenerateCaptcha();

        var captchaId = Guid.NewGuid().ToString();

        cache.Set(captchaId, code, TimeSpan.FromMinutes(5));

        var captchaImageBase64 = Convert.ToBase64String(imageBytes);

        return new CaptchaResult(captchaId, captchaImageBase64);
    }
}
