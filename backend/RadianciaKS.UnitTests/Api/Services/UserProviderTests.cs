using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RadianciaKS.Api.Services;

namespace RadianciaKS.UnitTests.Api.Services
{
    public class UserProviderTests
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

        public UserProviderTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        }

        [Fact]
        public void GetTenantId_Should_ReturnGuid_When_ValidTenantIdClaimExists()
        {
            var expectedTenantId = Guid.NewGuid();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
            new Claim("TenantId", expectedTenantId.ToString())
        }));

            var context = new DefaultHttpContext { User = user };
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

            var provider = new UserProvider(_httpContextAccessorMock.Object);

            var result = provider.GetTenantId();

            result.ShouldBe(expectedTenantId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("not-a-guid")]
        [InlineData("")]
        public void GetTenantId_Should_ReturnNull_When_TenantIdClaimIsMissingOrInvalid(string? invalidValue)
        {
            var identity = new ClaimsIdentity();
            if (invalidValue != null)
            {
                identity.AddClaim(new Claim("TenantId", invalidValue));
            }

            var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

            var provider = new UserProvider(_httpContextAccessorMock.Object);

            var result = provider.GetTenantId();

            result.ShouldBeNull();
        }

        [Fact]
        public void GetTenantId_Should_ReturnNull_When_HttpContextIsNull()
        {
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            var provider = new UserProvider(_httpContextAccessorMock.Object);

            provider.GetTenantId().ShouldBeNull();
        }

        [Fact]
        public void GetUserId_Should_ReturnGuid_When_ValidNameIdentifierClaimExists()
        {
            var expectedUserId = Guid.NewGuid();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString())
        }));

            var context = new DefaultHttpContext { User = user };
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

            var provider = new UserProvider(_httpContextAccessorMock.Object);

            var result = provider.GetUserId();

            result.ShouldBe(expectedUserId);
        }

        [Fact]
        public void GetUserId_Should_ReturnNull_When_NameIdentifierClaimIsMissingOrInvalid()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, "invalid-guid")
        }));

            var context = new DefaultHttpContext { User = user };
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

            var provider = new UserProvider(_httpContextAccessorMock.Object);

            provider.GetUserId().ShouldBeNull();
        }

        [Fact]
        public void GetUserRole_Should_ReturnRole_When_RoleClaimExists()
        {
            var expectedRole = "Manager";
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.Role, expectedRole)
        }));

            var context = new DefaultHttpContext { User = user };
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

            var provider = new UserProvider(_httpContextAccessorMock.Object);

            var result = provider.GetUserRole();

            result.ShouldBe(expectedRole);
        }

        [Fact]
        public void GetUserRole_Should_ReturnNull_When_RoleClaimIsMissing()
        {
            var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

            var provider = new UserProvider(_httpContextAccessorMock.Object);

            provider.GetUserRole().ShouldBeNull();
        }
    }
}