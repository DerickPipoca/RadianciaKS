using System.Net.Http.Headers;
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

        public async Task<bool> RegisterMachineAsync(string licenseKey, string fingerprint, string machineName, CancellationToken ct = default)
        {
            var endpoint = $"{_settings.AccountId}/machines";
            var payload = new KeygenRegisterMachineRequest
            {
                Data = new KeygenMachineDataRequest
                {
                    Attributes = new KeygenMachineAttributesRequest
                    {
                        Fingerprint = fingerprint.Trim(),
                        Name = machineName,
                        Platform = Environment.OSVersion.Platform.ToString()
                    }
                }
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("License", licenseKey.Trim());
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/vnd.api+json");

                var response = await _httpClient.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[KEYGEN] Máquina registrada com sucesso. Fingerprint: {Fingerprint}", fingerprint);
                    return true;
                }

                var errorBody = await response.Content.ReadAsStringAsync(ct);

                if (errorBody.Contains("FINGERPRINT_TAKEN"))
                {
                    _logger.LogInformation("[KEYGEN] Máquina já se encontrava registrada no Keygen.");
                    return true;
                }

                _logger.LogError("[KEYGEN] Erro ao registrar máquina. Status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KEYGEN] Falha ao tentar registrar máquina no Keygen.");
                return false;
            }
        }

        public async Task<KeygenValidationResult> ValidateKeyAsync(string licenseKey, string? fingerprint = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                return new KeygenValidationResult(false, false, "EMPTY_KEY", null, "Chave de licença não configurada.");
            }

            var endpoint = $"{_settings.AccountId}/licenses/actions/validate-key";
            var requestPayload = new KeygenValidateRequest
            {
                Meta = new KeygenValidateMetaRequest
                {
                    Key = licenseKey.Trim(),
                    Scope = !string.IsNullOrWhiteSpace(fingerprint)
                        ? new KeygenValidateScopeRequest { Fingerprint = fingerprint.Trim() }
                        : null
                }
            };

            try
            {
                using var content = new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/vnd.api+json");

                var response = await _httpClient.PostAsync(endpoint, content, ct);
                var result = await response.Content.ReadFromJsonAsync<KeygenValidateResponse>(cancellationToken: ct);

                if (result?.Meta == null)
                {
                    return new KeygenValidationResult(true, false, "MALFORMED_RESPONSE", null, "Resposta vazia ou inválida.");
                }

                return new KeygenValidationResult(
                    IsOnline: true,
                    IsValid: result.Meta.Valid,
                    Code: result.Meta.Code,
                    Expiry: result.Data?.Attributes.Expiry,
                    ErrorMessage: result.Meta.Detail
                );
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                _logger.LogInformation("Falha de conexão ao validar licença no Keygen (restaurante offline ou timeout): {Message}", ex.Message);
                return new KeygenValidationResult(false, false, "OFFLINE", null, ex.Message);
            }
        }
    }
}