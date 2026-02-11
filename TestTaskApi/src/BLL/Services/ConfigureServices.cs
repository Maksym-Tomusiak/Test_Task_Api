using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using BLL.Interfaces.Emails;
using BLL.Models;
using BLL.Services.Authentication;
using BLL.Services.CRUD;
using BLL.Services.Emails;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BLL.Services;

public static class ConfigureServices
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEmailService(configuration);
        services.AddCaptchaServices();
        services.AddCryptoService(configuration);
        services.AddImageOptimisingService();
        services.AddHangfire(configuration);
        services.AddBusinessServices();
        services.AddJwtService();
    }

    private static void AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IDiaryEntryService, DiaryEntryService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEntryImageService, EntryImageService>();
        services.AddScoped<IInviteService, InviteService>();
        services.AddScoped<ICaptchaBusinessService, CaptchaBusinessService>();
    }
    private static void AddEmailService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddScoped<IEmailService, EmailService>();

        services.AddSingleton<IBackgroundEmailQueue, BackgroundEmailQueue>();
        services.AddHostedService<EmailBackgroundService>();
    }
    
    private static void AddCaptchaServices(this IServiceCollection services)
    {
        services.AddScoped<ICaptchaService, SkiaCaptchaService>();
    }
    
    private static void AddCryptoService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICryptoService, AesCryptoService>();
    }
    
    private static void AddImageOptimisingService(this IServiceCollection services)
    {
        services.AddScoped<IImageOptimizer, SkiaImageOptimizer>();
    }
    
    private static void AddHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HangfireConnection");
        services.AddHangfire(config =>
            config.UsePostgreSqlStorage((opt) => { opt.UseNpgsqlConnection(connectionString); }));
        services.AddHangfireServer();
    }

    private static void AddJwtService(this IServiceCollection services)
    {
        services.AddScoped<IJwtProvider, JwtProvider>();
    }
}