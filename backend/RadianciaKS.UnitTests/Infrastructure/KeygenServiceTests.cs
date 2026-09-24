using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;
using RadianciaKS.Infrastructure.Licensing.Configuration;
using RadianciaKS.Infrastructure.Services;

namespace RadianciaKS.UnitTests.Infrastructure
{
    public class KeygenServiceTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<IOptions<KeygenSettings>> _optionsMock;
        private readonly Mock<ILogger<KeygenService>> _loggerMock;
        private readonly KeygenService _keygenService;

        public KeygenServiceTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://api.keygen.sh/v1/accounts/")
            };

            _optionsMock = new Mock<IOptions<KeygenSettings>>();
            _optionsMock.Setup(o => o.Value).Returns(new KeygenSettings
            {
                AccountId = "test-account-id"
            });

            _loggerMock = new Mock<ILogger<KeygenService>>();

            _keygenService = new KeygenService(_httpClient, _optionsMock.Object, _loggerMock.Object);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ValidateKeyAsync_WhenKeyIsNullOrWhitespace_ShouldReturnEmptyKeyResult(string? invalidKey)
        {
            var result = await _keygenService.ValidateKeyAsync(invalidKey!);

            Assert.False(result.IsOnline);
            Assert.False(result.IsValid);
            Assert.Equal("EMPTY_KEY", result.Code);
            Assert.Equal("Chave de licença não configurada.", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateKeyAsync_WhenKeyIsValidWithFingerprint_ShouldReturnValidResult()
        {
            const string jsonResponse = """
            {
                "meta": {
                    "valid": true,
                    "code": "VALID",
                    "detail": "Licença ativa e vinculada"
                },
                "data": {
                    "id": "lic-12345",
                    "type": "licenses",
                    "attributes": {
                        "expiry": "2027-12-31T23:59:59Z"
                    }
                }
            }
            """;

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            var result = await _keygenService.ValidateKeyAsync("VALID-KEY-123", "machine-fingerprint-abc");

            Assert.True(result.IsOnline);
            Assert.True(result.IsValid);
            Assert.Equal("VALID", result.Code);
            Assert.Equal("lic-12345", result.LicenseId);
            Assert.Equal(DateTime.Parse("2027-12-31T23:59:59Z").ToUniversalTime(), result.Expiry?.ToUniversalTime());
            Assert.Equal("Licença ativa e vinculada", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateKeyAsync_WhenKeyIsValidWithoutFingerprint_ShouldSendNullScopeAndReturnResult()
        {
            const string jsonResponse = """
            {
                "meta": {
                    "valid": false,
                    "code": "FINGERPRINT_REQUIRED",
                    "detail": "Fingerprint obrigatório"
                },
                "data": null
            }
            """;

            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            var result = await _keygenService.ValidateKeyAsync("SOME-KEY", fingerprint: null);

            Assert.True(result.IsOnline);
            Assert.False(result.IsValid);
            Assert.Equal("FINGERPRINT_REQUIRED", result.Code);
            Assert.Null(result.LicenseId);
            Assert.Null(result.Expiry);
        }

        [Fact]
        public async Task ValidateKeyAsync_WhenResponseMetaIsNull_ShouldReturnMalformedResponse()
        {
            const string jsonResponse = "{}";
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            var result = await _keygenService.ValidateKeyAsync("SOME-KEY", "fingerprint");

            Assert.True(result.IsOnline);
            Assert.False(result.IsValid);
            Assert.Equal("MALFORMED_RESPONSE", result.Code);
            Assert.Equal("Resposta vazia ou inválida.", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateKeyAsync_WhenHttpRequestFails_ShouldCatchExceptionAndReturnOffline()
        {
            SetupHttpException(new HttpRequestException("Falha ao resolver host Keygen"));

            var result = await _keygenService.ValidateKeyAsync("SOME-KEY", "fingerprint");

            Assert.False(result.IsOnline);
            Assert.False(result.IsValid);
            Assert.Equal("OFFLINE", result.Code);
            Assert.Contains("Falha ao resolver host Keygen", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateKeyAsync_WhenTaskCanceledOrTimeout_ShouldCatchExceptionAndReturnOffline()
        {
            SetupHttpException(new TaskCanceledException("Timeout limite excedido"));

            var result = await _keygenService.ValidateKeyAsync("SOME-KEY", "fingerprint");

            Assert.False(result.IsOnline);
            Assert.False(result.IsValid);
            Assert.Equal("OFFLINE", result.Code);
            Assert.Contains("Timeout limite excedido", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterMachineAsync_WhenStatusIsSuccess_ShouldReturnTrue()
        {
            SetupHttpResponse(HttpStatusCode.Created, "{\"data\":{\"id\":\"mach-1\"}}");

            var result = await _keygenService.RegisterMachineAsync("KEY-123", "lic-1", "fp-1", "PDV-Caixa-1");

            Assert.True(result);
        }

        [Fact]
        public async Task RegisterMachineAsync_WhenFingerprintAlreadyTaken_ShouldReturnTrue()
        {
            const string conflictResponse = """
            {
                "errors": [
                    { "code": "FINGERPRINT_TAKEN", "detail": "Machine fingerprint has already been taken" }
                ]
            }
            """;

            SetupHttpResponse(HttpStatusCode.UnprocessableEntity, conflictResponse);

            var result = await _keygenService.RegisterMachineAsync("KEY-123", "lic-1", "fp-1", "PDV-Caixa-1");

            Assert.True(result);
        }

        [Fact]
        public async Task RegisterMachineAsync_WhenHttpFailsWithOtherError_ShouldReturnFalse()
        {
            const string errorResponse = """
            {
                "errors": [
                    { "code": "MACHINE_LIMIT_EXCEEDED", "detail": "Limite de terminais atingido" }
                ]
            }
            """;

            SetupHttpResponse(HttpStatusCode.BadRequest, errorResponse);

            var result = await _keygenService.RegisterMachineAsync("KEY-123", "lic-1", "fp-1", "PDV-Caixa-1");

            Assert.False(result);
        }

        [Fact]
        public async Task RegisterMachineAsync_WhenExceptionOccurs_ShouldCatchAndReturnFalse()
        {
            SetupHttpException(new HttpRequestException("Erro de rede"));

            var result = await _keygenService.RegisterMachineAsync("KEY-123", "lic-1", "fp-1", "PDV-Caixa-1");

            Assert.False(result);
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content, Encoding.UTF8, "application/vnd.api+json")
                });
        }

        private void SetupHttpException(Exception exception)
        {
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(exception);
        }
    }
}