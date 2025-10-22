using System.Security.Cryptography;
using System.Text;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Servicio de encriptación/desencriptación usando AES-256
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IConfiguration configuration)
    {
        // Leer clave de encriptación desde configuración
        var encryptionKey = configuration["FileManagement:EncryptionKey"] 
            ?? throw new InvalidOperationException("FileManagement:EncryptionKey not configured in appsettings.json");

        // La clave debe ser de 32 bytes (256 bits) para AES-256
        if (encryptionKey.Length < 32)
        {
            encryptionKey = encryptionKey.PadRight(32, '0');
        }
        else if (encryptionKey.Length > 32)
        {
            encryptionKey = encryptionKey.Substring(0, 32);
        }

        _key = Encoding.UTF8.GetBytes(encryptionKey);
        
        // IV fijo derivado de la clave (en producción, considerar IV aleatorio por valor)
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(_key);
        _iv = hash.Take(16).ToArray(); // AES requiere IV de 16 bytes
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        var buffer = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var msDecrypt = new MemoryStream(buffer);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }
}

