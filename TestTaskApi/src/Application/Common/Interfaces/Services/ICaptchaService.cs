namespace Application.Common.Interfaces.Services;

public interface ICaptchaService
{
    (string Code, byte[] ImageBytes) GenerateCaptcha();
}