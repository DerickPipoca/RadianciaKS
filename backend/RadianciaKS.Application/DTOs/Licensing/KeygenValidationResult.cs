namespace RadianciaKS.Application.DTOs.Licensing
{
    public record KeygenValidationResult(bool IsOnline,
        bool IsValid,
        string Code,
        DateTimeOffset? Expiry,
        string? ErrorMessage = null
    );
}