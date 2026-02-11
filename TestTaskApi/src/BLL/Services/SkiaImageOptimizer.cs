using BLL.Interfaces;
using SkiaSharp;

namespace BLL.Services;

public class SkiaImageOptimizer : IImageOptimizer
{
    private const int MaxWidth = 1024;
    private const int MaxHeight = 1024;
    private const int Quality = 75;

    public async Task<(byte[] Bytes, string MimeType)> OptimizeAsync(Stream inputStream, CancellationToken ct)
    {
        using var memoryStream = new MemoryStream();
        await inputStream.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        using var originalBitmap = SKBitmap.Decode(memoryStream);
        if (originalBitmap == null)
        {
            throw new Exception("Не вдалося розпізнати формат зображення.");
        }

        var (newWidth, newHeight) = CalculateNewDimensions(originalBitmap.Width, originalBitmap.Height);

        using var resizedBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.Medium);

        using var image = SKImage.FromBitmap(resizedBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, Quality);

        return (data.ToArray(), "image/jpeg");
    }

    private (int width, int height) CalculateNewDimensions(int currentWidth, int currentHeight)
    {
        if (currentWidth <= MaxWidth && currentHeight <= MaxHeight)
        {
            return (currentWidth, currentHeight);
        }

        var ratioX = (double)MaxWidth / currentWidth;
        var ratioY = (double)MaxHeight / currentHeight;
        var ratio = Math.Min(ratioX, ratioY);

        return ((int)(currentWidth * ratio), (int)(currentHeight * ratio));
    }
}