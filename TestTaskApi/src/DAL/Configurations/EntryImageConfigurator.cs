using Domain.DiaryEntries;
using Domain.EntryImages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Configurations;

public class EntryImageConfigurator : IEntityTypeConfiguration<EntryImage>
{
    public void Configure(EntityTypeBuilder<EntryImage> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasConversion(
                entryImageId => entryImageId.Value,
                value => new EntryImageId(value));

        builder.Property(x => x.EntryId)
            .HasConversion(
                entryId => entryId.Value,
                value => new DiaryEntryId(value));

        builder.HasOne<DiaryEntry>(x => x.Entry)
            .WithMany()
            .HasForeignKey(x => x.EntryId);
    }
}