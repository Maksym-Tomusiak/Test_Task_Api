using BLL.Interfaces;
using SkiaSharp;

namespace BLL.Services;

public class SkiaCaptchaService : ICaptchaService
{
    public (string Code, byte[] ImageBytes) GenerateCaptcha()
    {
        const int width = 120;
        const int height = 50;
        var code = GenerateRandomString(4);

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        
        canvas.Clear(SKColors.White);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            TextSize = 30,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        var rand = new Random();

        using var noisePaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true, StrokeWidth = 1 };
        for (int i = 0; i < 10; i++)
        {
            canvas.DrawLine(rand.Next(width), rand.Next(height), rand.Next(width), rand.Next(height), noisePaint);
        }

        float x = 10;
        foreach (char c in code)
        {
            float y = rand.Next(30, 45);
            canvas.DrawText(c.ToString(), x, y, paint);
            x += 25;
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        
        return (code, data.ToArray());
    }

    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}