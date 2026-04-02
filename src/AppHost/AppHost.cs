var builder = DistributedApplication.CreateBuilder(args);

var seaConditions = builder.AddProject<Projects.SeaConditionsApi>("sea-conditions-api")
    .WithHttpHealthCheck("/alive");

var harborApi = builder.AddProject<Projects.HarborApi>("harbor-api")
    .WithReference(seaConditions)
    .WaitFor(seaConditions)
    .WithHttpHealthCheck("/alive");

builder.AddProject<Projects.Voyager>("voyager")
    .WithReference(harborApi)
    .WaitFor(harborApi)
    .WithArgs("calm-seas", "10");

await builder.Build().RunAsync();
