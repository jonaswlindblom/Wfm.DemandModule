using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Wfm.DemandModule.Tests;

public class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiSmokeTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Swagger_IsReachable()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/swagger/index.html");
        Assert.True(resp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Can_Issue_Token()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/auth/token", new { userId = "jonas", role = "Admin" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.accessToken));
    }

    [Fact]
    public async Task Unauthorized_Stream_Create_IsDenied()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/streams", new { name = "X", sourceSystem = "POS", industry = "retail" });
        Assert.True((int)resp.StatusCode is 401 or 403);
    }

    private sealed record TokenResponse(string accessToken, DateTime expiresAtUtc);
}
