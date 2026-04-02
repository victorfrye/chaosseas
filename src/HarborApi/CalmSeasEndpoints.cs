using System.Net.Http.Json;

using Microsoft.AspNetCore.Http.HttpResults;

namespace VictorFrye.ChaosSeas.HarborApi;

public static class CalmSeasEndpoints
{
    internal record VoyageReport(
        string Voyage,
        string Status,
        SeaConditionsResponse? Conditions,
        string Message,
        DateTimeOffset Timestamp);

    public static IServiceCollection AddCalmSeasServices(this IServiceCollection services)
    {
        services.AddHttpClient("CalmSeas", static client =>
        {
            client.BaseAddress = new Uri("https+http://sea-conditions-api");
        })
        .AddStandardResilienceHandler();

        return services;
    }

    public static IEndpointRouteBuilder MapCalmSeasEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/voyage/calm-seas")
            .WithTags("Calm Seas");

        group.MapGet("/", async (IHttpClientFactory factory, CancellationToken cancellationToken) =>
        {
            HttpClient client = factory.CreateClient("CalmSeas");

            try
            {
                SeaConditionsResponse? conditions = await client.GetFromJsonAsync<SeaConditionsResponse>(
                    "/conditions/unreliable", cancellationToken);

                return Results.Ok(new VoyageReport(
                    Voyage: "Calm Seas",
                    Status: "Success",
                    Conditions: conditions,
                    Message: "The standard resilience handler kept our voyage on course!",
                    Timestamp: DateTimeOffset.UtcNow));
            }
            catch (HttpRequestException)
            {
                return Results.Json(new VoyageReport(
                    Voyage: "Calm Seas",
                    Status: "Failed",
                    Conditions: null,
                    Message: "Even with resilience, the seas proved too rough. The circuit breaker has opened!",
                    Timestamp: DateTimeOffset.UtcNow),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetCalmSeasVoyage")
        .WithDescription("Demonstrates AddStandardResilienceHandler protecting against an unreliable downstream service.");

        return endpoints;
    }
}
