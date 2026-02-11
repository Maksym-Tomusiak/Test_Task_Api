using BLL.Models;

namespace BLL.Interfaces.Emails;

public interface IBackgroundEmailQueue
{
    Task QueueEmail(EmailMessage message);

    ValueTask<EmailMessage> DequeueEmailAsync(CancellationToken cancellationToken);
}