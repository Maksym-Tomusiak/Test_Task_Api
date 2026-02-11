using System.Security.Cryptography;
using BLL.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BLL.Services;

public class AesCryptoService : ICryptoService
{
    private readonly byte[] _key;

    public AesCryptoService(IConfiguration configuration)
    {
        var keyString = configuration["Encryption:Key"]; 
        _key = Convert.FromBase64String(keyString);
    }

    public (byte[] EncryptedData, byte[] IV) Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return (ms.ToArray(), aes.IV);
    }

    public string Decrypt(byte[] encryptedData, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream(encryptedData);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }
}