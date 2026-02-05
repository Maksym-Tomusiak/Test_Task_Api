using Application.Common.Interfaces.Queries;
using Application.EntryImages.Queries;
using Domain.EntryImages;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Api.Controllers;

[ApiController]
[Authorize]
public class EntryImagesController(IMessageBus messageBus, IDiaryEntryQueries diaryEntryQueries) : ControllerBase
{
    [HttpGet("api/entry-images/{id}")]
    public async Task<IResult> GetEntryImage(Guid id, CancellationToken cancellationToken)
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
                        var userId = HttpContext.User.FindFirst("id")?.Value;
                        if (userId == null || entry.UserId.ToString() != userId)
                        {
                            return Results.Forbid();
                        }

                        return Results.File(image.ImageData, image.MimeType);
                    },
                    () => Task.FromResult(Results.NotFound(new { error = "Entry not found" }))
                );
            },
            () => Task.FromResult(Results.NotFound(new { error = "Image not found" }))
        );
    }
}
