using Api.Dtos;
using Api.Modules.Errors;
using Application.Common.Models;
using Application.Invites.Queries;
using Application.Users.Commands;
using Application.Users.Exceptions;
using Domain.Invites;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Wolverine;

namespace Api.Controllers;

[ApiController]
public class InvitesController(IMessageBus messageBus, IMemoryCache cache) : ControllerBase
{
    private const string InvitesResetTokenKey = "Invites_Reset_Token";

    [Authorize(Roles = "Admin")]
    [HttpPost("api/invites")]
    public async Task<IResult> CreateInvite(CreateInviteDto request, CancellationToken cancellationToken)
    {
        var cmd = new InviteUserCommand(request.Email);
        var res = await messageBus.InvokeAsync<Either<UserException, string>>(cmd, cancellationToken);

        if (res.IsRight)
        {
            InvalidateInvitesList();
        }

        return res.Match<IResult>(
            msg => Results.Ok(new { message = msg }),
            ex => ex.ToIResult());
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("api/invites")]
    public async Task<IResult> GetInvites(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"invites_page_{pageNumber}_size_{pageSize}";

        var result = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AddExpirationToken(GetInvitesListResetToken());
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);

            var query = new GetAllInvitesQuery(pageNumber, pageSize);
            var domainResult = await messageBus.InvokeAsync<PaginatedResult<Invite>>(query, cancellationToken);
            
            return PaginatedResult<Invite>.MapFrom(domainResult, InviteDto.FromDomainModel);
        });

        return Results.Ok(result);
    }

    [HttpGet("api/invites/{code}")]
    public async Task<IResult> GetInviteByCode(Guid code, CancellationToken cancellationToken)
    {
        var cacheKey = $"invite_{code}";

        if (cache.TryGetValue(cacheKey, out InviteDto? cachedInvite))
        {
            return Results.Ok(cachedInvite);
        }

        var result = await messageBus.InvokeAsync<Option<Invite>>(new GetInviteByCodeQuery(code), cancellationToken);

        return result.Match<IResult>(
            invite =>
            {
                var dto = InviteDto.FromDomainModel(invite);
                cache.Set(cacheKey, dto, TimeSpan.FromMinutes(10));
                return Results.Ok(dto);
            },
            () => Results.NotFound(new { error = "Invite not found" }));
    }

    private IChangeToken GetInvitesListResetToken()
    {
        var cts = cache.GetOrCreate(InvitesResetTokenKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return new CancellationTokenSource();
        });

        return new CancellationChangeToken(cts!.Token);
    }

    private void InvalidateInvitesList()
    {
        if (cache.TryGetValue(InvitesResetTokenKey, out CancellationTokenSource? cts))
        {
            cts?.Cancel();
            cache.Remove(InvitesResetTokenKey);
        }
    }
}