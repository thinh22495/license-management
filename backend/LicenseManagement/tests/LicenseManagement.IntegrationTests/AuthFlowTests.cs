using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace LicenseManagement.IntegrationTests;

public class AuthFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthFlowTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturn200()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"test_{Guid.NewGuid():N}@example.com",
            password = "Test@123456",
            fullName = "Test User",
            phone = "0900000001"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("data").GetProperty("user").GetProperty("email").GetString().Should().Contain("@example.com");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldFail()
    {
        var email = $"dup_{Guid.NewGuid():N}@example.com";

        // First registration
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Test@123456",
            fullName = "First User",
        });

        // Second with same email
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Test@123456",
            fullName = "Second User",
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200()
    {
        var email = $"login_{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        // Register first
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            fullName = "Login Test",
        });

        // Login
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldFail()
    {
        var email = $"wrong_{Guid.NewGuid():N}@example.com";

        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Correct@123",
            fullName = "Test",
        });

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "Wrong@123",
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ShouldReturn200()
    {
        var email = $"auth_{Guid.NewGuid():N}@example.com";

        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Test@123456",
            fullName = "Auth Test",
        });

        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regBody = await regResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = regBody.GetProperty("data").GetProperty("accessToken").GetString();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
