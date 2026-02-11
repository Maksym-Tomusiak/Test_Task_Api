using Domain.EntryImages;
using LanguageExt;

namespace BLL.Interfaces.CRUD;

public interface IEntryImageService
{
    Task<Option<EntryImage>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Option<EntryImage>> GetByEntryIdAsync(Guid entryId, CancellationToken cancellationToken);
}
