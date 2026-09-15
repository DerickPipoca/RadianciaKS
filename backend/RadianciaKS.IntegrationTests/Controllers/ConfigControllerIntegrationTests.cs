using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class ConfigControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ConfigControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await _factory.ResetDatabaseAsync();
            _client.DefaultRequestHeaders.Authorization = null;
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task GetTenantConfig_Should_Return200Ok_WithoutAuthentication()
        {
            var response = await _client.GetAsync("/api/config/tenant");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            json.TryGetProperty("tenantId", out var tenantProperty).ShouldBeTrue();
            tenantProperty.GetString().ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GetTenantConfig_Should_ReturnExpectedTenantId_FromTestingConfiguration()
        {
            var response = await _client.GetAsync("/api/config/tenant");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var tenantId = json.GetProperty("tenantId").GetString();

            tenantId.ShouldBe(TestTenantProvider.DefaultTenantId.ToString());
        }
    }
}