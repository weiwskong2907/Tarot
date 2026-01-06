using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tarot.Api.Controllers;
using Tarot.Core.Entities;
using Xunit;

namespace Tarot.IntegrationTests;

public class DynamicResourcesTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    private void WithSuperAdmin()
    {
        _client.DefaultRequestHeaders.Remove("X-Test-Roles");
        _client.DefaultRequestHeaders.Add("X-Test-Roles", "SuperAdmin");
    }

    [Fact]
    public async Task GetSchemas_ShouldReturnSchemas()
    {
        WithSuperAdmin();
        var response = await _client.GetAsync("/api/v1/resources/schemas");
        response.EnsureSuccessStatusCode();

        var schemas = await response.Content.ReadFromJsonAsync<Dictionary<string, List<ResourceProperty>>>();
        Assert.NotNull(schemas);
        Assert.Contains("services", schemas!.Keys);
        Assert.Contains("appointments", schemas.Keys);
        
        var serviceProps = schemas["services"];
        Assert.Contains(serviceProps, p => p.Name == "Name");
        Assert.Contains(serviceProps, p => p.Name == "Price");
    }

    [Fact]
    public async Task CreateAndGet_Service_ShouldWork()
    {
        WithSuperAdmin();
        var newService = new 
        { 
            Name = "Dynamic Service", 
            Price = 99.99m, 
            DurationMin = 45, 
            IsActive = true 
        };

        // Create
        var createResp = await _client.PostAsJsonAsync("/api/v1/resources/services", newService);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<Service>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.Id);
        Assert.Equal("Dynamic Service", created.Name);

        // Get By Id
        var getResp = await _client.GetAsync($"/api/v1/resources/services/{created.Id}");
        getResp.EnsureSuccessStatusCode();
        var fetched = await getResp.Content.ReadFromJsonAsync<Service>();
        Assert.Equal(created.Id, fetched!.Id);

        // Get All
        var listResp = await _client.GetAsync("/api/v1/resources/services");
        listResp.EnsureSuccessStatusCode();
        var list = await listResp.Content.ReadFromJsonAsync<List<Service>>();
        Assert.Contains(list!, s => s.Id == created.Id);
    }

    [Fact]
    public async Task Update_ShouldUpdateEntity()
    {
        WithSuperAdmin();
        // Create first
        var createResp = await _client.PostAsJsonAsync("/api/v1/resources/services", new 
        { 
            Name = "ToUpdate", 
            Price = 10m, 
            DurationMin = 10 
        });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<Service>();

        // Update
        var updateResp = await _client.PutAsJsonAsync($"/api/v1/resources/services/{created!.Id}", new 
        { 
            Name = "Updated Name",
            Price = 20m,
            DurationMin = 10
        });
        updateResp.EnsureSuccessStatusCode();

        // Verify
        var getResp = await _client.GetAsync($"/api/v1/resources/services/{created.Id}");
        var updated = await getResp.Content.ReadFromJsonAsync<Service>();
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal(20m, updated.Price);
    }

    [Fact]
    public async Task Delete_ShouldRemoveEntity()
    {
        WithSuperAdmin();
        // Create first
        var createResp = await _client.PostAsJsonAsync("/api/v1/resources/services", new 
        { 
            Name = "ToDelete", 
            Price = 10m, 
            DurationMin = 10 
        });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<Service>();

        // Delete
        var delResp = await _client.DeleteAsync($"/api/v1/resources/services/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        // Verify Not Found
        var getResp = await _client.GetAsync($"/api/v1/resources/services/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    public class ResourceProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
    }
}
