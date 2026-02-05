using Application.Common.Interfaces.Services;
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
        AddCaptchaServices(services);
        AddCryptoService(services, configuration);
    }
    private static void AddEmailService(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddScoped<IEmailService, EmailService>();

        services.AddSingleton<IBackgroundEmailQueue, BackgroundEmailQueue>();
        services.AddHostedService<EmailBackgroundService>();
    }
    
    private static void AddCaptchaServices(IServiceCollection services)
    {
        services.AddScoped<ICaptchaService, SkiaCaptchaService>();
    }
    
    private static void AddCryptoService(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICryptoService, AesCryptoService>();
    }
    
    private static void AddImageOptimisingService(IServiceCollection services)
    {
        services.AddScoped<IImageOptimizer, SkiaImageOptimizer>();
    }
}