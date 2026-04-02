using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

string voyageScenario = "no-resilience";
string voyageCount = "10";
string failureRate = "30";

var seaConditions = builder.AddProject<Projects.SeaConditionsApi>("sea-conditions-api")
    .WithHttpHealthCheck("/alive")
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["FAILURE_RATE"] = failureRate;
    });

var harborApi = builder.AddProject<Projects.HarborApi>("harbor-api")
    .WithReference(seaConditions)
    .WaitFor(seaConditions)
    .WithHttpHealthCheck("/alive");

var voyager = builder.AddProject<Projects.VoyagerCli>("voyager")
    .WithReference(harborApi)
    .WaitFor(harborApi)
    .WithArgs(context =>
    {
        context.Args.Add(voyageScenario);
        context.Args.Add(voyageCount);
    })
    .WithExplicitStart();

voyager.WithCommand(
    name: "plan-voyage",
    displayName: "Plan Voyage",
    executeCommand: async context =>
    {
        IInteractionService interactionService = context.ServiceProvider
            .GetRequiredService<IInteractionService>();

        if (!interactionService.IsAvailable)
        {
            return CommandResults.Failure("Interaction service is not available.");
        }

        var result = await interactionService.PromptInputsAsync(
            title: "🏴\u200d☠️ Plan Voyage",
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
                        new("no-resilience", "🚫 No Resilience"),
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

        voyageScenario = result.Data[0].Value ?? "no-resilience";
        voyageCount = result.Data[1].Value ?? "10";

        failureRate = voyageScenario switch
        {
            "no-resilience" => "30",
            "calm-seas" => "10",
            "rough-seas-faults" => "50",
            "rough-seas-latency" => "30",
            "rough-seas-outcomes" => "60",
            "game-day" => "40",
            _ => "10"
        };

        return CommandResults.Success();
    },
    commandOptions: new CommandOptions
    {
        IconName = "VehicleShip",
        IconVariant = IconVariant.Filled
    });

await builder.Build().RunAsync();
