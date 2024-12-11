namespace Rise.Server.Tests.E2E;

public class BaseFunctionalTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    protected readonly HttpClient _client;
    protected readonly CustomWebApplicationFactory<Program> _factory;

    public BaseFunctionalTest(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
}