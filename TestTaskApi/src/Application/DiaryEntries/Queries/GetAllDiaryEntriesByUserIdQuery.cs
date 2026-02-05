using Application.Common.Interfaces.Queries;
using Domain.DiaryEntries;

namespace Application.DiaryEntries.Queries;

public record GetAllDiaryEntriesByUserIdQuery(Guid UserId);

public static class GetAllDiaryEntriesByUserIdHandler
{
    public static async Task<IReadOnlyList<DiaryEntry>> Handle(
        GetAllDiaryEntriesByUserIdQuery request, 
        IDiaryEntryQueries queries, 
        CancellationToken ct)
    {
        return await queries.GetAllByUserId(request.UserId, ct);
    }
}
