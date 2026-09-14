using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RadianciaKS.Application.Services;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class TokenServiceTests
    {
        private readonly IConfiguration _configuration;
        private readonly TokenService _tokenService;

        public TokenServiceTests()
        {
            var inMemorySettings = new Dictionary<string, string?>
        {
            { "JwtSettings:Secret", "chave_super_secreta_com_mais_de_256_bits_para_o_algoritmo_sha256_token_service" },
            { "JwtSettings:ExpiryInMinutes", "120" },
            { "JwtSettings:Issuer", "RadianciaKS_Auth" },
            { "JwtSettings:Audience", "RadianciaKS_Client" }
        };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _tokenService = new TokenService(_configuration);
        }

        private static Employee CreateMockEmployee(Action<Employee>? configure = null)
        {
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = "Lucas Gerente",
                Role = EmployeeRole.Manager,
                CPF = "98765432100",
                Active = true
            };

            configure?.Invoke(employee);
            return employee;
        }

        [Fact]
        public void GenerateToken_Should_ReturnValidJwt_WithAllClaimsAndMetadata()
        {
            var employee = CreateMockEmployee();

            var token = _tokenService.GenerateToken(employee);

            token.ShouldNotBeNullOrWhiteSpace();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            jwtToken.Header.Alg.ShouldBe(SecurityAlgorithms.HmacSha256);
            jwtToken.Issuer.ShouldBe("RadianciaKS_Auth");
            jwtToken.Audiences.ShouldContain("RadianciaKS_Client");

            var idClaim = jwtToken.Claims.First(c => c.Type == "nameid" || c.Type == ClaimTypes.NameIdentifier);
            idClaim.Value.ShouldBe(employee.Id.ToString());

            var nameClaim = jwtToken.Claims.First(c => c.Type == "unique_name" || c.Type == ClaimTypes.Name);
            nameClaim.Value.ShouldBe(employee.Name);

            var roleClaim = jwtToken.Claims.First(c => c.Type == "role" || c.Type == ClaimTypes.Role);
            roleClaim.Value.ShouldBe(EmployeeRole.Manager.ToString());

            var tenantClaim = jwtToken.Claims.First(c => c.Type == "TenantId");
            tenantClaim.Value.ShouldBe(employee.TenantId.ToString());

            jwtToken.ValidTo.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(115));
            jwtToken.ValidTo.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddMinutes(121));
        }

        [Fact]
        public void GenerateToken_Should_ReflectSpecificEmployeeRole_When_Provided()
        {
            var employee = CreateMockEmployee(e => e.Role = EmployeeRole.Admin);

            var token = _tokenService.GenerateToken(employee);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var roleClaim = jwtToken.Claims.First(c => c.Type == "role" || c.Type == ClaimTypes.Role);
            roleClaim.Value.ShouldBe(EmployeeRole.Admin.ToString());
        }

        [Fact]
        public void GenerateToken_Should_ThrowArgumentNullException_When_SecretKeyIsMissing()
        {
            var emptyConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                { "JwtSettings:ExpiryInMinutes", "60" },
                { "JwtSettings:Issuer", "Test" },
                { "JwtSettings:Audience", "Test" }
                })
                .Build();

            var tokenServiceWithoutSecret = new TokenService(emptyConfig);
            var employee = CreateMockEmployee();

            Should.Throw<ArgumentNullException>(() =>
                tokenServiceWithoutSecret.GenerateToken(employee));
        }
    }
}