using Microsoft.AspNetCore.Identity;

namespace Domain.Users;

public class User : IdentityUser<Guid>
{
    public DateTime DeleteRequestedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime RegisteredAt { get; set; }

    public void Delete()
    {
        DeleteRequestedAt = DateTime.UtcNow;
        IsDeleted = true;
    }
    
    public void Restore()
    {
        DeleteRequestedAt = DateTime.MinValue;
        IsDeleted = false;
    }
}