using Application.Common.Interfaces.Queries;
using Domain.DiaryEntries;
using LanguageExt;

namespace Application.DiaryEntries.Queries;

public record GetDiaryEntryByIdQuery(Guid Id);

public static class GetDiaryEntryByIdHandler
{
    public static async Task<Option<DiaryEntryWithImageId>> Handle(
        GetDiaryEntryByIdQuery request, 
        IDiaryEntryQueries diaryQueries,
        IEntryImageQueries imageQueries,
        CancellationToken ct)
    {
        var id = new DiaryEntryId(request.Id);
        var entryOption = await diaryQueries.GetById(id, ct);
        
        return await entryOption.MatchAsync(
            async entry =>
            {
                Guid? imageId = null;
                if (entry.HasImage)
                {
                    var imageOption = await imageQueries.GetByEntryId(entry.Id, ct);
                    imageId = imageOption.Match(
                        img => (Guid?)img.Id.Value,
                        () => null
                    );
                }
                
                return Option<DiaryEntryWithImageId>.Some(new DiaryEntryWithImageId(entry, imageId));
            },
            () => Task.FromResult(Option<DiaryEntryWithImageId>.None)
        );
    }
}
