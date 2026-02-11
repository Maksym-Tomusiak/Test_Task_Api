namespace BLL.Interfaces;

public interface ICryptoService
{
    (byte[] EncryptedData, byte[] IV) Encrypt(string plainText);
    string Decrypt(byte[] encryptedData, byte[] iv);
}