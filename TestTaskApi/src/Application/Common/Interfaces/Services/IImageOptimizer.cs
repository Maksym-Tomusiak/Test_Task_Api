namespace Application.Common.Interfaces.Services;

public interface IImageOptimizer
{
    Task<(byte[] Bytes, string MimeType)> OptimizeAsync(Stream inputStream, CancellationToken ct);
}