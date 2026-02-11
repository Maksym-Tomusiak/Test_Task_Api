using BLL.Interfaces.CRUD;
using BLL.Services.CRUD;
using DAL.Repositories.Interfaces.Queries;
using Domain.Invites;
using FluentAssertions;
using FluentAssertions.Primitives;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Tests.Unit.Services;

public class InviteServiceTests
{
    private readonly Mock<IInviteQueries> _mockInviteQueries;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly InviteService _service;

    public InviteServiceTests()
    {
        _mockInviteQueries = new Mock<IInviteQueries>();
        _mockCache = new Mock<IMemoryCache>();

        _service = new InviteService(_mockInviteQueries.Object, _mockCache.Object);
    }

    [Fact]
    public async Task GetByCodeAsync_WhenInviteExists_ReturnsInvite()
    {
        // Arrange
        var code = Guid.NewGuid();
        var email = "test@example.com";
        var invite = Invite.New(email, DateTime.UtcNow.AddDays(7));

        _mockInviteQueries
            .Setup(x => x.GetByCode(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option<Invite>.Some(invite));

        // Act
        var result = await _service.GetByCodeAsync(code, CancellationToken.None);

        // Assert
        result.IsSome.Should().BeTrue();
        result.Match<Either<bool, AndConstraint<StringAssertions>>>(
            inv => inv.Email.Should().Be(email),
            () => false
        );
    }

    [Fact]
    public async Task GetByCodeAsync_WhenInviteDoesNotExist_ReturnsNone()
    {
        // Arrange
        var code = Guid.NewGuid();

        _mockInviteQueries
            .Setup(x => x.GetByCode(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option<Invite>.None);

        // Act
        var result = await _service.GetByCodeAsync(code, CancellationToken.None);

        // Assert
        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public void InvalidateInvitesCache_WhenTokenExists_CancelsAndRemovesToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        object cacheEntry = cts;

        _mockCache
            .Setup(x => x.TryGetValue("Invites_Reset_Token", out cacheEntry))
            .Returns(true);

        // Act
        _service.InvalidateInvitesCache();

        // Assert
        _mockCache.Verify(x => x.Remove("Invites_Reset_Token"), Times.Once);
    }

    [Fact]
    public void InvalidateInvitesCache_WhenTokenDoesNotExist_DoesNothing()
    {
        // Arrange
        object? cacheEntry = null;

        _mockCache
            .Setup(x => x.TryGetValue("Invites_Reset_Token", out cacheEntry))
            .Returns(false);

        // Act
        _service.InvalidateInvitesCache();

        // Assert
        _mockCache.Verify(x => x.Remove(It.IsAny<object>()), Times.Never);
    }
}
