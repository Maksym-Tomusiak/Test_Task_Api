using Application.Common.Models;
using FluentAssertions;
using Infrastructure.Services.Emails;

namespace Tests.Unit.Services;

public class BackgroundEmailQueueTests
{
    private readonly BackgroundEmailQueue _sut;

    public BackgroundEmailQueueTests()
    {
        _sut = new BackgroundEmailQueue();
    }

    [Fact]
    public async Task QueueEmail_ShouldAddEmailToQueue()
    {
        // Arrange
        var emailMessage = new EmailMessage(
            "test@example.com",
            "Test Subject",
            "Test Body",
            false);

        // Act
        await _sut.QueueEmail(emailMessage);

        // Assert
        var dequeuedMessage = await _sut.DequeueEmailAsync(CancellationToken.None);
        dequeuedMessage.Should().BeEquivalentTo(emailMessage);
    }

    [Fact]
    public async Task QueueEmail_ShouldThrowException_WhenMessageIsNull()
    {
        // Act
        var act = async () => await _sut.QueueEmail(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DequeueEmailAsync_ShouldReturnEmailsInFifoOrder()
    {
        // Arrange
        var email1 = new EmailMessage("test1@example.com", "Subject 1", "Body 1", false);
        var email2 = new EmailMessage("test2@example.com", "Subject 2", "Body 2", true);
        var email3 = new EmailMessage("test3@example.com", "Subject 3", "Body 3", false);

        await _sut.QueueEmail(email1);
        await _sut.QueueEmail(email2);
        await _sut.QueueEmail(email3);

        // Act & Assert
        var dequeued1 = await _sut.DequeueEmailAsync(CancellationToken.None);
        dequeued1.Should().BeEquivalentTo(email1);

        var dequeued2 = await _sut.DequeueEmailAsync(CancellationToken.None);
        dequeued2.Should().BeEquivalentTo(email2);

        var dequeued3 = await _sut.DequeueEmailAsync(CancellationToken.None);
        dequeued3.Should().BeEquivalentTo(email3);
    }

    [Fact]
    public async Task QueueEmail_ShouldHandleMultipleEmails()
    {
        // Arrange
        var emails = Enumerable.Range(1, 10)
            .Select(i => new EmailMessage(
                $"test{i}@example.com",
                $"Subject {i}",
                $"Body {i}",
                i % 2 == 0))
            .ToList();

        // Act
        foreach (var email in emails)
        {
            await _sut.QueueEmail(email);
        }

        // Assert
        for (int i = 0; i < emails.Count; i++)
        {
            var dequeuedEmail = await _sut.DequeueEmailAsync(CancellationToken.None);
            dequeuedEmail.Should().BeEquivalentTo(emails[i]);
        }
    }

    [Fact]
    public async Task DequeueEmailAsync_ShouldWaitForEmail_WhenQueueIsEmpty()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var email = new EmailMessage("test@example.com", "Subject", "Body", false);

        // Act
        var dequeueTask = _sut.DequeueEmailAsync(cts.Token);
        
        // Add delay to ensure dequeue is waiting
        await Task.Delay(100);
        
        await _sut.QueueEmail(email);
        var result = await dequeueTask;

        // Assert
        result.Should().BeEquivalentTo(email);
    }

    [Fact]
    public async Task DequeueEmailAsync_ShouldThrowOperationCanceledException_WhenCancellationRequested()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        var dequeueTask = _sut.DequeueEmailAsync(cts.Token);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await dequeueTask);
    }

    [Fact]
    public async Task QueueEmail_ShouldHandleEmailsWithHtmlContent()
    {
        // Arrange
        var htmlEmail = new EmailMessage(
            "test@example.com",
            "HTML Email",
            "<html><body><h1>Test</h1></body></html>",
            true);

        // Act
        await _sut.QueueEmail(htmlEmail);
        var result = await _sut.DequeueEmailAsync(CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(htmlEmail);
        result.IsHtml.Should().BeTrue();
    }

    [Fact]
    public async Task QueueEmail_ShouldHandleEmailsWithSpecialCharacters()
    {
        // Arrange
        var specialEmail = new EmailMessage(
            "test@example.com",
            "Special: !@#$%^&*()",
            "Body with 你好 and émojis 🚀",
            false);

        // Act
        await _sut.QueueEmail(specialEmail);
        var result = await _sut.DequeueEmailAsync(CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(specialEmail);
    }
}
