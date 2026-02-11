using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using DAL.Repositories.Interfaces.Queries;
using Domain.DiaryEntries;
using Domain.EntryImages;
using LanguageExt;

namespace BLL.Services.CRUD;

public class EntryImageService(IEntryImageQueries imageQueries) : IEntryImageService
{
    public async Task<Option<EntryImage>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var imageId = new EntryImageId(id);
        return await imageQueries.GetById(imageId, cancellationToken);
    }

    public async Task<Option<EntryImage>> GetByEntryIdAsync(Guid entryId, CancellationToken cancellationToken)
    {
        var diaryEntryId = new DiaryEntryId(entryId);
        return await imageQueries.GetByEntryId(diaryEntryId, cancellationToken);
    }
}
