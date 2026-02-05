using Application.Common.Interfaces.Services.Emails;
using Infrastructure.Services.Emails;
using Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services;

public static class ConfigureServices
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        AddEmailService(services, configuration);
    }
    private static void AddEmailService(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddScoped<IEmailService, EmailService>();

        services.AddSingleton<IBackgroundEmailQueue, BackgroundEmailQueue>();
        services.AddHostedService<EmailBackgroundService>();
    }
}