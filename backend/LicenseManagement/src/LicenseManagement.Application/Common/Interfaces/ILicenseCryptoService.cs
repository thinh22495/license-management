namespace LicenseManagement.Application.Common.Interfaces;

public interface ILicenseCryptoService
{
    (string publicKey, string privateKeyEncrypted) GenerateKeyPair();
    string SignLicense(LicensePayload payload, string encryptedPrivateKey);
    LicensePayload? VerifyLicense(string token, string publicKey);
    string EncryptPrivateKey(byte[] privateKey);
    byte[] DecryptPrivateKey(string encryptedPrivateKey);
}

public class LicensePayload
{
    public string Lid { get; set; } = string.Empty; // license ID
    public string Pid { get; set; } = string.Empty; // product ID
    public string Uid { get; set; } = string.Empty; // user ID
    public string Tier { get; set; } = string.Empty;
    public string[] Features { get; set; } = [];
    public int MaxAct { get; set; }
    public long Iat { get; set; } // issued at (unix timestamp)
    public long Exp { get; set; } // expiry (unix timestamp)
    public string Hwid { get; set; } = string.Empty; // hardware ID
}
