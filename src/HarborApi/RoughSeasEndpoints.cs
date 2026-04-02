using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.Http.Resilience;

using Polly;
using Polly.Retry;
using Polly.Simmy;
using Polly.Simmy.Fault;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;

namespace VictorFrye.ChaosSeas.HarborApi;

public static class RoughSeasEndpoints
{
    internal record VoyageReport(
        string Voyage,
        string Status,
        SeaConditionsResponse? Conditions,
        string Message,
        long ElapsedMilliseconds,
        DateTimeOffset Timestamp);

    public static IServiceCollection AddRoughSeasServices(this IServiceCollection services)
    {
        // Fault injection client — throws HttpRequestException 50% of the time
        services.AddHttpClient("RoughSeas-Faults", static client =>
        {
            client.BaseAddress = new Uri("https+http://sea-conditions-api");
        })
        .AddResilienceHandler("fault-chaos", static builder =>
        {
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                BackoffType = DelayBackoffType.Constant,
                Delay = TimeSpan.FromMilliseconds(500),
                UseJitter = true
            });

            builder.AddTimeout(TimeSpan.FromSeconds(10));

            // Chaos: inject faults 50% of the time for demo visibility
            builder.AddChaosFault(new ChaosFaultStrategyOptions
            {
                InjectionRate = 0.5,
                FaultGenerator = static _ =>
                {
                    Exception fault = new HttpRequestException("Chaos fault injected — a rogue wave struck the hull!");
                    return ValueTask.FromResult<Exception?>(fault);
                }
            });
        });

        // Latency injection client — adds 3s delay 50% of the time
        services.AddHttpClient("RoughSeas-Latency", static client =>
        {
            client.BaseAddress = new Uri("https+http://sea-conditions-api");
        })
        .AddResilienceHandler("latency-chaos", static builder =>
        {
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                BackoffType = DelayBackoffType.Constant,
                Delay = TimeSpan.FromMilliseconds(500),
                UseJitter = true
            });

            builder.AddTimeout(TimeSpan.FromSeconds(10));

            // Chaos: inject latency 50% of the time for demo visibility
            builder.AddChaosLatency(new ChaosLatencyStrategyOptions
            {
                InjectionRate = 0.5,
                Latency = TimeSpan.FromSeconds(3)
            });
        });

        // Outcome injection client — returns HTTP 500 50% of the time
        services.AddHttpClient("RoughSeas-Outcomes", static client =>
        {
            client.BaseAddress = new Uri("https+http://sea-conditions-api");
        })
        .AddResilienceHandler("outcome-chaos", static builder =>
        {
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                BackoffType = DelayBackoffType.Constant,
                Delay = TimeSpan.FromMilliseconds(500),
                UseJitter = true
            });

            builder.AddTimeout(TimeSpan.FromSeconds(10));

            // Chaos: inject bad outcomes 50% of the time for demo visibility
            builder.AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>
            {
                InjectionRate = 0.5,
                OutcomeGenerator = static _ =>
                {
                    HttpResponseMessage response = new(HttpStatusCode.InternalServerError)
                    {
                        ReasonPhrase = "Chaos outcome injected — the ship's compass is spinning!"
                    };
                    return ValueTask.FromResult<Outcome<HttpResponseMessage>?>(Outcome.FromResult(response));
                }
            });
        });

        return services;
    }

    public static IEndpointRouteBuilder MapRoughSeasEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/voyage/rough-seas")
            .WithTags("Rough Seas");

        group.MapGet("/faults", async (IHttpClientFactory factory, CancellationToken cancellationToken) =>
        {
            HttpClient client = factory.CreateClient("RoughSeas-Faults");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                SeaConditionsResponse? conditions = await client.GetFromJsonAsync<SeaConditionsResponse>(
                    "/conditions", cancellationToken);

                stopwatch.Stop();
                return Results.Ok(new VoyageReport(
                    Voyage: "Rough Seas — Fault Injection",
                    Status: "Success",
                    Conditions: conditions,
                    Message: "The ship weathered the storm — no fault was injected this time!",
                    ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                    Timestamp: DateTimeOffset.UtcNow));
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                return Results.Json(new VoyageReport(
                    Voyage: "Rough Seas — Fault Injection",
                    Status: "Failed",
                    Conditions: null,
                    Message: $"Chaos struck! {ex.Message}",
                    ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                    Timestamp: DateTimeOffset.UtcNow),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetRoughSeasFaults")
        .WithDescription("Demonstrates Polly AddChaosFault — injects HttpRequestExceptions at 50% rate.");

        group.MapGet("/latency", async (IHttpClientFactory factory, CancellationToken cancellationToken) =>
        {
            HttpClient client = factory.CreateClient("RoughSeas-Latency");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                SeaConditionsResponse? conditions = await client.GetFromJsonAsync<SeaConditionsResponse>(
                    "/conditions", cancellationToken);

                stopwatch.Stop();
                string latencyMessage = stopwatch.ElapsedMilliseconds > 2000
                    ? "The voyage was sluggish — chaos latency was injected!"
                    : "Smooth sailing — no latency injected this time!";

                return Results.Ok(new VoyageReport(
                    Voyage: "Rough Seas — Latency Injection",
                    Status: "Success",
                    Conditions: conditions,
                    Message: latencyMessage,
                    ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                    Timestamp: DateTimeOffset.UtcNow));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                stopwatch.Stop();
                return Results.Json(new VoyageReport(
                    Voyage: "Rough Seas — Latency Injection",
                    Status: "Failed",
                    Conditions: null,
                    Message: $"The voyage timed out! {ex.Message}",
                    ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                    Timestamp: DateTimeOffset.UtcNow),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetRoughSeasLatency")
        .WithDescription("Demonstrates Polly AddChaosLatency — injects 3-second delays at 50% rate.");

        group.MapGet("/outcomes", async (IHttpClientFactory factory, CancellationToken cancellationToken) =>
        {
            HttpClient client = factory.CreateClient("RoughSeas-Outcomes");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            HttpResponseMessage response = await client.GetAsync("/conditions", cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                SeaConditionsResponse? conditions = await response.Content.ReadFromJsonAsync<SeaConditionsResponse>(cancellationToken);

                return Results.Ok(new VoyageReport(
                    Voyage: "Rough Seas — Outcome Injection",
                    Status: "Success",
                    Conditions: conditions,
                    Message: "The voyage returned safely — no chaos outcome was injected!",
                    ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                    Timestamp: DateTimeOffset.UtcNow));
            }
            else
            {
                return Results.Json(new VoyageReport(
                    Voyage: "Rough Seas — Outcome Injection",
                    Status: "Failed",
                    Conditions: null,
                    Message: $"Chaos outcome injected! Received HTTP {(int)response.StatusCode} — {response.ReasonPhrase}",
                    ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                    Timestamp: DateTimeOffset.UtcNow),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetRoughSeasOutcomes")
        .WithDescription("Demonstrates Polly AddChaosOutcome — injects HTTP 500 responses at 50% rate.");

        return endpoints;
    }
}
