using DAL.DbContext;
using DAL.Seeders;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DAL.DbInit;

public static class DbModule
{
    public static async Task InitializeDb(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
        await initializer.InitializeAsync();
        
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (bool.Parse(config["AllowSeeder"]!))
        {
            await app.SeedRolesAsync();
            await app.SeedUsersAsync();
        }
    }
}