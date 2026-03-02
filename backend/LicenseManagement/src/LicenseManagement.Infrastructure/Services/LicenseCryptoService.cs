using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LicenseManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LicenseManagement.Infrastructure.Services;

public class LicenseCryptoService : ILicenseCryptoService
{
    private readonly string _masterKey;

    public LicenseCryptoService(IConfiguration configuration)
    {
        _masterKey = configuration["Security:MasterEncryptionKey"]
            ?? throw new InvalidOperationException("MasterEncryptionKey not configured");
    }

    public (string publicKey, string privateKeyEncrypted) GenerateKeyPair()
    {
        using var ed25519 = ECDsa.Create(ECCurve.NamedCurves.nistP256); // Using ECDSA P-256 as Ed25519 alternative
        var privateKeyBytes = ed25519.ExportECPrivateKey();
        var publicKeyBytes = ed25519.ExportSubjectPublicKeyInfo();

        var encryptedPrivateKey = EncryptPrivateKey(privateKeyBytes);
        var publicKey = Convert.ToBase64String(publicKeyBytes);

        return (publicKey, encryptedPrivateKey);
    }

    public string SignLicense(LicensePayload payload, string encryptedPrivateKey)
    {
        var privateKeyBytes = DecryptPrivateKey(encryptedPrivateKey);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportECPrivateKey(privateKeyBytes, out _);

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var payloadBase64 = Base64UrlEncode(payloadBytes);

        var signature = ecdsa.SignData(payloadBytes, HashAlgorithmName.SHA256);
        var signatureBase64 = Base64UrlEncode(signature);

        return $"{payloadBase64}.{signatureBase64}";
    }

    public LicensePayload? VerifyLicense(string token, string publicKey)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2) return null;

            var payloadBytes = Base64UrlDecode(parts[0]);
            var signatureBytes = Base64UrlDecode(parts[1]);
            var publicKeyBytes = Convert.FromBase64String(publicKey);

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            if (!ecdsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256))
                return null;

            return JsonSerializer.Deserialize<LicensePayload>(payloadBytes);
        }
        catch
        {
            return null;
        }
    }

    public string EncryptPrivateKey(byte[] privateKey)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKey(_masterKey);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(privateKey, 0, privateKey.Length);

        // Prepend IV to encrypted data
        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }

    public byte[] DecryptPrivateKey(string encryptedPrivateKey)
    {
        var data = Convert.FromBase64String(encryptedPrivateKey);

        using var aes = Aes.Create();
        aes.Key = DeriveKey(_masterKey);

        var iv = new byte[16];
        Buffer.BlockCopy(data, 0, iv, 0, 16);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 16, data.Length - 16);
    }

    private static byte[] DeriveKey(string masterKey)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(masterKey));
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}
