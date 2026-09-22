using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadianciaKS.Application.DTOs.Licensing;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Infrastructure.Licensing.Configuration;

namespace RadianciaKS.Infrastructure.Services
{
    public class KeygenService : IKeygenService
    {
        private readonly HttpClient _httpClient;
        private readonly KeygenSettings _settings;
        private readonly ILogger<KeygenService> _logger;

        public KeygenService(
            HttpClient httpClient,
            IOptions<KeygenSettings> settings,
            ILogger<KeygenService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<KeygenValidationResult> ValidateKeyAsync(string licenseKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                return new KeygenValidationResult(false, false, "EMPTY_KEY", null, "Chave de licença não configurada.");
            }

            var endpoint = $"{_settings.AccountId}/licenses/actions/validate-key";
            var requestPayload = new KeygenValidateRequest
            {
                Meta = new KeygenValidateMetaRequest { Key = licenseKey.Trim() }
            };
    
            try
            {
                using var content = new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/vnd.api+json");

                var response = await _httpClient.PostAsync(endpoint, content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Keygen devolveu status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                    return new KeygenValidationResult(true, false, "HTTP_ERROR", null, $"Status {(int)response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<KeygenValidateResponse>(cancellationToken: ct);

                if (result?.Meta == null)
                {
                    return new KeygenValidationResult(true, false, "MALFORMED_RESPONSE", null, "Resposta vazia ou inválida.");
                }

                return new KeygenValidationResult(
                    IsOnline: true,
                    IsValid: result.Meta.Valid,
                    Code: result.Meta.Code,
                    Expiry: result.Data?.Attributes.Expiry
                );
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                _logger.LogInformation("Falha de ligação ao validar licença no Keygen (servidor offline ou timeout): {Message}", ex.Message);
                return new KeygenValidationResult(false, false, "OFFLINE", null, ex.Message);
            }
        }
    }
}