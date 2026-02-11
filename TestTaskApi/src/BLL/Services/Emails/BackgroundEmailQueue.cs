using System.Threading.Channels;
using BLL.Interfaces.Emails;
using BLL.Models;

namespace BLL.Services.Emails;

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