using System.Text.Json;
using System.Text.Json.Serialization;
using Rise.Persistence;
using Rise.Server.Tests.E2E;
using Rise.Shared.Users;

public abstract class BaseControllerE2ETests : IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected readonly JsonSerializerOptions JsonOptions;

    protected BaseControllerE2ETests(CustomWebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = Factory.CreateClient();
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new ImmutableListJsonConverter<RoleDto>(), new JsonStringEnumConverter() }
        };
    }

    public async Task InitializeAsync()
    {
        await ResetDatabase();
        SeedData();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task ResetDatabase()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    protected abstract void SeedData();

    protected string GenerateJwtToken(string name = "admin", string role = "Admin", string id = "auth0|6713ad524e8a8907fbf0d57f")
    {
        return Factory.GenerateJwtToken(name, role, id);
    }
}
