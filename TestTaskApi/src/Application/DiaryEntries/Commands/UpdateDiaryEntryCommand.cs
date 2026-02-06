using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Services;
using Application.DiaryEntries.Exceptions;
using Domain.DiaryEntries;
using Domain.EntryImages;
using LanguageExt;
using Microsoft.AspNetCore.Http;

namespace Application.DiaryEntries.Commands;

public record UpdateDiaryEntryCommand(
    Guid DiaryEntryId,
    string Content,
    Stream? ImageStream,
    bool DeleteCurrentImage = false);

public static class UpdateDiaryEntryCommandHandler
{
    public static async Task<Either<DiaryEntryException, DiaryEntry>> Handle(
        UpdateDiaryEntryCommand command,
        IDiaryEntryRepository diaryEntryRepository,
        IDiaryEntryQueries diaryEntryQueries,
        IEntryImageRepository imageRepository,
        IEntryImageQueries imageQueries,
        ICryptoService cryptoService,
        IImageOptimizer imageOptimizer,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContextAccessor.HttpContext?.User?.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return new UnauthorizedDiaryEntryAccessException();
        }

        var diaryEntryId = new DiaryEntryId(command.DiaryEntryId);
        var diaryEntryOption = await diaryEntryQueries.GetById(diaryEntryId, cancellationToken);

        return await diaryEntryOption.Match(
            async entry =>
            {
                if (entry.UserId != userId)
                {
                    return new UnauthorizedDiaryEntryAccessException();
                }

                var (encText, textIv) = cryptoService.Encrypt(command.Content);
                
                bool hasImage = entry.HasImage;

                if (command.ImageStream != null)
                {
                    await DeleteExistingImage(entry.Id, imageQueries, imageRepository, cancellationToken);

                    var (optimizedBytes, mimeType) = await imageOptimizer.OptimizeAsync(command.ImageStream, cancellationToken);
                    var newImage = EntryImage.New(entry.Id, optimizedBytes, mimeType);
                    await imageRepository.Add(newImage, cancellationToken);
                    
                    hasImage = true;
                }
                else if (command.DeleteCurrentImage)
                {
                    await DeleteExistingImage(entry.Id, imageQueries, imageRepository, cancellationToken);
                    hasImage = false;
                }
                
                entry.EncryptedContent = encText;
                entry.InitializationVector = textIv;
                entry.HasImage = hasImage;
                
                return await diaryEntryRepository.Update(entry, cancellationToken);
            },
            () => Task.FromResult<Either<DiaryEntryException, DiaryEntry>>(
                new DiaryEntryNotFoundException(command.DiaryEntryId))
        );
    }

    private static async Task DeleteExistingImage(
        DiaryEntryId entryId, 
        IEntryImageQueries queries, 
        IEntryImageRepository repo, 
        CancellationToken ct)
    {
        var existingImages = await queries.GetByEntryId(entryId, ct);
        foreach (var image in existingImages)
        {
            await repo.Delete(image, ct);
        }
    }
}