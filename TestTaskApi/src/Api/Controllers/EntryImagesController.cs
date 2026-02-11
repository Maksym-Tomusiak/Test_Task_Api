using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Controllers;

[ApiController]
[Authorize]
public class EntryImagesController(
    IEntryImageService entryImageService,
    IDiaryEntryService diaryEntryService,
    IMemoryCache cache) : ControllerBase
{
    [HttpGet("api/entry-images/{id}")]
    public async Task<IResult> GetEntryImage(Guid id, CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.FindFirst("id")?.Value;
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var cacheKey = $"entry_image_{id}_user_{userId}";

        if (!cache.TryGetValue(cacheKey, out (byte[] ImageData, string MimeType)? cachedImage))
        {
            var result = await entryImageService.GetByIdAsync(id, cancellationToken);

            return await result.Match<Task<IResult>>(
                async image =>
                {
                    var diaryEntry = await diaryEntryService.GetByIdAsync(image.EntryId.Value, cancellationToken);

                    return await diaryEntry.Match<Task<IResult>>(
                        entry =>
                        {
                            if (entry.Entry.UserId.ToString() != userId)
                            {
                                return Task.FromResult(Results.Forbid());
                            }

                            var imageData = (image.ImageData, image.MimeType);
                            cache.Set(cacheKey, imageData, TimeSpan.FromMinutes(10));
                            return Task.FromResult(Results.File(image.ImageData, image.MimeType));
                        },
                        () => Task.FromResult(Results.NotFound(new { error = "Entry not found" }))
                    );
                },
                () => Task.FromResult(Results.NotFound(new { error = "Image not found" }))
            );
        }

        return Results.File(cachedImage.Value.ImageData, cachedImage.Value.MimeType);
    }
}
