using FluentAssertions;
using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using LicenseManagement.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace LicenseManagement.UnitTests.Services;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "ThisIsATestSecretKeyThatIsAtLeast32Characters!!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
            })
            .Build();

        _jwtService = new JwtService(config);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FullName = "Test User",
            Role = UserRole.User,
        };

        var token = _jwtService.GenerateAccessToken(user);

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT has 3 parts
    }

    [Fact]
    public void GenerateAccessToken_ShouldBeValidatable()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FullName = "Test User",
            Role = UserRole.Admin,
        };

        var token = _jwtService.GenerateAccessToken(user);
        var validatedId = _jwtService.ValidateAccessToken(token);

        validatedId.Should().Be(userId);
    }

    [Fact]
    public void ValidateAccessToken_WithInvalidToken_ShouldReturnNull()
    {
        var result = _jwtService.ValidateAccessToken("invalid.token.here");
        result.Should().BeNull();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyString()
    {
        var token = _jwtService.GenerateRefreshToken();
        token.Should().NotBeNullOrEmpty();
        token.Length.Should().BeGreaterThan(20);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldBeUniqueEachTime()
    {
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        token1.Should().NotBe(token2);
    }
}
