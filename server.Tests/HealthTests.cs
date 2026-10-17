using System.Net;

namespace Game.Server.Tests;

public class HealthTests(PostgresFixture pg) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Health_WithDatabase_IsHealthy()
    {
        Assert.SkipUnless(pg.Available, "Docker is not running");
        using var factory = new ServerFactory(pg.ConnectionString);

        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_WithoutDatabase_IsUnhealthy()
    {
        using var factory = new ServerFactory("Host=localhost;Port=1;Database=none;Username=none;Password=none;Timeout=2");

        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_IsServedInDevelopment()
    {
        using var factory = new ServerFactory("Host=localhost;Port=1;Database=none;Username=none;Password=none");

        var response = await factory.CreateClient().GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
