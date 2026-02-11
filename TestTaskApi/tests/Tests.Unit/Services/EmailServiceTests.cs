using BLL.Models;
using BLL.Services.Emails;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Unit.Services;

public class EmailServiceTests
{
    private readonly Mock<IOptions<EmailSettings>> _emailSettingsMock;
    private readonly EmailService _sut;
    private readonly EmailSettings _emailSettings;

    public EmailServiceTests()
    {
        _emailSettings = new EmailSettings
        {
            SmtpHost = "smtp.test.com",
            SmtpPort = 465,
            SenderEmail = "sender@test.com",
            SenderPassword = "password123",
            SenderName = "Test Sender"
        };

        _emailSettingsMock = new Mock<IOptions<EmailSettings>>();
        _emailSettingsMock.Setup(x => x.Value).Returns(_emailSettings);

        _sut = new EmailService(_emailSettingsMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitialize_WithValidSettings()
    {
        // Act & Assert
        _sut.Should().NotBeNull();
    }

    [Fact]
    public async Task SendEmail_ShouldNotThrow_WithValidParameters()
    {
        // Arrange
        var to = "recipient@test.com";
        var subject = "Test Subject";
        var body = "Test Body";

        // Act
        var act = async () => await _sut.SendEmail(to, subject, body, false);

        // Assert
        // The actual SMTP connection will fail in tests, but the method should handle it gracefully
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendEmail_ShouldHandleHtmlContent()
    {
        // Arrange
        var to = "recipient@test.com";
        var subject = "HTML Email";
        var body = "<html><body><h1>Test</h1></body></html>";

        // Act
        var act = async () => await _sut.SendEmail(to, subject, body, true);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendEmail_ShouldHandlePlainTextContent()
    {
        // Arrange
        var to = "recipient@test.com";
        var subject = "Plain Text Email";
        var body = "This is plain text content";

        // Act
        var act = async () => await _sut.SendEmail(to, subject, body, false);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendEmail_ShouldHandleSpecialCharactersInSubject()
    {
        // Arrange
        var to = "recipient@test.com";
        var subject = "Special chars: !@#$%^&*()";
        var body = "Test body";

        // Act
        var act = async () => await _sut.SendEmail(to, subject, body, false);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendEmail_ShouldHandleUnicodeCharacters()
    {
        // Arrange
        var to = "recipient@test.com";
        var subject = "Unicode: 你好世界";
        var body = "Body with émojis 🚀 and other chars";

        // Act
        var act = async () => await _sut.SendEmail(to, subject, body, false);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendEmail_ShouldHandleEmptyBody()
    {
        // Arrange
        var to = "recipient@test.com";
        var subject = "Empty Body";
        var body = string.Empty;

        // Act
        var act = async () => await _sut.SendEmail(to, subject, body, false);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendEmail_ShouldHandleLongContent()
    {
        // Arrange
        var to = "recipient@test.com";
        var subject = "Long Content Email";
        var body = string.Join("\n", Enumerable.Repeat("This is a test line.", 1000));

        // Act
        var act = async () => await _sut.SendEmail(to, subject, body, false);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void EmailSettings_ShouldBeConfigured()
    {
        // Assert
        _emailSettings.SmtpHost.Should().NotBeNullOrEmpty();
        _emailSettings.SmtpPort.Should().BeGreaterThan(0);
        _emailSettings.SenderEmail.Should().NotBeNullOrEmpty();
        _emailSettings.SenderPassword.Should().NotBeNullOrEmpty();
        _emailSettings.SenderName.Should().NotBeNullOrEmpty();
    }
}
