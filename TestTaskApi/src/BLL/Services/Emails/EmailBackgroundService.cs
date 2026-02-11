using BLL.Interfaces.Emails;
using BLL.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BLL.Services.Emails;

public class EmailBackgroundService(
    IBackgroundEmailQueue emailQueue,
    IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            EmailMessage? message = null;
            try
            {
                message = await emailQueue.DequeueEmailAsync(stoppingToken);

                using var scope = scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                await emailService.SendEmail(
                    message.ToEmail,
                    message.Subject,
                    message.Body,
                    message.IsHtml);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
    }
}