using DAL.DbContext;
using Domain.Roles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DAL.Seeders;

public static class RolesSeeder
{
    public static async Task SeedRolesAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

        var roles = new[]
        {
            "Admin",
            "User"
        };

        foreach (var role in roles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }
            await roleManager.CreateAsync(new Role { Name = role });
        }
        await context.SaveChangesAsync();
    }
}