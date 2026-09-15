using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RadianciaKS.Api.Services;

namespace RadianciaKS.UnitTests.Api.Services
{
    public class TenantProviderTests
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IConfiguration> _configurationMock;

        public TenantProviderTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _configurationMock = new Mock<IConfiguration>();
        }

        [Fact]
        public void GetTenantId_Should_ReturnHeaderTenantId_When_ValidHeaderIsPresent()
        {
            var expectedTenantId = Guid.NewGuid();
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Tenant-Id"] = expectedTenantId.ToString();
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

            var provider = new TenantProvider(_httpContextAccessorMock.Object, _configurationMock.Object);

            var result = provider.GetTenantId();

            result.ShouldBe(expectedTenantId);
        }

        [Fact]
        public void GetTenantId_Should_ReturnConfigurationTenantId_When_HeaderIsMissingButConfigurationExists()
        {
            var expectedTenantId = Guid.NewGuid();
            var context = new DefaultHttpContext();
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);
            _configurationMock.Setup(c => c["TENANT_ID"]).Returns(expectedTenantId.ToString());

            var provider = new TenantProvider(_httpContextAccessorMock.Object, _configurationMock.Object);

            var result = provider.GetTenantId();

            result.ShouldBe(expectedTenantId);
        }

        [Fact]
        public void GetTenantId_Should_ReturnEnvironmentTenantId_When_HeaderAndConfigAreMissing_ButEnvVariableExists()
        {
            var expectedTenantId = Guid.NewGuid();
            var context = new DefaultHttpContext();
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);
            _configurationMock.Setup(c => c["TENANT_ID"]).Returns((string?)null);

            Environment.SetEnvironmentVariable("TENANT_ID", expectedTenantId.ToString());
            try
            {
                var provider = new TenantProvider(_httpContextAccessorMock.Object, _configurationMock.Object);

                var result = provider.GetTenantId();

                result.ShouldBe(expectedTenantId);
            }
            finally
            {
                Environment.SetEnvironmentVariable("TENANT_ID", null);
            }
        }

        [Fact]
        public void GetTenantId_Should_ThrowArgumentException_When_HttpContextExists_And_TenantIsNotConfigured()
        {
            var context = new DefaultHttpContext();
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);
            _configurationMock.Setup(c => c["TENANT_ID"]).Returns((string?)null);
            Environment.SetEnvironmentVariable("TENANT_ID", null);

            var provider = new TenantProvider(_httpContextAccessorMock.Object, _configurationMock.Object);

            var exception = Should.Throw<ArgumentException>(() => provider.GetTenantId());
            exception.Message.ShouldBe("Tenant não informado.");
        }

        [Fact]
        public void GetTenantId_Should_ReturnConfigurationTenantId_When_HttpContextIsNull()
        {
            var expectedTenantId = Guid.NewGuid();
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            _configurationMock.Setup(c => c["TENANT_ID"]).Returns(expectedTenantId.ToString());

            var provider = new TenantProvider(_httpContextAccessorMock.Object, _configurationMock.Object);

            var result = provider.GetTenantId();

            result.ShouldBe(expectedTenantId);
        }

        [Fact]
        public void GetTenantId_Should_ReturnEnvironmentTenantId_When_HttpContextIsNull_And_OnlyEnvVariableExists()
        {
            var expectedTenantId = Guid.NewGuid();
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            _configurationMock.Setup(c => c["TENANT_ID"]).Returns((string?)null);

            Environment.SetEnvironmentVariable("TENANT_ID", expectedTenantId.ToString());
            try
            {
                var provider = new TenantProvider(_httpContextAccessorMock.Object, _configurationMock.Object);

                var result = provider.GetTenantId();

                result.ShouldBe(expectedTenantId);
            }
            finally
            {
                Environment.SetEnvironmentVariable("TENANT_ID", null);
            }
        }

        [Fact]
        public void GetTenantId_Should_ThrowArgumentException_When_HttpContextIsNull_And_NoTenantConfiguredAtStartup()
        {
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            _configurationMock.Setup(c => c["TENANT_ID"]).Returns((string?)null);
            Environment.SetEnvironmentVariable("TENANT_ID", null);

            var provider = new TenantProvider(_httpContextAccessorMock.Object, _configurationMock.Object);

            var exception = Should.Throw<ArgumentException>(() => provider.GetTenantId());
            exception.Message.ShouldBe("Tenant não informado na inicialização do sistema.");
        }
    }
}