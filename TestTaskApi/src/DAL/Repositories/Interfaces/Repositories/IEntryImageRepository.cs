using Domain.EntryImages;

namespace DAL.Repositories.Interfaces.Repositories;

public interface IEntryImageRepository
{
    Task<EntryImage> Add(EntryImage entryImage, CancellationToken cancellationToken);
    Task<EntryImage> Update(EntryImage entryImage, CancellationToken cancellationToken);
    Task<EntryImage> Delete(EntryImage entryImage, CancellationToken cancellationToken);
}