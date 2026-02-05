using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Tests.Unit.Services;

public class AesCryptoServiceTests
{
    private readonly AesCryptoService _sut;
    private readonly string _testKey;

    public AesCryptoServiceTests()
    {
        // Generate a valid 256-bit (32-byte) key for AES
        var keyBytes = new byte[32];
        Random.Shared.NextBytes(keyBytes);
        _testKey = Convert.ToBase64String(keyBytes);

        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x["Encryption:Key"]).Returns(_testKey);

        _sut = new AesCryptoService(configurationMock.Object);
    }

    [Fact]
    public void Encrypt_ShouldReturnEncryptedDataAndIV_WhenGivenPlainText()
    {
        // Arrange
        var plainText = "Test message for encryption";

        // Act
        var (encryptedData, iv) = _sut.Encrypt(plainText);

        // Assert
        encryptedData.Should().NotBeEmpty();
        iv.Should().NotBeEmpty();
        iv.Should().HaveCount(16); // AES IV is always 16 bytes
        encryptedData.Should().NotBeEquivalentTo(System.Text.Encoding.UTF8.GetBytes(plainText));
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginalPlainText_WhenGivenEncryptedData()
    {
        // Arrange
        var plainText = "Test message for encryption and decryption";
        var (encryptedData, iv) = _sut.Encrypt(plainText);

        // Act
        var decryptedText = _sut.Decrypt(encryptedData, iv);

        // Assert
        decryptedText.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_ShouldProduceDifferentIVs_ForConsecutiveCalls()
    {
        // Arrange
        var plainText = "Same message";

        // Act
        var (_, iv1) = _sut.Encrypt(plainText);
        var (_, iv2) = _sut.Encrypt(plainText);

        // Assert
        iv1.Should().NotBeEquivalentTo(iv2);
    }

    [Fact]
    public void Encrypt_ShouldProduceDifferentEncryptedData_ForConsecutiveCalls()
    {
        // Arrange
        var plainText = "Same message";

        // Act
        var (encryptedData1, _) = _sut.Encrypt(plainText);
        var (encryptedData2, _) = _sut.Encrypt(plainText);

        // Assert
        encryptedData1.Should().NotBeEquivalentTo(encryptedData2);
    }

    [Fact]
    public void Encrypt_ShouldHandleEmptyString()
    {
        // Arrange
        var plainText = string.Empty;

        // Act
        var (encryptedData, iv) = _sut.Encrypt(plainText);
        var decryptedText = _sut.Decrypt(encryptedData, iv);

        // Assert
        decryptedText.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var plainText = "Special chars: !@#$%^&*()_+-={}[]|\\:;<>?,./~`";

        // Act
        var (encryptedData, iv) = _sut.Encrypt(plainText);
        var decryptedText = _sut.Decrypt(encryptedData, iv);

        // Assert
        decryptedText.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_ShouldHandleUnicodeCharacters()
    {
        // Arrange
        var plainText = "Unicode: 你好世界 مرحبا العالم שלום עולם";

        // Act
        var (encryptedData, iv) = _sut.Encrypt(plainText);
        var decryptedText = _sut.Decrypt(encryptedData, iv);

        // Assert
        decryptedText.Should().Be(plainText);
    }
}
