namespace Application.Common.Interfaces.Services.Emails;

public interface IEmailService
{
    Task SendEmail(string to, string subject, string body, bool isHtml = false);
}