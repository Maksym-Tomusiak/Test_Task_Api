using DAL.Converters;
using Domain.Invites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Configurations;

public class InviteConfigurator : IEntityTypeConfiguration<Invite>
{
    public void Configure(EntityTypeBuilder<Invite> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasConversion(
                inviteId => inviteId.Value,
                value => new InviteId(value));

        builder.Property(x => x.ExpiresAt)
            .HasConversion(new DateTimeUtcConverter());

        builder.Property(x => x.IsUsed)
            .HasDefaultValue(false);
    }
}
