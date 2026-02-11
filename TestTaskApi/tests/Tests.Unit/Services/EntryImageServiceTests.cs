using BLL.Interfaces;
using BLL.Interfaces.CRUD;
using BLL.Services.CRUD;
using DAL.Repositories.Interfaces.Queries;
using Domain.DiaryEntries;
using Domain.EntryImages;
using FluentAssertions;
using LanguageExt;
using Moq;

namespace Tests.Unit.Services;

public class EntryImageServiceTests
{
    private readonly Mock<IEntryImageQueries> _mockEntryImageQueries;
    private readonly EntryImageService _service;

    public EntryImageServiceTests()
    {
        _mockEntryImageQueries = new Mock<IEntryImageQueries>();
        _service = new EntryImageService(_mockEntryImageQueries.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenImageExists_ReturnsImage()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var entryId = new DiaryEntryId(Guid.NewGuid());
        var imageData = new byte[] { 1, 2, 3, 4, 5 };
        var mimeType = "image/png";
        var image = EntryImage.New(entryId, imageData, mimeType);

        _mockEntryImageQueries
            .Setup(x => x.GetById(It.IsAny<EntryImageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option<EntryImage>.Some(image));

        // Act
        var result = await _service.GetByIdAsync(imageId, CancellationToken.None);

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            img =>
            {
                img.ImageData.Should().BeEquivalentTo(imageData);
                img.MimeType.Should().Be(mimeType);
                return true;
            },
            () => false
        ).Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WhenImageDoesNotExist_ReturnsNone()
    {
        // Arrange
        var imageId = Guid.NewGuid();

        _mockEntryImageQueries
            .Setup(x => x.GetById(It.IsAny<EntryImageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option<EntryImage>.None);

        // Act
        var result = await _service.GetByIdAsync(imageId, CancellationToken.None);

        // Assert
        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetByEntryIdAsync_WhenImageExists_ReturnsImage()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var diaryEntryId = new DiaryEntryId(entryId);
        var imageData = new byte[] { 1, 2, 3, 4, 5 };
        var mimeType = "image/jpeg";
        var image = EntryImage.New(diaryEntryId, imageData, mimeType);

        _mockEntryImageQueries
            .Setup(x => x.GetByEntryId(It.IsAny<DiaryEntryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option<EntryImage>.Some(image));

        // Act
        var result = await _service.GetByEntryIdAsync(entryId, CancellationToken.None);

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match(
            img =>
            {
                img.ImageData.Should().BeEquivalentTo(imageData);
                img.MimeType.Should().Be(mimeType);
                return true;
            },
            () => false
        ).Should().BeTrue();
    }

    [Fact]
    public async Task GetByEntryIdAsync_WhenImageDoesNotExist_ReturnsNone()
    {
        // Arrange
        var entryId = Guid.NewGuid();

        _mockEntryImageQueries
            .Setup(x => x.GetByEntryId(It.IsAny<DiaryEntryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option<EntryImage>.None);

        // Act
        var result = await _service.GetByEntryIdAsync(entryId, CancellationToken.None);

        // Assert
        result.IsNone.Should().BeTrue();
    }
}
