using Application.Common.Interfaces.Queries;
using Domain.EntryImages;
using LanguageExt;

namespace Application.EntryImages.Queries;

public record GetEntryImageByIdQuery(Guid Id);

public static class GetEntryImageByIdHandler
{
    public static async Task<Option<EntryImage>> Handle(
        GetEntryImageByIdQuery request, 
        IEntryImageQueries queries, 
        CancellationToken ct)
    {
        var id = new EntryImageId(request.Id);
        return await queries.GetById(id, ct);
    }
}
