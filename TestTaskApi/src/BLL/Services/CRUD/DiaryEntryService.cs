using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using BLL.Modules.Exceptions;
using DAL.Repositories.Interfaces.Queries;
using DAL.Repositories.Interfaces.Repositories;
using Domain.DiaryEntries;
using Domain.EntryImages;
using LanguageExt;
using Microsoft.AspNetCore.Http;

namespace BLL.Services.CRUD;

public class DiaryEntryService(
    IDiaryEntryRepository diaryEntryRepository,
    IDiaryEntryQueries diaryEntryQueries,
    IEntryImageRepository imageRepository,
    IEntryImageQueries imageQueries,
    ICryptoService cryptoService,
    IImageOptimizer imageOptimizer,
    IHttpContextAccessor httpContextAccessor) : IDiaryEntryService
{
    public async Task<PaginatedDiaryEntries> GetAllByUserIdAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        string? searchTerm,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var allEntries = await diaryEntryQueries.GetAllByUserId(userId, cancellationToken);

        IEnumerable<DiaryEntry> filteredEntries = allEntries;

        if (startDate.HasValue)
        {
            filteredEntries = filteredEntries.Where(e => e.EntryDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            filteredEntries = filteredEntries.Where(e => e.EntryDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredEntries = filteredEntries
                .Where(entry =>
                {
                    var decryptedContent = cryptoService.Decrypt(entry.EncryptedContent, entry.InitializationVector);
                    return decryptedContent.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
                });
        }

        var filteredList = filteredEntries.ToList();
        var totalCount = filteredList.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var paginatedEntries = filteredList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new List<DiaryEntryWithImageId>();
        foreach (var entry in paginatedEntries)
        {
            Guid? imageId = null;
            if (entry.HasImage)
            {
                var imageOption = await imageQueries.GetByEntryId(entry.Id, cancellationToken);
                imageId = imageOption.Match(
                    img => (Guid?)img.Id.Value,
                    () => null
                );
            }

            result.Add(new DiaryEntryWithImageId(entry, imageId));
        }

        return new PaginatedDiaryEntries(
            result,
            totalCount,
            pageNumber,
            pageSize,
            totalPages);
    }

    public async Task<Option<DiaryEntryWithImageId>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entryId = new DiaryEntryId(id);
        var entryOption = await diaryEntryQueries.GetById(entryId, cancellationToken);

        return await entryOption.MatchAsync(
            async entry =>
            {
                Guid? imageId = null;
                if (entry.HasImage)
                {
                    var imageOption = await imageQueries.GetByEntryId(entry.Id, cancellationToken);
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

    public async Task<Either<DiaryEntryException, DiaryEntry>> CreateAsync(
        string content,
        Stream? imageStream,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirst("id")?.Value;
        if (userId == null)
        {
            return new UnauthorizedDiaryEntryAccessException();
        }

        var (encText, textIv) = cryptoService.Encrypt(content);

        var entry = DiaryEntry.New(
            Guid.Parse(userId),
            encText,
            textIv,
            DateTime.UtcNow,
            imageStream != null
        );

        await diaryEntryRepository.Add(entry, cancellationToken);

        if (imageStream != null)
        {
            var (optimizedBytes, mimeType) = await imageOptimizer.OptimizeAsync(imageStream, cancellationToken);

            var imageEntity = EntryImage.New(
                entry.Id,
                optimizedBytes,
                mimeType);

            await imageRepository.Add(imageEntity, cancellationToken);
        }

        return entry;
    }

    public async Task<Either<DiaryEntryException, DiaryEntry>> UpdateAsync(
        Guid diaryEntryId,
        string content,
        Stream? imageStream,
        bool deleteCurrentImage,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContextAccessor.HttpContext?.User?.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return new UnauthorizedDiaryEntryAccessException();
        }

        var entryId = new DiaryEntryId(diaryEntryId);
        var diaryEntryOption = await diaryEntryQueries.GetById(entryId, cancellationToken);

        return await diaryEntryOption.Match(
            async entry =>
            {
                if (entry.UserId != userId)
                {
                    return new UnauthorizedDiaryEntryAccessException();
                }

                var (encText, textIv) = cryptoService.Encrypt(content);

                bool hasImage = entry.HasImage;

                if (imageStream != null)
                {
                    await DeleteExistingImageAsync(entry.Id, cancellationToken);

                    var (optimizedBytes, mimeType) = await imageOptimizer.OptimizeAsync(imageStream, cancellationToken);
                    var newImage = EntryImage.New(entry.Id, optimizedBytes, mimeType);
                    await imageRepository.Add(newImage, cancellationToken);

                    hasImage = true;
                }
                else if (deleteCurrentImage)
                {
                    await DeleteExistingImageAsync(entry.Id, cancellationToken);
                    hasImage = false;
                }

                entry.EncryptedContent = encText;
                entry.InitializationVector = textIv;
                entry.HasImage = hasImage;

                return await diaryEntryRepository.Update(entry, cancellationToken);
            },
            () => Task.FromResult<Either<DiaryEntryException, DiaryEntry>>(
                new DiaryEntryNotFoundException(diaryEntryId))
        );
    }

    public async Task<Either<DiaryEntryException, DiaryEntry>> DeleteAsync(
        Guid diaryEntryId,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContextAccessor.HttpContext?.User?.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return new UnauthorizedDiaryEntryAccessException();
        }

        var entryId = new DiaryEntryId(diaryEntryId);

        var diaryEntry = await diaryEntryQueries.GetById(entryId, cancellationToken);

        return await diaryEntry.Match(
            async entry =>
            {
                if (entry.UserId != userId)
                {
                    return new UnauthorizedDiaryEntryAccessException();
                }

                if (!CanBeDeleted(entry))
                {
                    return new DiaryEntryEntryCannotBeDeletedException(diaryEntryId);
                }

                var entryImages = await imageQueries.GetByEntryId(entryId, cancellationToken);
                await entryImages.MatchAsync(
                    async image =>
                    {
                        await imageRepository.Delete(image, cancellationToken);
                        return Unit.Default;
                    },
                    () => Task.FromResult(Unit.Default)
                );

                await diaryEntryRepository.Delete(entry, cancellationToken);
                return entry;
            },
            () => Task.FromResult<Either<DiaryEntryException, DiaryEntry>>(
                new DiaryEntryNotFoundException(diaryEntryId))
        );
    }

    private async Task DeleteExistingImageAsync(DiaryEntryId entryId, CancellationToken ct)
    {
        var existingImage = await imageQueries.GetByEntryId(entryId, ct);
        await existingImage.MatchAsync(
            async image =>
            {
                await imageRepository.Delete(image, ct);
                return Unit.Default;
            },
            () => Task.FromResult(Unit.Default)
        );
    }

    private static bool CanBeDeleted(DiaryEntry entry)
    {
        var timeSinceCreation = DateTime.UtcNow - entry.EntryDate;
        return timeSinceCreation.TotalDays <= 2;
    }
}
