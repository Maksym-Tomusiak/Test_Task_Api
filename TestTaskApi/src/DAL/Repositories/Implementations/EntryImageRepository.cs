using DAL.DbContext;
using DAL.Repositories.Interfaces.Queries;
using DAL.Repositories.Interfaces.Repositories;
using Domain.DiaryEntries;
using Domain.EntryImages;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Implementations;

public class EntryImageRepository(ApplicationDbContext context) : IEntryImageRepository, IEntryImageQueries
{
    public async Task<EntryImage> Add(EntryImage entryImage, CancellationToken cancellationToken)
    {
        await context.EntryImages.AddAsync(entryImage, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entryImage;
    }

    public async Task<EntryImage> Update(EntryImage entryImage, CancellationToken cancellationToken)
    {
        context.EntryImages.Update(entryImage);
        await context.SaveChangesAsync(cancellationToken);
        return entryImage;
    }

    public async Task<EntryImage> Delete(EntryImage entryImage, CancellationToken cancellationToken)
    {
        context.EntryImages.Remove(entryImage);
        await context.SaveChangesAsync(cancellationToken);
        return entryImage;
    }

    public async Task<Option<EntryImage>> GetById(EntryImageId id, CancellationToken cancellationToken)
    {
        var entity = await context.EntryImages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            
        return entity == null ? Option<EntryImage>.None : Option<EntryImage>.Some(entity);
    }

    public async Task<Option<EntryImage>> GetByEntryId(DiaryEntryId entryId, CancellationToken cancellationToken)
    {
        var entity = await context.EntryImages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EntryId == entryId, cancellationToken);
            
        return entity == null ? Option<EntryImage>.None : Option<EntryImage>.Some(entity);
    }
}