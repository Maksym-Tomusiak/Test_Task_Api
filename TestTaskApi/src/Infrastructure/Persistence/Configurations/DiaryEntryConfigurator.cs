using Domain.DiaryEntries;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DiaryEntryConfigurator : IEntityTypeConfiguration<DiaryEntry>
{
    public void Configure(EntityTypeBuilder<DiaryEntry> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasConversion(
                diaryEntryId => diaryEntryId.Value,
                value => new DiaryEntryId(value));

        builder.Property(x => x.EntryDate)
            .HasConversion(new DateTimeUtcConverter());

        builder.Property(x => x.HasImage)
            .HasDefaultValue(false);

        builder.HasOne<User>(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId);
    }
}
