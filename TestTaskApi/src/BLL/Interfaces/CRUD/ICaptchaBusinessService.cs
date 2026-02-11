namespace BLL.Interfaces.CRUD;

public record CaptchaResult(string CaptchaId, string CaptchaImageBase64);

public interface ICaptchaBusinessService
{
    CaptchaResult GenerateCaptcha();
}
