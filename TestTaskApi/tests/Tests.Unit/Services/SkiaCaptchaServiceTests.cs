using BLL.Services;
using FluentAssertions;

namespace Tests.Unit.Services;

public class SkiaCaptchaServiceTests
{
    private readonly SkiaCaptchaService _sut;

    public SkiaCaptchaServiceTests()
    {
        _sut = new SkiaCaptchaService();
    }

    [Fact]
    public void GenerateCaptcha_ShouldReturnCodeAndImage()
    {
        // Act
        var (code, imageBytes) = _sut.GenerateCaptcha();

        // Assert
        code.Should().NotBeNullOrEmpty();
        imageBytes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateCaptcha_ShouldReturnCodeWithCorrectLength()
    {
        // Act
        var (code, _) = _sut.GenerateCaptcha();

        // Assert
        code.Should().HaveLength(4);
    }

    [Fact]
    public void GenerateCaptcha_ShouldReturnCodeWithValidCharacters()
    {
        // Arrange
        const string validChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        // Act
        var (code, _) = _sut.GenerateCaptcha();

        // Assert
        code.Should().NotBeNullOrEmpty();
        code.All(c => validChars.Contains(c)).Should().BeTrue();
    }

    [Fact]
    public void GenerateCaptcha_ShouldGenerateDifferentCodes()
    {
        // Act
        var (code1, _) = _sut.GenerateCaptcha();
        var (code2, _) = _sut.GenerateCaptcha();
        var (code3, _) = _sut.GenerateCaptcha();

        // Assert
        // At least one should be different (extremely high probability)
        var allSame = code1 == code2 && code2 == code3;
        allSame.Should().BeFalse();
    }

    [Fact]
    public void GenerateCaptcha_ShouldReturnValidPngImage()
    {
        // Act
        var (_, imageBytes) = _sut.GenerateCaptcha();

        // Assert
        imageBytes.Should().NotBeNullOrEmpty();
        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
        imageBytes.Should().HaveCountGreaterThan(8);
        imageBytes[0].Should().Be(0x89);
        imageBytes[1].Should().Be(0x50);
        imageBytes[2].Should().Be(0x4E);
        imageBytes[3].Should().Be(0x47);
    }

    [Fact]
    public void GenerateCaptcha_ShouldGenerateDifferentImages()
    {
        // Act
        var (_, imageBytes1) = _sut.GenerateCaptcha();
        var (_, imageBytes2) = _sut.GenerateCaptcha();

        // Assert
        imageBytes1.Should().NotBeEquivalentTo(imageBytes2);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void GenerateCaptcha_ShouldGenerateMultipleCaptchas_WithoutError(int count)
    {
        // Act
        var action = () =>
        {
            for (int i = 0; i < count; i++)
            {
                var (code, imageBytes) = _sut.GenerateCaptcha();
                code.Should().NotBeNullOrEmpty();
                imageBytes.Should().NotBeNullOrEmpty();
            }
        };

        // Assert
        action.Should().NotThrow();
    }
}
