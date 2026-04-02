using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using VictorFrye.ChaosSeas.HarborApi;

namespace VictorFrye.ChaosSeas.HarborApi.Tests;

public class GameDayTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GameDayTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GameDayStatus_ReturnsOk()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/voyage/game-day/status", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GameDayStatus_DefaultFlags_AllDisabled()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/voyage/game-day/status", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        JsonDocument doc = JsonDocument.Parse(content);
        JsonElement root = doc.RootElement;

        Assert.False(root.GetProperty("chaosEnabled").GetBoolean());
        Assert.False(root.GetProperty("chaosFault").GetBoolean());
        Assert.False(root.GetProperty("chaosLatency").GetBoolean());
        Assert.False(root.GetProperty("chaosOutcome").GetBoolean());
    }

    [Fact]
    public async Task GameDayStatus_WhenChaosDisabled_MessageIndicatesSafe()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/voyage/game-day/status", TestContext.Current.CancellationToken);

        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("disabled", content, StringComparison.OrdinalIgnoreCase);
    }
}
