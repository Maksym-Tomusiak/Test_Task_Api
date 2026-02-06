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

        var query = new GetAllDiaryEntriesByUserIdQuery(Guid.Parse(userId), pageNumber, pageSize, searchTerm, startDate, endDate);
        var paginatedResult = await messageBus.InvokeAsync<PaginatedDiaryEntries>(query, cancellationToken);
        
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
            var query = new GetDiaryEntryByIdQuery(id);
            var result = await messageBus.InvokeAsync<Option<DiaryEntryWithImageId>>(query, cancellationToken);

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

        var cmd = new CreateDiaryEntryCommand(request.Content, imageStream);
        var res = await messageBus.InvokeAsync<Either<DiaryEntryException, DiaryEntry>>(cmd, cancellationToken);

        return await res.MatchAsync<IResult>(
            async entry =>
            {
                var decryptedContent = cryptoService.Decrypt(entry.EncryptedContent, entry.InitializationVector);
                Guid? imageId = null;
                
                if (entry.HasImage)
                {
                    var query = new GetDiaryEntryByIdQuery(entry.Id.Value);
                    var entryWithImageOption = await messageBus.InvokeAsync<Option<DiaryEntryWithImageId>>(query, cancellationToken);
                    imageId = entryWithImageOption.Match(
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

        var cmd = new UpdateDiaryEntryCommand(id, request.Content, imageStream, deleteCurrentImage);
        var res = await messageBus.InvokeAsync<Either<DiaryEntryException, DiaryEntry>>(cmd, cancellationToken);

        return await res.MatchAsync<IResult>(
            async entry =>
            {
                var userId = HttpContext.User.FindFirst("id")?.Value;
                if (userId != null)
                {
                    // Invalidate related caches
                    cache.Remove($"diary_entry_{id}_user_{userId}");
                    cache.Remove($"entry_image_{id}_user_{userId}");
                }

                var decryptedContent = cryptoService.Decrypt(entry.EncryptedContent, entry.InitializationVector);
                Guid? imageId = null;
                
                if (entry.HasImage)
                {
                    var query = new GetDiaryEntryByIdQuery(entry.Id.Value);
                    var entryWithImageOption = await messageBus.InvokeAsync<Option<DiaryEntryWithImageId>>(query, cancellationToken);
                    imageId = entryWithImageOption.Match(
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
        var cmd = new DeleteDiaryEntryCommand(id);
        var res = await messageBus.InvokeAsync<Either<DiaryEntryException, DiaryEntry>>(cmd, cancellationToken);

        return res.Match<IResult>(
            entry => 
            {
                var userId = HttpContext.User.FindFirst("id")?.Value;
                if (userId != null)
                {
                    // Invalidate related caches
                    cache.Remove($"diary_entry_{id}_user_{userId}");
                    cache.Remove($"entry_image_{id}_user_{userId}");
                }
                return Results.Ok(new { message = "Diary entry deleted successfully" });
            },
            ex => ex.ToIResult());
    }
}
