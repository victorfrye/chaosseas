using Microsoft.OpenApi;

using VictorFrye.ChaosSeas.Extensions.ServiceDefaults;
using VictorFrye.ChaosSeas.HarborApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddCalmSeasServices();
builder.Services.AddRoughSeasServices();
builder.Services.AddGameDayServices();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(static options =>
{
    options.AddDocumentTransformer(static async (document, _, cancellationToken) =>
        await Task.Run(() =>
        {
            document.Info = new()
            {
                Title = "Harbor API",
                Version = "v1",
                Description = "API for navigating the chaos seas — demonstrating resilience and chaos engineering with Polly."
            };
        }, cancellationToken));
});

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();
app.MapOpenApi().CacheOutput(p => p.Expire(TimeSpan.FromDays(1)));

app.MapCalmSeasEndpoints();
app.MapRoughSeasEndpoints();
app.MapGameDayEndpoints();

app.UseHttpsRedirection();

await app.RunAsync();
