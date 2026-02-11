namespace BLL.Interfaces;

public interface ICaptchaService
{
    (string Code, byte[] ImageBytes) GenerateCaptcha();
}