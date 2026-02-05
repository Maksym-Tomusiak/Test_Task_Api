using Domain.Invites;

namespace Api.Dtos;

public record InviteDto(
    Guid Id,
    Guid Code,
    string Email,
    bool IsUsed,
    DateTime ExpiresAt)
{
    public static InviteDto FromDomainModel(Invite invite)
        => new(invite.Id.Value, invite.Code, invite.Email, invite.IsUsed, invite.ExpiresAt);
}

public record CreateInviteDto(string Email);
