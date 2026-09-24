using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq.EntityFrameworkCore;
using RadianciaKS.Api.Middlewares;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.UnitTests.Api.Middlewares
{
    public class LicenseValidationMiddlewareTests
    {
        private readonly Mock<IApplicationDbContext> _dbContextMock;

        public LicenseValidationMiddlewareTests()
        {
            _dbContextMock = new Mock<IApplicationDbContext>();
        }

        [Theory]
        [InlineData("/api/auth/login", "POST")]
        [InlineData("/api/auth/refresh", "POST")]
        [InlineData("/swagger/index.html", "GET")]
        [InlineData("/shubs/kds", "GET")]
        [InlineData("/api/orders", "OPTIONS")]
        [InlineData("/api/products", "options")]
        public async Task InvokeAsync_WhenRequestIsBypassed_ShouldCallNextWithoutQueryingDatabase(string path, string method)
        {
            var nextInvoked = false;
            RequestDelegate next = _ =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            };

            var middleware = new LicenseValidationMiddleware(next);
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Request.Method = method;

            await middleware.InvokeAsync(context, _dbContextMock.Object);

            Assert.True(nextInvoked);
            _dbContextMock.Verify(db => db.SystemLicenses, Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_WhenPathIsNull_ShouldProceedWithValidationAndCheckDatabase()
        {
            var nextInvoked = false;
            RequestDelegate next = _ =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            };

            var middleware = new LicenseValidationMiddleware(next);
            var context = new DefaultHttpContext();
            context.Request.Path = default(PathString);
            context.Request.Method = "GET";

            _dbContextMock.Setup(db => db.SystemLicenses).ReturnsDbSet(new List<SystemLicense>());

            await middleware.InvokeAsync(context, _dbContextMock.Object);

            Assert.False(nextInvoked);
            Assert.Equal(StatusCodes.Status402PaymentRequired, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WhenNoLicenseExists_ShouldReturn402WithNotFoundPayload()
        {
            var nextInvoked = false;
            RequestDelegate next = _ =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            };

            var middleware = new LicenseValidationMiddleware(next);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/orders";
            context.Request.Method = "GET";

            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            _dbContextMock.Setup(db => db.SystemLicenses).ReturnsDbSet(new List<SystemLicense>());

            await middleware.InvokeAsync(context, _dbContextMock.Object);

            Assert.False(nextInvoked);
            Assert.Equal(StatusCodes.Status402PaymentRequired, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseBodyStream);
            var responseBody = await reader.ReadToEndAsync();
            using var jsonDocument = JsonDocument.Parse(responseBody);

            Assert.Equal(402, jsonDocument.RootElement.GetProperty("statusCode").GetInt32());
            Assert.Equal("Payment Required", jsonDocument.RootElement.GetProperty("error").GetString());
            Assert.Equal("NOT_FOUND", jsonDocument.RootElement.GetProperty("status").GetString());
        }

        [Theory]
        [InlineData(LicenseStatus.SUSPENDED)]
        [InlineData(LicenseStatus.EXPIRED)]
        [InlineData(LicenseStatus.UNVALIDATED)]
        public async Task InvokeAsync_WhenLicenseIsInvalid_ShouldReturn402WithCurrentStatus(LicenseStatus status)
        {
            var nextInvoked = false;
            RequestDelegate next = _ =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            };

            var middleware = new LicenseValidationMiddleware(next);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/products";
            context.Request.Method = "POST";

            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            var license = new SystemLicense
            {
                Id = Guid.NewGuid(),
                LicenseKey = "INVALID-KEY-123",
                Status = status,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
                LastKnownSystemTime = DateTimeOffset.UtcNow.AddDays(-2)
            };

            _dbContextMock.Setup(db => db.SystemLicenses).ReturnsDbSet(new List<SystemLicense> { license });

            await middleware.InvokeAsync(context, _dbContextMock.Object);

            Assert.False(nextInvoked);
            Assert.Equal(StatusCodes.Status402PaymentRequired, context.Response.StatusCode);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseBodyStream);
            var responseBody = await reader.ReadToEndAsync();
            using var jsonDocument = JsonDocument.Parse(responseBody);

            Assert.Equal(status.ToString(), jsonDocument.RootElement.GetProperty("status").GetString());
        }

        [Fact]
        public async Task InvokeAsync_WhenLicenseIsValid_ShouldCallNextMiddleware()
        {
            var nextInvoked = false;
            RequestDelegate next = _ =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            };

            var middleware = new LicenseValidationMiddleware(next);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/categories";
            context.Request.Method = "GET";

            var license = new SystemLicense
            {
                Id = Guid.NewGuid(),
                LicenseKey = "VALID-ACTIVE-KEY",
                Status = LicenseStatus.ACTIVE,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
                LastKnownSystemTime = DateTimeOffset.UtcNow.AddHours(-1)
            };

            _dbContextMock.Setup(db => db.SystemLicenses).ReturnsDbSet(new List<SystemLicense> { license });

            await middleware.InvokeAsync(context, _dbContextMock.Object);

            Assert.True(nextInvoked);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }
    }
}