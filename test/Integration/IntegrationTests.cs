namespace VictorFrye.ChaosSeas.Integration.Tests;

public class IntegrationTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task AppHost_StartsSuccessfully()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>(cancellationToken);

        await using DistributedApplication app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync(
            "sea-conditions-api", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync(
            "harbor-api", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
    }

    [Theory]
    [InlineData("harbor-api", "/alive")]
    [InlineData("harbor-api", "/health")]
    [InlineData("sea-conditions-api", "/alive")]
    [InlineData("sea-conditions-api", "/health")]
    public async Task HealthEndpoints_ReturnOk(string resourceName, string path)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>(cancellationToken);

        await using DistributedApplication app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync(
            resourceName, cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        HttpClient client = app.CreateHttpClient(resourceName);

        HttpResponseMessage response = await client.GetAsync(path, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("harbor-api", "/voyage/game-day/status")]
    public async Task ApiEndpoints_ReturnSuccessStatusCode(string resourceName, string path)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>(cancellationToken);

        await using DistributedApplication app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync(
            resourceName, cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        HttpClient client = app.CreateHttpClient(resourceName);

        HttpResponseMessage response = await client.GetAsync(path, cancellationToken);

        Assert.True(response.IsSuccessStatusCode, $"Expected success status code for {path}, got {response.StatusCode}");
    }
}
