namespace Domain.Invites;

public record InviteId(Guid Value)
{
    public static InviteId Empty() => new(Guid.Empty);
    public static InviteId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}