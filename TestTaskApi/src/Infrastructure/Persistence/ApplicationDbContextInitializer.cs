using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence;

public class ApplicationDbContextInitializer(ApplicationDbContext context, IConfiguration configuration)
{
    public async Task InitializeAsync()
    {
        await context.Database.MigrateAsync();
    }
}