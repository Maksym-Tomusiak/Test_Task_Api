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
using Wolverine;

namespace Api.Controllers;

[ApiController]
public class InvitesController(IMessageBus messageBus) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost("api/invites")]
    public async Task<IResult> CreateInvite(CreateInviteDto request, CancellationToken cancellationToken)
    {
        var cmd = new InviteUserCommand(request.Email);
        var res = await messageBus.InvokeAsync<Either<UserException, string>>(cmd, cancellationToken);

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
        var query = new GetAllInvitesQuery(pageNumber, pageSize);
        var result = await messageBus.InvokeAsync<PaginatedResult<Invite>>(query, cancellationToken);
        var mappedResult = PaginatedResult<Invite>.MapFrom(result, InviteDto.FromDomainModel);
        
        return Results.Ok(mappedResult);
    }

    [HttpGet("api/invites/{code}")]
    public async Task<IResult> GetInviteByCode(Guid code, CancellationToken cancellationToken)
    {
        var query = new GetInviteByCodeQuery(code);
        var result = await messageBus.InvokeAsync<Option<Invite>>(query, cancellationToken);

        return result.Match<IResult>(
            invite => Results.Ok(InviteDto.FromDomainModel(invite)),
            () => Results.NotFound(new { error = "Invite not found" })
        );
    }
}
