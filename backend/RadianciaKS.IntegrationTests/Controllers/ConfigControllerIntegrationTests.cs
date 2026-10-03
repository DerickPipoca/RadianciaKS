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

            json.TryGetProperty("tenantId", out var tenantProp).ShouldBeTrue();
            tenantProp.GetString().ShouldNotBeNullOrWhiteSpace();

            json.TryGetProperty("cloudApiUrl", out var cloudProp).ShouldBeTrue();
            cloudProp.GetString().ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GetTenantConfig_Should_ReturnExpectedConfiguration_FromTestingEnvironment()
        {
            var response = await _client.GetAsync("/api/config/tenant");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            var tenantId = json.GetProperty("tenantId").GetString();
            var cloudApiUrl = json.GetProperty("cloudApiUrl").GetString();

            tenantId.ShouldBe(TestTenantProvider.DefaultTenantId.ToString());

            cloudApiUrl.ShouldBe(CustomWebApplicationFactory.DefaultCloudApiUrl);
        }

        [Fact]
        public async Task GetTenantConfig_Should_ReturnValidGuidFormat()
        {
            var response = await _client.GetAsync("/api/config/tenant");

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var tenantIdString = json.GetProperty("tenantId").GetString();

            Guid.TryParse(tenantIdString, out _).ShouldBeTrue();
        }
    }
}