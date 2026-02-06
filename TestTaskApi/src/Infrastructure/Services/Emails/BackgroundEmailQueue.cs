using System.Threading.Channels;
using Application.Common.Interfaces.Services.Emails;
using Application.Common.Models;

namespace Infrastructure.Services.Emails;

public class BackgroundEmailQueue : IBackgroundEmailQueue
{
    private readonly Channel<EmailMessage> _channel;

    public BackgroundEmailQueue()
    {
        var options = new BoundedChannelOptions(capacity: 100)
        {
            FullMode = BoundedChannelFullMode.Wait, 
            SingleReader = true
        };
        
        _channel = Channel.CreateBounded<EmailMessage>(options);
    }

    public async Task QueueEmail(EmailMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }
        await _channel.Writer.WriteAsync(message);
    }

    public async ValueTask<EmailMessage> DequeueEmailAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}