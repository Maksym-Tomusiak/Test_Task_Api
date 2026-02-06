using Domain.RefreshTokens;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RefreshTokenConfigurator : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new RefreshTokenId(x))
            .IsRequired();
        
        builder.Property(x => x.Token)
            .HasColumnType("varchar(255)")
            .IsRequired();
        
        builder.Property(x => x.Expires)
            .HasConversion(new DateTimeUtcConverter());
        
        builder.Property(x => x.IsRevoked)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();
        
        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<RefreshToken>(x => x.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}