using Domain.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.Seeders;

public static class UsersSeeder
{
    public static async Task SeedUsersAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        if (!context.Users.Any())
        {
            var roles = await context.Roles.ToListAsync();
            var adminRole = roles.FirstOrDefault(r => r.Name == "Admin");

            if (adminRole == null)
            {
                throw new Exception("Roles must be seeded before adding users.");
            }

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = "admin",
            };

            await userManager.CreateAsync(adminUser, "admin");
            await userManager.AddToRoleAsync(adminUser, adminRole.Name);
        }
    }
}