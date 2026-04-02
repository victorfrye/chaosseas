using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.Http.Resilience;
using Microsoft.FeatureManagement;

using Polly;
using Polly.Simmy;
using Polly.Simmy.Fault;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;

namespace VictorFrye.ChaosSeas.HarborApi;

public static class GameDayEndpoints
{
    internal record VoyageReport(
        string Voyage,
        string Status,
        SeaConditionsResponse? Conditions,
        string Message,
        long ElapsedMilliseconds,
        DateTimeOffset Timestamp);

    internal record GameDayStatus(
        bool ChaosEnabled,
        bool ChaosFault,
        bool ChaosLatency,
        bool ChaosOutcome,
        string Message);

    public static IServiceCollection AddGameDayServices(this IServiceCollection services)
    {
        services.AddFeatureManagement();

        services.AddHttpClient("GameDay", static client =>
        {
            client.BaseAddress = new Uri("https+http://sea-conditions-api");
        })
        .AddResilienceHandler("game-day-chaos", (builder, context) =>
        {
            IFeatureManager featureManager = context.ServiceProvider.GetRequiredService<IFeatureManager>();

            // Standard resilience first
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(500),
                UseJitter = true
            });

            builder.AddTimeout(TimeSpan.FromSeconds(15));

            // Chaos: fault injection — gated by ChaosEnabled AND ChaosFault flags
            builder.AddChaosFault(new ChaosFaultStrategyOptions
            {
                InjectionRate = 0.5,
                EnabledGenerator = async _ =>
                    await featureManager.IsEnabledAsync("ChaosEnabled")
                    && await featureManager.IsEnabledAsync("ChaosFault"),
                FaultGenerator = static _ =>
                {
                    Exception fault = new HttpRequestException("Game day fault — a kraken attacks the ship!");
                    return ValueTask.FromResult<Exception?>(fault);
                }
            });

            // Chaos: latency injection — gated by ChaosEnabled AND ChaosLatency flags
            builder.AddChaosLatency(new ChaosLatencyStrategyOptions
            {
                InjectionRate = 0.5,
                Latency = TimeSpan.FromSeconds(3),
                EnabledGenerator = async _ =>
                    await featureManager.IsEnabledAsync("ChaosEnabled")
                    && await featureManager.IsEnabledAsync("ChaosLatency")
            });

            // Chaos: outcome injection — gated by ChaosEnabled AND ChaosOutcome flags
            builder.AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>
            {
                InjectionRate = 0.5,
                EnabledGenerator = async _ =>
                    await featureManager.IsEnabledAsync("ChaosEnabled")
                    && await featureManager.IsEnabledAsync("ChaosOutcome"),
                OutcomeGenerator = static _ =>
                {
                    HttpResponseMessage response = new(HttpStatusCode.InternalServerError)
                    {
                        ReasonPhrase = "Game day outcome — the treasure map was a fake!"
                    };
                    return ValueTask.FromResult<Outcome<HttpResponseMessage>?>(Outcome.FromResult(response));
                }
            });
        });

        return services;
    }

    public static IEndpointRouteBuilder MapGameDayEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/voyage/game-day")
            .WithTags("Game Day");

        group.MapGet("/", async (IHttpClientFactory factory, CancellationToken cancellationToken) =>
        {
            HttpClient client = factory.CreateClient("GameDay");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                HttpResponseMessage response = await client.GetAsync("/conditions", cancellationToken);
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    SeaConditionsResponse? conditions = await response.Content
                        .ReadFromJsonAsync<SeaConditionsResponse>(cancellationToken);

                    return Results.Ok(new VoyageReport(
                        Voyage: "Game Day",
                        Status: "Success",
                        Conditions: conditions,
                        Message: "The crew survived the game day exercise!",
                        ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                        Timestamp: DateTimeOffset.UtcNow));
                }
                else
                {
                    return Results.Json(new VoyageReport(
                        Voyage: "Game Day",
                        Status: "Failed",
                        Conditions: null,
                        Message: $"Game day chaos struck! HTTP {(int)response.StatusCode} — {response.ReasonPhrase}",
                        ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                        Timestamp: DateTimeOffset.UtcNow),
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                stopwatch.Stop();
                return Results.Json(new VoyageReport(
                    Voyage: "Game Day",
                    Status: "Failed",
                    Conditions: null,
                    Message: $"Game day chaos struck! {ex.Message}",
                    ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                    Timestamp: DateTimeOffset.UtcNow),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetGameDayVoyage")
        .WithDescription("Demonstrates feature-flag-controlled chaos injection for game day exercises.");

        group.MapGet("/status", async (IFeatureManager featureManager) =>
        {
            bool chaosEnabled = await featureManager.IsEnabledAsync("ChaosEnabled");
            bool chaosFault = await featureManager.IsEnabledAsync("ChaosFault");
            bool chaosLatency = await featureManager.IsEnabledAsync("ChaosLatency");
            bool chaosOutcome = await featureManager.IsEnabledAsync("ChaosOutcome");

            string message = chaosEnabled
                ? "⚠️ Game day is ACTIVE — chaos strategies are armed!"
                : "✅ All clear — chaos is disabled. Safe for production.";

            return Results.Ok(new GameDayStatus(
                ChaosEnabled: chaosEnabled,
                ChaosFault: chaosFault,
                ChaosLatency: chaosLatency,
                ChaosOutcome: chaosOutcome,
                Message: message));
        })
        .WithName("GetGameDayStatus")
        .WithDescription("Returns the current state of all chaos feature flags.");

        return endpoints;
    }
}
