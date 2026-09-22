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

        [JsonPropertyName("scope")]
        public KeygenValidateScopeRequest? Scope { get; set; }
    }

    public class KeygenValidateScopeRequest
    {
        [JsonPropertyName("fingerprint")]
        public string Fingerprint { get; set; } = string.Empty;
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

    public class KeygenRegisterMachineRequest
    {
        [JsonPropertyName("data")]
        public KeygenMachineDataRequest Data { get; set; } = new();
    }

    public class KeygenMachineDataRequest
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "machines";

        [JsonPropertyName("attributes")]
        public KeygenMachineAttributesRequest Attributes { get; set; } = new();

        [JsonPropertyName("relationships")]
        public KeygenMachineRelationshipsRequest Relationships { get; set; } = new();
    }

    public class KeygenMachineAttributesRequest
    {
        [JsonPropertyName("fingerprint")]
        public string Fingerprint { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("platform")]
        public string? Platform { get; set; }
    }

    public class KeygenMachineRelationshipsRequest
    {
        [JsonPropertyName("license")]
        public KeygenLicenseRelationshipRequest License { get; set; } = new();
    }

    public class KeygenLicenseRelationshipRequest
    {
        [JsonPropertyName("data")]
        public KeygenResourceIdentifier Data { get; set; } = new();
    }

    public class KeygenResourceIdentifier
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "licenses";

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}