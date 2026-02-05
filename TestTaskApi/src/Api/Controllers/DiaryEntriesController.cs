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
using Wolverine;

namespace Api.Controllers;

[ApiController]
[Authorize]
public class DiaryEntriesController(IMessageBus messageBus, ICryptoService cryptoService) : ControllerBase
{
    [HttpGet("api/diary-entries")]
    public async Task<IResult> GetDiaryEntries(CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.FindFirst("id")?.Value;
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new GetAllDiaryEntriesByUserIdQuery(Guid.Parse(userId));
        var entries = await messageBus.InvokeAsync<IReadOnlyList<DiaryEntry>>(query, cancellationToken);
        
        var dtos = entries.Select(e =>
        {
            var decryptedContent = cryptoService.Decrypt(e.EncryptedContent, e.InitializationVector);
            return DiaryEntryDto.FromDomainModel(e, decryptedContent);
        });

        return Results.Ok(dtos);
    }

    [HttpGet("api/diary-entries/{id}")]
    public async Task<IResult> GetDiaryEntry(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetDiaryEntryByIdQuery(id);
        var result = await messageBus.InvokeAsync<Option<DiaryEntry>>(query, cancellationToken);

        return result.Match<IResult>(
            entry =>
            {
                var userId = HttpContext.User.FindFirst("id")?.Value;
                if (userId == null || entry.UserId.ToString() != userId)
                {
                    return Results.Forbid();
                }

                var decryptedContent = cryptoService.Decrypt(entry.EncryptedContent, entry.InitializationVector);
                return Results.Ok(DiaryEntryDto.FromDomainModel(entry, decryptedContent));
            },
            () => Results.NotFound(new { error = "Diary entry not found" })
        );
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
            entry => Results.Ok(new { message = "Diary entry deleted successfully" }),
            ex => ex.ToIResult());
    }
}
