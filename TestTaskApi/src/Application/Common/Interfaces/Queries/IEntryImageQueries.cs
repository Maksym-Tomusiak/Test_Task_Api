using Domain.DiaryEntries;
using Domain.EntryImages;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IEntryImageQueries
{
    Task<Option<EntryImage>> GetById(EntryImageId id, CancellationToken cancellationToken);

    Task<Option<EntryImage>> GetByEntryId(DiaryEntryId entryId, CancellationToken cancellationToken);
}