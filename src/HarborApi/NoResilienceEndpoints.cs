using System.Net.Http.Json;

using Microsoft.AspNetCore.Http.HttpResults;

namespace VictorFrye.ChaosSeas.HarborApi;

public static class NoResilienceEndpoints
{
    internal record VoyageReport(
        string Voyage,
        string Status,
        SeaConditionsResponse? Conditions,
        string Message,
        DateTimeOffset Timestamp);

    public static IServiceCollection AddNoResilienceServices(this IServiceCollection services)
    {
        services.AddHttpClient("NoResilience", static client =>
        {
            client.BaseAddress = new Uri("https+http://sea-conditions-api");
        });

        return services;
    }

    public static IEndpointRouteBuilder MapNoResilienceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/voyage/no-resilience")
            .WithTags("No Resilience");

        group.MapGet("/", async (IHttpClientFactory factory, CancellationToken cancellationToken) =>
        {
            HttpClient client = factory.CreateClient("NoResilience");

            try
            {
                SeaConditionsResponse? conditions = await client.GetFromJsonAsync<SeaConditionsResponse>(
                    "/conditions", cancellationToken);

                return Results.Ok(new VoyageReport(
                    Voyage: "No Resilience",
                    Status: "Success",
                    Conditions: conditions,
                    Message: "The ship returned — but only by luck. No resilience pipeline protected this voyage!",
                    Timestamp: DateTimeOffset.UtcNow));
            }
            catch (HttpRequestException)
            {
                return Results.Json(new VoyageReport(
                    Voyage: "No Resilience",
                    Status: "Failed",
                    Conditions: null,
                    Message: "Ship lost at sea! Without retries or circuit breakers, a single failure sinks the voyage.",
                    Timestamp: DateTimeOffset.UtcNow),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetNoResilienceVoyage")
        .WithDescription("Demonstrates what happens without any Polly resilience pipeline — no retries, no circuit breaker, no timeout.");

        return endpoints;
    }
}
