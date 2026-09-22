using System.Text.Json.Serialization;

namespace RadianciaKS.Application.DTOs.Licensing
{
    public class KeygenValidateRequest
    {
        [JsonPropertyName("meta")]
        public KeygenValidateMetaRequest Meta { get; set; } = new();
    }

    public class KeygenValidateMetaRequest
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
    }

    public class KeygenValidateResponse
    {
        [JsonPropertyName("meta")]
        public KeygenMetaResponse? Meta { get; set; }

        [JsonPropertyName("data")]
        public KeygenDataResponse? Data { get; set; }
    }

    public class KeygenMetaResponse
    {
        [JsonPropertyName("valid")]
        public bool Valid { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("detail")]
        public string? Detail { get; set; }
    }

    public class KeygenDataResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public KeygenAttributesResponse Attributes { get; set; } = new();
    }

    public class KeygenAttributesResponse
    {
        [JsonPropertyName("expiry")]
        public DateTimeOffset? Expiry { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}