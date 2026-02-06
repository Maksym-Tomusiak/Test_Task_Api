using Api.Dtos;
using Api.Modules.Errors;
using Application.Common.Interfaces.Services;
using Application.DiaryEntries.Commands;
using Application.DiaryEntries.Exceptions;
using Application.DiaryEntries.Queries;
using Domain.DiaryEntries;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Wolverine;

namespace Api.Controllers;

[ApiController]
[Authorize]
public class DiaryEntriesController(IMessageBus messageBus, ICryptoService cryptoService, IMemoryCache cache) : ControllerBase
{
    [HttpGet("api/diary-entries")]
    public async Task<IResult> GetDiaryEntries(CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.FindFirst("id")?.Value;
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var cacheKey = $"diary_entries_user_{userId}";
        
        if (!cache.TryGetValue(cacheKey, out IEnumerable<DiaryEntryDto>? cachedEntries))
        {
            var query = new GetAllDiaryEntriesByUserIdQuery(Guid.Parse(userId));
            var entries = await messageBus.InvokeAsync<IReadOnlyList<DiaryEntry>>(query, cancellationToken);
            
            var dtos = entries.Select(e =>
            {
                var decryptedContent = cryptoService.Decrypt(e.EncryptedContent, e.InitializationVector);
                return DiaryEntryDto.FromDomainModel(e, decryptedContent);
            }).ToList();

            cache.Set(cacheKey, dtos, TimeSpan.FromMinutes(3));
            return Results.Ok(dtos);
        }

        return Results.Ok(cachedEntries);
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
            var query = new GetDiaryEntryByIdQuery(id);
            var result = await messageBus.InvokeAsync<Option<DiaryEntry>>(query, cancellationToken);

            return result.Match<IResult>(
                entry =>
                {
                    if (entry.UserId.ToString() != userId)
                    {
                        return Results.Forbid();
                    }

                    var decryptedContent = cryptoService.Decrypt(entry.EncryptedContent, entry.InitializationVector);
                    var entryDto = DiaryEntryDto.FromDomainModel(entry, decryptedContent);
                    cache.Set(cacheKey, entryDto, TimeSpan.FromMinutes(5));
                    return Results.Ok(entryDto);
                },
                () => Results.NotFound(new { error = "Diary entry not found" })
            );
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

        var cmd = new CreateDiaryEntryCommand(request.Content, imageStream);
        var res = await messageBus.InvokeAsync<Either<DiaryEntryException, DiaryEntry>>(cmd, cancellationToken);

        return res.Match<IResult>(
            entry =>
            {
                var userId = HttpContext.User.FindFirst("id")?.Value;
                if (userId != null)
                {
                    // Invalidate user's diary entries list cache
                    cache.Remove($"diary_entries_user_{userId}");
                }

                var decryptedContent = cryptoService.Decrypt(entry.EncryptedContent, entry.InitializationVector);
                return Results.Created($"/api/diary-entries/{entry.Id.Value}", DiaryEntryDto.FromDomainModel(entry, decryptedContent));
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

        var cmd = new UpdateDiaryEntryCommand(id, request.Content, imageStream, deleteCurrentImage);
        var res = await messageBus.InvokeAsync<Either<DiaryEntryException, DiaryEntry>>(cmd, cancellationToken);

        return res.Match<IResult>(
            entry =>
            {
                var userId = HttpContext.User.FindFirst("id")?.Value;
                if (userId != null)
                {
                    // Invalidate related caches
                    cache.Remove($"diary_entries_user_{userId}");
                    cache.Remove($"diary_entry_{id}_user_{userId}");
                    cache.Remove($"entry_image_{id}_user_{userId}");
                }

                var decryptedContent = cryptoService.Decrypt(entry.EncryptedContent, entry.InitializationVector);
                return Results.Ok(DiaryEntryDto.FromDomainModel(entry, decryptedContent));
            },
            ex => ex.ToIResult());
    }

    [HttpDelete("api/diary-entries/{id}")]
    public async Task<IResult> DeleteDiaryEntry(Guid id, CancellationToken cancellationToken)
    {
        var cmd = new DeleteDiaryEntryCommand(id);
        var res = await messageBus.InvokeAsync<Either<DiaryEntryException, DiaryEntry>>(cmd, cancellationToken);

        return res.Match<IResult>(
            entry => 
            {
                var userId = HttpContext.User.FindFirst("id")?.Value;
                if (userId != null)
                {
                    // Invalidate related caches
                    cache.Remove($"diary_entries_user_{userId}");
                    cache.Remove($"diary_entry_{id}_user_{userId}");
                    cache.Remove($"entry_image_{id}_user_{userId}");
                }
                return Results.Ok(new { message = "Diary entry deleted successfully" });
            },
            ex => ex.ToIResult());
    }
}
