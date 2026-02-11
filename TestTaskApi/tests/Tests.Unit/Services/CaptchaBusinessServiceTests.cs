using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using BLL.Services.CRUD;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Tests.Unit.Services;

public class CaptchaBusinessServiceTests
{
    private readonly Mock<ICaptchaService> _mockCaptchaService;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly CaptchaBusinessService _service;

    public CaptchaBusinessServiceTests()
    {
        _mockCaptchaService = new Mock<ICaptchaService>();
        _mockCache = new Mock<IMemoryCache>();

        _service = new CaptchaBusinessService(_mockCache.Object, _mockCaptchaService.Object);
    }

    [Fact]
    public void GenerateCaptcha_ReturnsValidResult()
    {
        // Arrange
        var captchaCode = "ABC123";
        var captchaImageBytes = new byte[] { 1, 2, 3, 4, 5 };

        _mockCaptchaService
            .Setup(x => x.GenerateCaptcha())
            .Returns((captchaCode, captchaImageBytes));

        object? cacheEntry = null;
        _mockCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = _service.GenerateCaptcha();

        // Assert
        result.Should().NotBeNull();
        result.CaptchaId.Should().NotBeNullOrEmpty();
        result.CaptchaImageBase64.Should().NotBeNullOrEmpty();
        result.CaptchaImageBase64.Should().Be(Convert.ToBase64String(captchaImageBytes));
    }

    [Fact]
    public void GenerateCaptcha_StoresCaptchaInCache()
    {
        // Arrange
        var captchaCode = "XYZ789";
        var captchaImageBytes = new byte[] { 5, 4, 3, 2, 1 };

        _mockCaptchaService
            .Setup(x => x.GenerateCaptcha())
            .Returns((captchaCode, captchaImageBytes));

        var mockCacheEntry = new Mock<ICacheEntry>();
        mockCacheEntry.SetupProperty(e => e.Value);
        mockCacheEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);

        _mockCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(mockCacheEntry.Object);

        // Act
        var result = _service.GenerateCaptcha();

        // Assert
        _mockCache.Verify(x => x.CreateEntry(It.IsAny<string>()), Times.Once);
        mockCacheEntry.Object.Value.Should().Be(captchaCode);
        mockCacheEntry.Object.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void GenerateCaptcha_GeneratesUniqueCaptchaIds()
    {
        // Arrange
        var captchaCode = "TEST123";
        var captchaImageBytes = new byte[] { 1, 2, 3 };

        _mockCaptchaService
            .Setup(x => x.GenerateCaptcha())
            .Returns((captchaCode, captchaImageBytes));

        _mockCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        // Act
        var result1 = _service.GenerateCaptcha();
        var result2 = _service.GenerateCaptcha();

        // Assert
        result1.CaptchaId.Should().NotBe(result2.CaptchaId);
    }
}
