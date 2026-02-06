namespace Application.Captcha.Commands;

public record GenerateCaptchaResult(string CaptchaId, string CaptchaImageBase64);