using Rise.Server.Tests.E2E;
using Xunit;

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestsCollection : ICollectionFixture<CustomWebApplicationFactory<Program>>
{
    // This class has no code, and is never created. 
    // Its purpose is to annotate the collection with [CollectionDefinition] 
    // and associate the fixture with it.
}
