using FluentAssertions;
using LicenseManagement.Infrastructure.Services;

namespace LicenseManagement.UnitTests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldReturnNonEmptyHash()
    {
        var hash = _hasher.HashPassword("MyPassword123");
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPassword_ShouldReturnDifferentHashEachTime()
    {
        var hash1 = _hasher.HashPassword("same-password");
        var hash2 = _hasher.HashPassword("same-password");

        hash1.Should().NotBe(hash2); // Different salt
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var password = "Str0ng!Pass@word";
        var hash = _hasher.HashPassword(password);

        _hasher.VerifyPassword(password, hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        var hash = _hasher.HashPassword("correct-password");

        _hasher.VerifyPassword("wrong-password", hash).Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ShouldNotThrow()
    {
        var hash = _hasher.HashPassword("something");

        _hasher.VerifyPassword("", hash).Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ShouldReturnFalse()
    {
        _hasher.VerifyPassword("password", "not-valid-base64!!!").Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithTruncatedHash_ShouldReturnFalse()
    {
        // Hash that's valid base64 but wrong length
        var shortHash = Convert.ToBase64String(new byte[16]);
        _hasher.VerifyPassword("password", shortHash).Should().BeFalse();
    }
}
