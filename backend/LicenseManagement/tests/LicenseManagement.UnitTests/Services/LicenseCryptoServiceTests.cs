using FluentAssertions;
using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace LicenseManagement.UnitTests.Services;

public class LicenseCryptoServiceTests
{
    private readonly ILicenseCryptoService _service;

    public LicenseCryptoServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:MasterEncryptionKey"] = "TestMasterKeyForUnitTests2024!!"
            })
            .Build();

        _service = new LicenseCryptoService(config);
    }

    [Fact]
    public void GenerateKeyPair_ShouldReturnValidKeys()
    {
        var (publicKey, privateKeyEncrypted) = _service.GenerateKeyPair();

        publicKey.Should().NotBeNullOrEmpty();
        privateKeyEncrypted.Should().NotBeNullOrEmpty();

        // Public key should be valid base64
        var publicKeyBytes = Convert.FromBase64String(publicKey);
        publicKeyBytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateKeyPair_ShouldReturnDifferentKeysEachTime()
    {
        var (publicKey1, _) = _service.GenerateKeyPair();
        var (publicKey2, _) = _service.GenerateKeyPair();

        publicKey1.Should().NotBe(publicKey2);
    }

    [Fact]
    public void SignAndVerify_ShouldRoundTrip()
    {
        var (publicKey, privateKeyEnc) = _service.GenerateKeyPair();

        var payload = new LicensePayload
        {
            Lid = Guid.NewGuid().ToString(),
            Pid = Guid.NewGuid().ToString(),
            Uid = Guid.NewGuid().ToString(),
            Tier = "Pro",
            Features = ["feature1", "feature2"],
            MaxAct = 3,
            Iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Exp = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds(),
            Hwid = "test-hardware-id",
        };

        var signedToken = _service.SignLicense(payload, privateKeyEnc);

        signedToken.Should().NotBeNullOrEmpty();
        signedToken.Should().Contain(".");

        // Verify
        var verified = _service.VerifyLicense(signedToken, publicKey);

        verified.Should().NotBeNull();
        verified!.Lid.Should().Be(payload.Lid);
        verified.Pid.Should().Be(payload.Pid);
        verified.Uid.Should().Be(payload.Uid);
        verified.Tier.Should().Be("Pro");
        verified.MaxAct.Should().Be(3);
        verified.Hwid.Should().Be("test-hardware-id");
    }

    [Fact]
    public void VerifyLicense_WithTamperedToken_ShouldReturnNull()
    {
        var (publicKey, privateKeyEnc) = _service.GenerateKeyPair();

        var payload = new LicensePayload
        {
            Lid = Guid.NewGuid().ToString(),
            Pid = Guid.NewGuid().ToString(),
            Uid = Guid.NewGuid().ToString(),
            Tier = "Basic",
            Iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Exp = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds(),
            Hwid = "hw-123",
        };

        var signedToken = _service.SignLicense(payload, privateKeyEnc);

        // Tamper with the payload part
        var parts = signedToken.Split('.');
        var tamperedToken = "AAAA" + parts[0][4..] + "." + parts[1];

        var verified = _service.VerifyLicense(tamperedToken, publicKey);
        verified.Should().BeNull();
    }

    [Fact]
    public void VerifyLicense_WithWrongPublicKey_ShouldReturnNull()
    {
        var (_, privateKeyEnc) = _service.GenerateKeyPair();
        var (wrongPublicKey, _) = _service.GenerateKeyPair();

        var payload = new LicensePayload
        {
            Lid = Guid.NewGuid().ToString(),
            Pid = Guid.NewGuid().ToString(),
            Uid = Guid.NewGuid().ToString(),
            Tier = "Pro",
            Iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Exp = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds(),
            Hwid = "hw-456",
        };

        var signedToken = _service.SignLicense(payload, privateKeyEnc);

        var verified = _service.VerifyLicense(signedToken, wrongPublicKey);
        verified.Should().BeNull();
    }

    [Fact]
    public void VerifyLicense_WithInvalidFormat_ShouldReturnNull()
    {
        var (publicKey, _) = _service.GenerateKeyPair();

        _service.VerifyLicense("not-a-valid-token", publicKey).Should().BeNull();
        _service.VerifyLicense("", publicKey).Should().BeNull();
        _service.VerifyLicense("a.b.c", publicKey).Should().BeNull();
    }

    [Fact]
    public void EncryptDecrypt_PrivateKey_ShouldRoundTrip()
    {
        var originalKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        var encrypted = _service.EncryptPrivateKey(originalKey);
        encrypted.Should().NotBeNullOrEmpty();

        var decrypted = _service.DecryptPrivateKey(encrypted);
        decrypted.Should().BeEquivalentTo(originalKey);
    }
}
