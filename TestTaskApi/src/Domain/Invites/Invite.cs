namespace Domain.Invites;

public class Invite
{
    public InviteId Id { get; set; }
    public Guid Code { get; set; }
    public string Email { get; set; }
    public bool IsUsed { get; set; }
    public DateTime ExpiresAt { get; set; }
    
    private Invite(InviteId id, Guid code, string email, bool isUsed, DateTime expiresAt)
    {
        Id = id;
        Code = code;
        Email = email;
        IsUsed = isUsed;
        ExpiresAt = expiresAt;
    }
    
    public static Invite New(string email, DateTime expiresAt) =>
        new(InviteId.New(), Guid.NewGuid(), email, false, expiresAt);
    
    public void MarkAsUsed() => IsUsed = true;
}