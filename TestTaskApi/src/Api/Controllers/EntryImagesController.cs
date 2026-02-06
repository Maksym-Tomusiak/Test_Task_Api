using Application.Common.Interfaces.Queries;
using Application.EntryImages.Queries;
using Domain.EntryImages;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Wolverine;

namespace Api.Controllers;

[ApiController]
[Authorize]
public class EntryImagesController(IMessageBus messageBus, IDiaryEntryQueries diaryEntryQueries, IMemoryCache cache) : ControllerBase
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
            var query = new GetEntryImageByIdQuery(id);
            var result = await messageBus.InvokeAsync<Option<EntryImage>>(query, cancellationToken);

            return await result.Match<Task<IResult>>(
                async image =>
                {
                    // Verify the user has access to this image by checking the diary entry
                    var diaryEntry = await diaryEntryQueries.GetById(image.EntryId, cancellationToken);
                    
                    return await diaryEntry.Match<Task<IResult>>(
                        async entry =>
                        {
                            if (entry.UserId.ToString() != userId)
                            {
                                return Results.Forbid();
                            }

                            var imageData = (image.ImageData, image.MimeType);
                            cache.Set(cacheKey, imageData, TimeSpan.FromMinutes(10));
                            return Results.File(image.ImageData, image.MimeType);
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
