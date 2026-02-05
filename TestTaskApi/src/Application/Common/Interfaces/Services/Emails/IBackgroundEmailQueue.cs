using Application.Common.Models;

namespace Application.Common.Interfaces.Services.Emails;

public interface IBackgroundEmailQueue
{
    Task QueueEmail(EmailMessage message);

    ValueTask<EmailMessage> DequeueEmailAsync(CancellationToken cancellationToken);
}