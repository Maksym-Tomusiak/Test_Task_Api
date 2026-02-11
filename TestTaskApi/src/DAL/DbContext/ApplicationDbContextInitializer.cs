using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DAL.DbContext;

public class ApplicationDbContextInitializer(ApplicationDbContext context, IConfiguration configuration)
{
    public async Task InitializeAsync()
    {
        await context.Database.MigrateAsync();
    }
}