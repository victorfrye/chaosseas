using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

string voyageScenario = "calm-seas";
string voyageCount = "10";

var seaConditions = builder.AddProject<Projects.SeaConditionsApi>("sea-conditions-api")
    .WithHttpHealthCheck("/alive");

var harborApi = builder.AddProject<Projects.HarborApi>("harbor-api")
    .WithReference(seaConditions)
    .WaitFor(seaConditions)
    .WithHttpHealthCheck("/alive");

var voyager = builder.AddProject<Projects.Voyager>("voyager")
    .WithReference(harborApi)
    .WaitFor(harborApi)
    .WithArgs(context =>
    {
        context.Args.Add(voyageScenario);
        context.Args.Add(voyageCount);
    })
    .WithExplicitStart();

voyager.WithCommand(
    name: "launch-voyage",
    displayName: "Launch Voyage",
    executeCommand: async context =>
    {
        IInteractionService interactionService = context.ServiceProvider
            .GetRequiredService<IInteractionService>();

        if (!interactionService.IsAvailable)
        {
            return CommandResults.Failure("Interaction service is not available.");
        }

        var result = await interactionService.PromptInputsAsync(
            title: "🏴\u200d☠️ Launch Voyage",
            message: "Configure your chaos engineering voyage:",
            inputs:
            [
                new()
                {
                    Name = "Scenario",
                    InputType = InputType.Choice,
                    Required = true,
                    Options =
                    [
                        new("calm-seas", "🌊 Calm Seas"),
                        new("rough-seas-faults", "⚡ Rough Seas — Faults"),
                        new("rough-seas-latency", "⏱️ Rough Seas — Latency"),
                        new("rough-seas-outcomes", "💀 Rough Seas — Outcomes"),
                        new("game-day", "🏴\u200d☠️ Game Day")
                    ]
                },
                new()
                {
                    Name = "Request Count",
                    InputType = InputType.Number,
                    Required = true,
                    Placeholder = "10"
                }
            ]);

        if (result.Canceled)
        {
            return CommandResults.Failure("Voyage canceled.");
        }

        voyageScenario = result.Data[0].Value ?? "calm-seas";
        voyageCount = result.Data[1].Value ?? "10";

        return CommandResults.Success();
    },
    commandOptions: new CommandOptions
    {
        IconName = "VehicleShip",
        IconVariant = IconVariant.Filled
    });

await builder.Build().RunAsync();
