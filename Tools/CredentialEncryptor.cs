using System;
using System.Security.Cryptography;
using System.Text;

namespace WsUtaSystem.Tools;

/// <summary>
/// Herramienta de línea de comandos para encriptar credenciales
/// Uso: dotnet run --project Tools/CredentialEncryptor.cs -- --key "YOUR_32_CHAR_KEY" --username "user" --password "pass" --domain "DOMAIN"
/// </summary>
class CredentialEncryptor
{
    static void Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  Credential Encryptor - AES-256");
        Console.WriteLine("===========================================\n");

        // Parsear argumentos
        string? encryptionKey = GetArgument(args, "--key");
        string? username = GetArgument(args, "--username");
        string? password = GetArgument(args, "--password");
        string? domain = GetArgument(args, "--domain");

        if (string.IsNullOrEmpty(encryptionKey))
        {
            Console.WriteLine("ERROR: --key is required");
            ShowUsage();
            return;
        }

        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password) && string.IsNullOrEmpty(domain))
        {
            Console.WriteLine("ERROR: At least one of --username, --password, or --domain is required");
            ShowUsage();
            return;
        }

        // Ajustar clave a 32 caracteres
        if (encryptionKey.Length < 32)
        {
            encryptionKey = encryptionKey.PadRight(32, '0');
        }
        else if (encryptionKey.Length > 32)
        {
            encryptionKey = encryptionKey.Substring(0, 32);
        }

        Console.WriteLine($"Encryption Key: {encryptionKey}\n");

        // Encriptar valores
        if (!string.IsNullOrEmpty(username))
        {
            string encryptedUsername = Encrypt(username, encryptionKey);
            Console.WriteLine($"Username (plain):     {username}");
            Console.WriteLine($"Username (encrypted): {encryptedUsername}\n");
        }

        if (!string.IsNullOrEmpty(password))
        {
            string encryptedPassword = Encrypt(password, encryptionKey);
            Console.WriteLine($"Password (plain):     {password}");
            Console.WriteLine($"Password (encrypted): {encryptedPassword}\n");
        }

        if (!string.IsNullOrEmpty(domain))
        {
            string encryptedDomain = Encrypt(domain, encryptionKey);
            Console.WriteLine($"Domain (plain):     {domain}");
            Console.WriteLine($"Domain (encrypted): {encryptedDomain}\n");
        }

        Console.WriteLine("===========================================");
        Console.WriteLine("  Add to appsettings.json:");
        Console.WriteLine("===========================================");
        Console.WriteLine("{");
        Console.WriteLine("  \"FileManagement\": {");
        Console.WriteLine($"    \"EncryptionKey\": \"{encryptionKey}\",");
        Console.WriteLine("    \"NetworkCredentials\": {");
        
        if (!string.IsNullOrEmpty(username))
            Console.WriteLine($"      \"Username\": \"{Encrypt(username, encryptionKey)}\",");
        
        if (!string.IsNullOrEmpty(password))
            Console.WriteLine($"      \"Password\": \"{Encrypt(password, encryptionKey)}\",");
        
        if (!string.IsNullOrEmpty(domain))
            Console.WriteLine($"      \"Domain\": \"{Encrypt(domain, encryptionKey)}\"");
        
        Console.WriteLine("    }");
        Console.WriteLine("  }");
        Console.WriteLine("}");
    }

    static string Encrypt(string plainText, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(keyBytes);
        var iv = hash.Take(16).ToArray();

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = iv;
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

    static string? GetArgument(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }

    static void ShowUsage()
    {
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  dotnet run --project Tools/CredentialEncryptor.cs -- --key <32_char_key> --username <user> --password <pass> [--domain <domain>]");
        Console.WriteLine("\nExample:");
        Console.WriteLine("  dotnet run --project Tools/CredentialEncryptor.cs -- --key \"MySecretKey123456789012345678\" --username \"nas_user\" --password \"nas_pass123\" --domain \"WORKGROUP\"");
        Console.WriteLine("\nArguments:");
        Console.WriteLine("  --key       (required) Encryption key (32 characters)");
        Console.WriteLine("  --username  (optional) Username to encrypt");
        Console.WriteLine("  --password  (optional) Password to encrypt");
        Console.WriteLine("  --domain    (optional) Domain to encrypt");
    }
}

