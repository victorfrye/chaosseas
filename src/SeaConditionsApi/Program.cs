using Microsoft.AspNetCore.Http.HttpResults;

using VictorFrye.ChaosSeas.Extensions.ServiceDefaults;
using VictorFrye.ChaosSeas.SeaConditionsApi;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Sea Conditions API";
        document.Info.Description = "API for reporting current sea conditions";
        return Task.CompletedTask;
    });
});

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.MapOpenApi();

app.MapGet("/conditions", () =>
{
    SeaCondition condition = GenerateCondition();
    return TypedResults.Ok(condition);
})
.WithName("GetSeaConditions")
.WithDescription("Returns a randomly generated sea condition report.");

app.MapGet("/conditions/unreliable", Results<Ok<SeaCondition>, ProblemHttpResult> () =>
{
    bool isUnreliable = Random.Shared.NextDouble() < 0.4;

    if (isUnreliable)
    {
        return TypedResults.Problem(
            detail: "The seas are too treacherous to report!",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    SeaCondition condition = GenerateCondition();
    return TypedResults.Ok(condition);
})
.WithName("GetSeaConditionsUnreliable")
.WithDescription("Returns a randomly generated sea condition report, but may fail unpredictably.");

app.UseHttpsRedirection();

await app.RunAsync();

static SeaCondition GenerateCondition()
{
    string[] descriptions =
    [
        "Smooth sailing ahead, captain!",
        "A light breeze fills the sails",
        "Choppy waters — hold fast!",
        "Storm clouds gathering on the horizon",
        "Batten down the hatches — rough seas ahead!",
        "A tempest brews — all hands on deck!",
        "Neptune himself couldn't calm these waters!"
    ];

    SeaState[] states = Enum.GetValues<SeaState>();
    int index = Random.Shared.Next(states.Length);

    SeaState state = states[index];
    double windSpeedKnots = Math.Round(Random.Shared.NextDouble() * 60, 1);
    double waveHeightMeters = Math.Round(Random.Shared.NextDouble() * 14, 1);
    double visibilityNauticalMiles = Math.Round(Random.Shared.NextDouble() * 30, 1);
    double waterTemperatureCelsius = Math.Round((Random.Shared.NextDouble() * 30) + 2, 1);
    string description = descriptions[index];
    DateTimeOffset timestamp = DateTimeOffset.UtcNow;

    return new SeaCondition(
        state,
        windSpeedKnots,
        waveHeightMeters,
        visibilityNauticalMiles,
        waterTemperatureCelsius,
        description,
        timestamp);
}
