using System.Reflection;
using Domain.DiaryEntries;
using Domain.EntryImages;
using Domain.Invites;
using Domain.Roles;
using Domain.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User, Role, Guid>(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Invite> Invites { get; set; }
    public DbSet<DiaryEntry> DiaryEntries { get; set; }
    public DbSet<EntryImage> EntryImages { get; set; }
    
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);
    }
}