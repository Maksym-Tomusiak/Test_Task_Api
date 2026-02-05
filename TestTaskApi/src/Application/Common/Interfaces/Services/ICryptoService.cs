namespace Application.Common.Interfaces.Services;

public interface ICryptoService
{
    (byte[] EncryptedData, byte[] IV) Encrypt(string plainText);
    string Decrypt(byte[] encryptedData, byte[] iv);
}