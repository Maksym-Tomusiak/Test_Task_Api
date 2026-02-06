using Application.Common.Interfaces.Services.Emails;
using Application.Common.Models;
using FluentAssertions;
using Infrastructure.Services.Emails;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Unit.Services;

public class EmailBackgroundServiceTests
{
    private readonly Mock<IBackgroundEmailQueue> _emailQueueMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly EmailBackgroundService _sut;

    public EmailBackgroundServiceTests()
    {
        _emailQueueMock = new Mock<IBackgroundEmailQueue>();
        _emailServiceMock = new Mock<IEmailService>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();

        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEmailService)))
            .Returns(_emailServiceMock.Object);

        serviceScopeMock
            .Setup(x => x.ServiceProvider)
            .Returns(serviceProviderMock.Object);

        _scopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(serviceScopeMock.Object);

        _sut = new EmailBackgroundService(_emailQueueMock.Object, _scopeFactoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDequeueAndSendEmail()
    {
        // Arrange
        var emailMessage = new EmailMessage(
            "test@example.com",
            "Test Subject",
            "Test Body",
            false);

        var cts = new CancellationTokenSource();
        
        _emailQueueMock
            .SetupSequence(x => x.DequeueEmailAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailMessage)
            .Throws(new OperationCanceledException());

        // Act
        var executeTask = _sut.StartAsync(cts.Token);
        await Task.Delay(100); // Give time to process
        cts.Cancel();
        await _sut.StopAsync(CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendEmail(
                emailMessage.ToEmail,
                emailMessage.Subject,
                emailMessage.Body,
                emailMessage.IsHtml),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldContinueProcessing_WhenEmailSendingFails()
    {
        // Arrange
        var email1 = new EmailMessage("test1@example.com", "Subject 1", "Body 1", false);
        var email2 = new EmailMessage("test2@example.com", "Subject 2", "Body 2", false);

        var cts = new CancellationTokenSource();

        _emailQueueMock
            .SetupSequence(x => x.DequeueEmailAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(email1)
            .ReturnsAsync(email2)
            .Throws(new OperationCanceledException());

        _emailServiceMock
            .Setup(x => x.SendEmail(email1.ToEmail, email1.Subject, email1.Body, email1.IsHtml))
            .ThrowsAsync(new Exception("Send failed"));

        _emailServiceMock
            .Setup(x => x.SendEmail(email2.ToEmail, email2.Subject, email2.Body, email2.IsHtml))
            .Returns(Task.CompletedTask);

        // Act
        var executeTask = _sut.StartAsync(cts.Token);
        await Task.Delay(6000); // Wait for retry delay (5 seconds + buffer)
        cts.Cancel();
        await _sut.StopAsync(CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendEmail(email2.ToEmail, email2.Subject, email2.Body, email2.IsHtml),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopGracefully_WhenCancellationRequested()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        _emailQueueMock
            .Setup(x => x.DequeueEmailAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        await _sut.StartAsync(cts.Token);
        cts.Cancel();
        var act = async () => await _sut.StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateNewScope_ForEachEmail()
    {
        // Arrange
        var email1 = new EmailMessage("test1@example.com", "Subject 1", "Body 1", false);
        var email2 = new EmailMessage("test2@example.com", "Subject 2", "Body 2", false);

        var cts = new CancellationTokenSource();

        _emailQueueMock
            .SetupSequence(x => x.DequeueEmailAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(email1)
            .ReturnsAsync(email2)
            .Throws(new OperationCanceledException());

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await _sut.StopAsync(CancellationToken.None);

        // Assert
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleHtmlEmails()
    {
        // Arrange
        var htmlEmail = new EmailMessage(
            "test@example.com",
            "HTML Subject",
            "<html><body>Test</body></html>",
            true);

        var cts = new CancellationTokenSource();

        _emailQueueMock
            .SetupSequence(x => x.DequeueEmailAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(htmlEmail)
            .Throws(new OperationCanceledException());

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await _sut.StopAsync(CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendEmail(
                htmlEmail.ToEmail,
                htmlEmail.Subject,
                htmlEmail.Body,
                true),
            Times.Once);
    }
}
