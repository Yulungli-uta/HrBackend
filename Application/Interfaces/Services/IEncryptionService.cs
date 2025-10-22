namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Servicio de encriptación/desencriptación usando AES-256
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encripta un texto usando AES-256
    /// </summary>
    /// <param name="plainText">Texto a encriptar</param>
    /// <returns>Texto encriptado en Base64</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Desencripta un texto usando AES-256
    /// </summary>
    /// <param name="cipherText">Texto encriptado en Base64</param>
    /// <returns>Texto desencriptado</returns>
    string Decrypt(string cipherText);
}

