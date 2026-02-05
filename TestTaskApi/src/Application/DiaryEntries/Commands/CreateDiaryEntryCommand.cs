using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Services;
using Application.DiaryEntries.Exceptions;
using Domain.DiaryEntries;
using Domain.EntryImages;
using Domain.Users;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Application.DiaryEntries.Commands;

public record CreateDiaryEntryCommand(
    string Content,
    Stream? ImageStream);

public static class CreateDiaryEntryCommandHandler
{
    public static async Task<Either<DiaryEntryException, DiaryEntry>> Handle(
        CreateDiaryEntryCommand command,
        IDiaryEntryRepository diaryEntryRepository,
        UserManager<User> userManager,
        IEntryImageRepository imageRepository,
        IEntryImageQueries imageQueries,
        ICryptoService cryptoService,
        IImageOptimizer imageOptimizer,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirst("id")?.Value;
        if (userId == null)
        {
            return new UnauthorizedDiaryEntryAccessException();
        }
        
        var (encText, textIv) = cryptoService.Encrypt(command.Content);
        
        var entry = DiaryEntry.New
        (
            Guid.Parse(userId),
            encText,
            textIv,
            DateTime.UtcNow,
            command.ImageStream != null
        );
        
        await diaryEntryRepository.Add(entry, cancellationToken);
        
        if (command.ImageStream != null)
        {
            var (optimizedBytes, mimeType) = await imageOptimizer.OptimizeAsync(command.ImageStream, cancellationToken);

            var imageEntity = EntryImage.New(
                entry.Id,
                optimizedBytes,
                mimeType);
            
            await imageRepository.Add(imageEntity, cancellationToken);
        }
        
        return entry;
    }
}