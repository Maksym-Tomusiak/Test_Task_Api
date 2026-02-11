using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using BLL.Modules.Exceptions;
using BLL.Services.CRUD;
using DAL.Repositories.Interfaces.Queries;
using DAL.Repositories.Interfaces.Repositories;
using Domain.DiaryEntries;
using Domain.EntryImages;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace Tests.Unit.Services;

public class DiaryEntryServiceTests
{
    private readonly Mock<IDiaryEntryRepository> _mockDiaryEntryRepository;
    private readonly Mock<IDiaryEntryQueries> _mockDiaryEntryQueries;
    private readonly Mock<IEntryImageRepository> _mockImageRepository;
    private readonly Mock<IEntryImageQueries> _mockImageQueries;
    private readonly Mock<ICryptoService> _mockCryptoService;
    private readonly Mock<IImageOptimizer> _mockImageOptimizer;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly DiaryEntryService _service;

    public DiaryEntryServiceTests()
    {
        _mockDiaryEntryRepository = new Mock<IDiaryEntryRepository>();
        _mockDiaryEntryQueries = new Mock<IDiaryEntryQueries>();
        _mockImageRepository = new Mock<IEntryImageRepository>();
        _mockImageQueries = new Mock<IEntryImageQueries>();
        _mockCryptoService = new Mock<ICryptoService>();
        _mockImageOptimizer = new Mock<IImageOptimizer>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        _service = new DiaryEntryService(
            _mockDiaryEntryRepository.Object,
            _mockDiaryEntryQueries.Object,
            _mockImageRepository.Object,
            _mockImageQueries.Object,
            _mockCryptoService.Object,
            _mockImageOptimizer.Object,
            _mockHttpContextAccessor.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntryExists_ReturnsEntryWithImageId()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var entry = DiaryEntry.New(userId, new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }, DateTime.UtcNow, true);
        var image = EntryImage.New(entry.Id, new byte[] { 7, 8, 9 }, "image/png");

        _mockDiaryEntryQueries
            .Setup(x => x.GetById(It.IsAny<DiaryEntryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option<DiaryEntry>.Some(entry));

        _mockImageQueries
            .Setup(x => x.GetByEntryId(It.IsAny<DiaryEntryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option<EntryImage>.Some(image));

        // Act
        var result = await _service.GetByIdAsync(entryId, CancellationToken.None);

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            entryWithImage =>
            {
                entryWithImage.Entry.Should().Be(entry);
                entryWithImage.ImageId.Should().NotBeNull();
                return true;
            },
            () => false
        ).Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntryDoesNotExist_ReturnsNone()
    {
        // Arrange
        var entryId = Guid.NewGuid();

        _mockDiaryEntryQueries
            .Setup(x => x.GetById(It.IsAny<DiaryEntryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option<DiaryEntry>.None);

        // Act
        var result = await _service.GetByIdAsync(entryId, CancellationToken.None);

        // Assert
        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var content = "Test content";
        var encryptedData = new byte[] { 1, 2, 3 };
        var iv = new byte[] { 4, 5, 6 };

        SetupHttpContext(userId);

        _mockCryptoService
            .Setup(x => x.Encrypt(content))
            .Returns((encryptedData, iv));

        _mockDiaryEntryRepository
            .Setup(x => x.Add(It.IsAny<DiaryEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiaryEntry entry, CancellationToken ct) => entry);

        // Act
        var result = await _service.CreateAsync(content, null, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            entry =>
            {
                entry.UserId.Should().Be(userId);
                entry.EncryptedContent.Should().BeEquivalentTo(encryptedData);
                entry.InitializationVector.Should().BeEquivalentTo(iv);
                return entry;
            },
            ex => null!
        ).Should().NotBeNull();

        _mockDiaryEntryRepository.Verify(x => x.Add(It.IsAny<DiaryEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutUserId_ReturnsUnauthorizedError()
    {
        // Arrange
        SetupHttpContext(null);

        // Act
        var result = await _service.CreateAsync("Test content", null, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.Match(
            entry => null,
            ex => ex
        ).Should().BeOfType<UnauthorizedDiaryEntryAccessException>();

        _mockDiaryEntryRepository.Verify(x => x.Add(It.IsAny<DiaryEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenEntryCannotBeDeleted_ReturnsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var oldEntry = DiaryEntry.New(userId, new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }, DateTime.UtcNow.AddDays(-3), false);

        SetupHttpContext(userId);

        _mockDiaryEntryQueries
            .Setup(x => x.GetById(It.IsAny<DiaryEntryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option<DiaryEntry>.Some(oldEntry));

        // Act
        var result = await _service.DeleteAsync(entryId, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.Match(
            entry => null,
            ex => ex
        ).Should().BeOfType<DiaryEntryEntryCannotBeDeletedException>();

        _mockDiaryEntryRepository.Verify(x => x.Delete(It.IsAny<DiaryEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_WithSearchTerm_FiltersCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entry1 = DiaryEntry.New(userId, new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }, DateTime.UtcNow, false);
        var entry2 = DiaryEntry.New(userId, new byte[] { 7, 8, 9 }, new byte[] { 10, 11, 12 }, DateTime.UtcNow, false);
        var entries = new List<DiaryEntry> { entry1, entry2 };

        _mockDiaryEntryQueries
            .Setup(x => x.GetAllByUserId(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        _mockCryptoService
            .Setup(x => x.Decrypt(entry1.EncryptedContent, entry1.InitializationVector))
            .Returns("Hello World");

        _mockCryptoService
            .Setup(x => x.Decrypt(entry2.EncryptedContent, entry2.InitializationVector))
            .Returns("Test content");

        // Act
        var result = await _service.GetAllByUserIdAsync(userId, 1, 10, "Hello", null, null, CancellationToken.None);

        // Assert
        result.Items.Count.Should().Be(1);
        result.TotalCount.Should().Be(1);
    }

    private void SetupHttpContext(Guid? userId)
    {
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            var claims = new List<Claim>
            {
                new Claim("id", userId.Value.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            httpContext.User = claimsPrincipal;
        }

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
    }
}
