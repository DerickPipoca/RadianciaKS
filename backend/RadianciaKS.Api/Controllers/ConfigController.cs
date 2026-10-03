using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConfigController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("tenant")]
        [AllowAnonymous]
        public IActionResult GetTenantConfig()
        {
            var tenantId = _configuration["TENANT_ID"]
                ?? Environment.GetEnvironmentVariable("TENANT_ID");

            var cloudUrl = _configuration["CLOUD_API_URL"]
                ?? Environment.GetEnvironmentVariable("CLOUD_API_URL");

            if (tenantId == null)
                throw new ArgumentException("Variavel de ambiente 'TENANT_ID' não definida...");

            if (cloudUrl == null)
                throw new ArgumentException("Variavel de ambiente 'CLOUD_API_URL' não definida...");

            return Ok(new
            {
                tenantId = Guid.Parse(tenantId),
                cloudApiUrl = cloudUrl
            });
        }
    }
}