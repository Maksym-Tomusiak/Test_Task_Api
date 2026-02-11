using BLL.Services;
using FluentAssertions;
using SkiaSharp;

namespace Tests.Unit.Services;

public class SkiaImageOptimizerTests
{
    private readonly SkiaImageOptimizer _sut;

    public SkiaImageOptimizerTests()
    {
        _sut = new SkiaImageOptimizer();
    }

    private static Stream CreateTestImage(int width, int height, SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        
        canvas.Clear(SKColors.Red);
        
        using var paint = new SKPaint { Color = SKColors.Blue };
        canvas.DrawRect(10, 10, width - 20, height - 20, paint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 100);
        
        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task OptimizeAsync_ShouldReturnJpegMimeType()
    {
        // Arrange
        using var inputStream = CreateTestImage(800, 600);

        // Act
        var (_, mimeType) = await _sut.OptimizeAsync(inputStream, CancellationToken.None);

        // Assert
        mimeType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task OptimizeAsync_ShouldReturnNonEmptyBytes()
    {
        // Arrange
        using var inputStream = CreateTestImage(800, 600);

        // Act
        var (bytes, _) = await _sut.OptimizeAsync(inputStream, CancellationToken.None);

        // Assert
        bytes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task OptimizeAsync_ShouldReduceImageSize_WhenImageIsLargerThanMaxDimensions()
    {
        // Arrange
        var largeWidth = 2048;
        var largeHeight = 1536;
        using var inputStream = CreateTestImage(largeWidth, largeHeight);

        // Act
        var (bytes, _) = await _sut.OptimizeAsync(inputStream, CancellationToken.None);

        // Assert
        using var outputBitmap = SKBitmap.Decode(bytes);
        outputBitmap.Width.Should().BeLessOrEqualTo(1024);
        outputBitmap.Height.Should().BeLessOrEqualTo(1024);
    }

    [Fact]
    public async Task OptimizeAsync_ShouldMaintainAspectRatio()
    {
        // Arrange
        var originalWidth = 2000;
        var originalHeight = 1000;
        using var inputStream = CreateTestImage(originalWidth, originalHeight);
        var originalAspectRatio = (double)originalWidth / originalHeight;

        // Act
        var (bytes, _) = await _sut.OptimizeAsync(inputStream, CancellationToken.None);

        // Assert
        using var outputBitmap = SKBitmap.Decode(bytes);
        var newAspectRatio = (double)outputBitmap.Width / outputBitmap.Height;
        Math.Abs(originalAspectRatio - newAspectRatio).Should().BeLessThan(0.01);
    }

    [Fact]
    public async Task OptimizeAsync_ShouldNotIncreaseSize_WhenImageIsAlreadySmall()
    {
        // Arrange
        var smallWidth = 500;
        var smallHeight = 400;
        using var inputStream = CreateTestImage(smallWidth, smallHeight);

        // Act
        var (bytes, _) = await _sut.OptimizeAsync(inputStream, CancellationToken.None);

        // Assert
        using var outputBitmap = SKBitmap.Decode(bytes);
        outputBitmap.Width.Should().Be(smallWidth);
        outputBitmap.Height.Should().Be(smallHeight);
    }

    [Fact]
    public async Task OptimizeAsync_ShouldHandleSquareImages()
    {
        // Arrange
        using var inputStream = CreateTestImage(1500, 1500);

        // Act
        var (bytes, _) = await _sut.OptimizeAsync(inputStream, CancellationToken.None);

        // Assert
        using var outputBitmap = SKBitmap.Decode(bytes);
        outputBitmap.Width.Should().Be(1024);
        outputBitmap.Height.Should().Be(1024);
    }

    [Fact]
    public async Task OptimizeAsync_ShouldHandlePortraitOrientation()
    {
        // Arrange
        using var inputStream = CreateTestImage(800, 1600);

        // Act
        var (bytes, _) = await _sut.OptimizeAsync(inputStream, CancellationToken.None);

        // Assert
        using var outputBitmap = SKBitmap.Decode(bytes);
        outputBitmap.Width.Should().Be(512);
        outputBitmap.Height.Should().Be(1024);
    }

    [Fact]
    public async Task OptimizeAsync_ShouldHandleLandscapeOrientation()
    {
        // Arrange
        using var inputStream = CreateTestImage(1600, 800);

        // Act
        var (bytes, _) = await _sut.OptimizeAsync(inputStream, CancellationToken.None);

        // Assert
        using var outputBitmap = SKBitmap.Decode(bytes);
        outputBitmap.Width.Should().Be(1024);
        outputBitmap.Height.Should().Be(512);
    }

    [Fact]
    public async Task OptimizeAsync_ShouldThrowException_WhenStreamContainsInvalidImage()
    {
        // Arrange
        var invalidStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        var act = async () => await _sut.OptimizeAsync(invalidStream, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task OptimizeAsync_ShouldHandleDifferentImageFormats()
    {
        // Arrange
        using var jpegStream = CreateTestImage(800, 600, SKEncodedImageFormat.Jpeg);

        // Act
        var (bytes, mimeType) = await _sut.OptimizeAsync(jpegStream, CancellationToken.None);

        // Assert
        bytes.Should().NotBeNullOrEmpty();
        mimeType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task OptimizeAsync_ShouldReduceFileSize()
    {
        // Arrange
        using var inputStream = CreateTestImage(2048, 2048, SKEncodedImageFormat.Png);
        var originalSize = inputStream.Length;

        // Act
        var (bytes, _) = await _sut.OptimizeAsync(inputStream, CancellationToken.None);

        // Assert
        ((long)bytes.Length).Should().BeLessThan(originalSize);
    }
}
