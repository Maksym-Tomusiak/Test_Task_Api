using Application.Common.Interfaces.Queries;
using Domain.DiaryEntries;
using Domain.EntryImages;
using LanguageExt;

namespace Application.EntryImages.Queries;

public record GetEntryImageByEntryIdQuery(Guid EntryId);

public static class GetEntryImageByEntryIdHandler
{
    public static async Task<Option<EntryImage>> Handle(
        GetEntryImageByEntryIdQuery request, 
        IEntryImageQueries queries, 
        CancellationToken ct)
    {
        var entryId = new DiaryEntryId(request.EntryId);
        return await queries.GetByEntryId(entryId, ct);
    }
}
