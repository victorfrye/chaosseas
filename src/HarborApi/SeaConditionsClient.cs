using System.Net.Http.Json;

namespace VictorFrye.ChaosSeas.HarborApi;

public class SeaConditionsClient(HttpClient httpClient)
{
    public async Task<SeaConditionsResponse?> GetConditionsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<SeaConditionsResponse>("/conditions", cancellationToken);
    }
}

public record SeaConditionsResponse(
    string State,
    double WindSpeedKnots,
    double WaveHeightMeters,
    double VisibilityNauticalMiles,
    double WaterTemperatureCelsius,
    string Description,
    DateTimeOffset Timestamp);
