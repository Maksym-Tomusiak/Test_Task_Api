using BLL.Dtos;
using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using BLL.Modules.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Controllers;

[ApiController]
[Authorize]
public class DiaryEntriesController(
    IDiaryEntryService diaryEntryService,
    ICryptoService cryptoService,
    IMemoryCache cache) : ControllerBase
{
    [HttpGet("api/diary-entries")]
    public async Task<IResult> GetDiaryEntries(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 5,
        [FromQuery] string? searchTerm = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var userId = HttpContext.User.FindFirst("id")?.Value;
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var paginatedResult = await diaryEntryService.GetAllByUserIdAsync(
            Guid.Parse(userId),
            pageNumber,
            pageSize,
            searchTerm,
            startDate,
            endDate,
            cancellationToken);

        var dtos = paginatedResult.Items.Select(e =>
        {
            var decryptedContent = cryptoService.Decrypt(e.Entry.EncryptedContent, e.Entry.InitializationVector);
            return DiaryEntryDto.FromDomainModel(e.Entry, decryptedContent, e.ImageId);
        }).ToList();

        var result = new PaginatedDiaryEntriesDto(
            dtos,
            paginatedResult.TotalCount,
            paginatedResult.PageNumber,
            paginatedResult.PageSize,
            paginatedResult.TotalPages);

        return Results.Ok(result);
    }

    [HttpGet("api/diary-entries/{id}")]
    public async Task<IResult> GetDiaryEntry(Guid id, CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.FindFirst("id")?.Value;
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var cacheKey = $"diary_entry_{id}_user_{userId}";

        if (!cache.TryGetValue(cacheKey, out DiaryEntryDto? cachedEntry))
        {
            var result = await diaryEntryService.GetByIdAsync(id, cancellationToken);

            if (result.IsNone)
            {
                return Results.NotFound(new { error = "Diary entry not found" });
            }

            var entryWithImage = result.First();
            if (entryWithImage.Entry.UserId.ToString() != userId)
            {
                return Results.Forbid();
            }

            var decryptedContent = cryptoService.Decrypt(entryWithImage.Entry.EncryptedContent, entryWithImage.Entry.InitializationVector);
            var entryDto = DiaryEntryDto.FromDomainModel(entryWithImage.Entry, decryptedContent, entryWithImage.ImageId);
            cache.Set(cacheKey, entryDto, TimeSpan.FromMinutes(5));
            return Results.Ok(entryDto);
        }

        return Results.Ok(cachedEntry);
    }

    [HttpPost("api/diary-entries")]
    public async Task<IResult> CreateDiaryEntry([FromForm] CreateDiaryEntryDto request, [FromForm] IFormFile? image, CancellationToken cancellationToken)
    {
        Stream? imageStream = null;
        if (image != null)
        {
            imageStream = image.OpenReadStream();
        }

        var res = await diaryEntryService.CreateAsync(request.Content, imageStream, cancellationToken);

        return await res.MatchAsync<IResult>(
            async entry =>
            {
                var decryptedContent = cryptoService.Decrypt(entry.EncryptedContent, entry.InitializationVector);
                Guid? imageId = null;

                if (entry.HasImage)
                {
                    var entryResult = await diaryEntryService.GetByIdAsync(entry.Id.Value, cancellationToken);
                    imageId = entryResult.Match(
                        e => e.ImageId,
                        () => null
                    );
                }

                return Results.Created($"/api/diary-entries/{entry.Id.Value}", DiaryEntryDto.FromDomainModel(entry, decryptedContent, imageId));
            },
            ex => ex.ToIResult());
    }

    [HttpPut("api/diary-entries/{id}")]
    public async Task<IResult> UpdateDiaryEntry(
        Guid id,
        [FromForm] UpdateDiaryEntryDto request,
        [FromForm] IFormFile? image,
        [FromForm] bool deleteCurrentImage = false,
        CancellationToken cancellationToken = default)
    {
        Stream? imageStream = null;
        if (image != null)
        {
            imageStream = image.OpenReadStream();
        }

        var res = await diaryEntryService.UpdateAsync(id, request.Content, imageStream, deleteCurrentImage, cancellationToken);

        return await res.MatchAsync<IResult>(
            async entry =>
            {
                var userId = HttpContext.User.FindFirst("id")?.Value;
                if (userId != null)
                {
                    cache.Remove($"diary_entry_{id}_user_{userId}");
                    cache.Remove($"entry_image_{id}_user_{userId}");
                }

                var decryptedContent = cryptoService.Decrypt(entry.EncryptedContent, entry.InitializationVector);
                Guid? imageId = null;

                if (entry.HasImage)
                {
                    var entryResult = await diaryEntryService.GetByIdAsync(entry.Id.Value, cancellationToken);
                    imageId = entryResult.Match(
                        e => e.ImageId,
                        () => null
                    );
                }

                return Results.Ok(DiaryEntryDto.FromDomainModel(entry, decryptedContent, imageId));
            },
            ex => ex.ToIResult());
    }

    [HttpDelete("api/diary-entries/{id}")]
    public async Task<IResult> DeleteDiaryEntry(Guid id, CancellationToken cancellationToken)
    {
        var res = await diaryEntryService.DeleteAsync(id, cancellationToken);

        return res.Match<IResult>(
            entry =>
            {
                var userId = HttpContext.User.FindFirst("id")?.Value;
                if (userId != null)
                {
                    cache.Remove($"diary_entry_{id}_user_{userId}");
                    cache.Remove($"entry_image_{id}_user_{userId}");
                }
                return Results.Ok(new { message = "Diary entry deleted successfully" });
            },
            ex => ex.ToIResult());
    }
}
