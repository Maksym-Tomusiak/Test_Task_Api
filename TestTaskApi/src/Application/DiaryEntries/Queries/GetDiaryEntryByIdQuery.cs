using Application.Common.Interfaces.Queries;
using Domain.DiaryEntries;
using LanguageExt;

namespace Application.DiaryEntries.Queries;

public record GetDiaryEntryByIdQuery(Guid Id);

public static class GetDiaryEntryByIdHandler
{
    public static async Task<Option<DiaryEntry>> Handle(
        GetDiaryEntryByIdQuery request, 
        IDiaryEntryQueries queries, 
        CancellationToken ct)
    {
        var id = new DiaryEntryId(request.Id);
        return await queries.GetById(id, ct);
    }
}
