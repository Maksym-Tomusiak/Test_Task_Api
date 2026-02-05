using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.DiaryEntries.Exceptions;
using Domain.DiaryEntries;
using LanguageExt;
using Microsoft.AspNetCore.Http;

namespace Application.DiaryEntries.Commands;

public record DeleteDiaryEntryCommand(Guid DiaryEntryId);

public static class DeleteDiaryEntryCommandHandler
{
    public static async Task<Either<DiaryEntryException, DiaryEntry>> Handle(
        DeleteDiaryEntryCommand command,
        IDiaryEntryRepository diaryEntryRepository,
        IDiaryEntryQueries diaryEntryQueries,
        IEntryImageRepository imageRepository,
        IEntryImageQueries imageQueries,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContextAccessor.HttpContext?.User?.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return new UnauthorizedDiaryEntryAccessException();
        }

        var diaryEntryId = new DiaryEntryId(command.DiaryEntryId);

        var diaryEntry = await diaryEntryQueries.GetById(diaryEntryId, cancellationToken);

        return await diaryEntry.Match(
            async entry =>
            {
                if (entry.UserId != userId)
                {
                    return new UnauthorizedDiaryEntryAccessException();
                }
                
                if (!entry.CanBeDeleted())
                {
                    return new DiaryEntryEntryCannotBeDeletedException(command.DiaryEntryId);
                }

                var entryImages = await imageQueries.GetByEntryId(diaryEntryId, cancellationToken);
                foreach (var image in entryImages)
                {
                    await imageRepository.Delete(image, cancellationToken);
                }

                await diaryEntryRepository.Delete(entry, cancellationToken);
                return entry;
            },
            () => Task.FromResult<Either<DiaryEntryException, DiaryEntry>>(
                new DiaryEntryNotFoundException(command.DiaryEntryId))
        );
    }
    
    private static bool CanBeDeleted(this DiaryEntry entry)
    {
        var timeSinceCreation = DateTime.UtcNow - entry.EntryDate;
        return timeSinceCreation.TotalDays <= 2;
    }
}