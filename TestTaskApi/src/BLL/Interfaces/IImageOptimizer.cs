namespace BLL.Interfaces;

public interface IImageOptimizer
{
    Task<(byte[] Bytes, string MimeType)> OptimizeAsync(Stream inputStream, CancellationToken ct);
}