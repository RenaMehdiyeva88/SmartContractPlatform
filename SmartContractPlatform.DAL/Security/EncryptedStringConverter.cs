using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SmartContractPlatform.DAL.Security;

public class EncryptedStringConverter : ValueConverter<string, string>
{
    // NOT BENE: açarı appsettings/Key Vault-dan oxu, kodda saxlama!
    public EncryptedStringConverter(string base64Key)
        : base(
            v => Encrypt(v, ValidateKey(base64Key)),
            v => Decrypt(v, ValidateKey(base64Key)))
    {
    }

    private static byte[] ValidateKey(string base64Key)
    {
        try
        {
            var key = Convert.FromBase64String(base64Key);
            if (key.Length is 16 or 24 or 32)
            {
                return key;
            }
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Encryption:Key must be a valid Base64-encoded AES key.", exception);
        }

        throw new InvalidOperationException("Encryption:Key must decode to 16, 24, or 32 bytes for AES.");
    }

    private static string Encrypt(string plainText, byte[] key)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = CreateDeterministicIv(plainText, key);

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // IV + cipher birlikdə saxlanılır ki, decrypt zamanı lazım olsun
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    private static string Decrypt(string cipherText, byte[] key)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        byte[] fullCipher;

        try
        {
            fullCipher = Convert.FromBase64String(cipherText);
        }
        catch (FormatException)
        {
            return cipherText;
        }

        if (fullCipher.Length <= 16)
        {
            return cipherText;
        }

        using var aes = Aes.Create();
        aes.Key = key;

        var iv = new byte[16];
        var cipherBytes = new byte[fullCipher.Length - 16];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
        Buffer.BlockCopy(fullCipher, 16, cipherBytes, 0, cipherBytes.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        byte[] plainBytes;
        try
        {
            plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        }
        catch (CryptographicException)
        {
            return cipherText;
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] CreateDeterministicIv(string plainText, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(plainText))[..16];
    }
}