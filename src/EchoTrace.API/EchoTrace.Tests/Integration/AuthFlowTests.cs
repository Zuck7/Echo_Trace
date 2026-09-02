using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace EchoTrace.Tests.Integration;

public sealed class AuthFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthFlowTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record RegisterRequest(string Email, string Password, string OrgLegalName, string OrgCountry);
    private record LoginRequest(string Email, string Password);
    private record LoginResponse(string AccessToken, int ExpiresIn, string TokenType);
    private record OrgResponse(Guid OrgId, string LegalName);

    [Fact]
    public async Task Register_then_login_returns_a_usable_access_token()
    {
        var email = $"{Guid.NewGuid()}@acme.test";
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "SecureP@ss123", "Acme Test Corp", "US"));
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "SecureP@ss123"));
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await login.Content.ReadFromJsonAsync<LoginResponse>();
        token!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var email = $"{Guid.NewGuid()}@acme.test";
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "SecureP@ss123", "Acme Test Corp", "US"));

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "WrongPassword"));

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Registering_the_same_email_twice_is_rejected()
    {
        var email = $"{Guid.NewGuid()}@acme.test";
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "SecureP@ss123", "Acme Test Corp", "US"));

        var second = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "AnotherP@ss123", "Different Corp", "GB"));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Anonymous_requests_to_protected_endpoints_are_rejected()
    {
        var response = await _client.GetAsync("/api/v1/organizations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_tenant_cannot_see_another_tenants_organizations()
    {
        var tokenA = await RegisterAndLogin("Tenant A Corp");
        var orgIdB = await RegisterAndGetOrgId("Tenant B Corp");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);

        var listResponse = await _client.GetFromJsonAsync<List<OrgResponse>>("/api/v1/organizations");
        listResponse.Should().NotContain(o => o.OrgId == orgIdB);

        var directLookup = await _client.GetAsync($"/api/v1/organizations/{orgIdB}");
        directLookup.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    private async Task<string> RegisterAndLogin(string orgName)
    {
        var email = $"{Guid.NewGuid()}@acme.test";
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "SecureP@ss123", orgName, "US"));
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "SecureP@ss123"));
        var token = await login.Content.ReadFromJsonAsync<LoginResponse>();
        return token!.AccessToken;
    }

    private async Task<Guid> RegisterAndGetOrgId(string orgName)
    {
        var email = $"{Guid.NewGuid()}@acme.test";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "SecureP@ss123", orgName, "US"));
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return Guid.Parse(body!["orgId"].ToString()!);
    }
}
