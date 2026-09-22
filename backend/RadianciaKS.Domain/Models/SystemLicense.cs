using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Domain.Models
{
    public class SystemLicense
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string LicenseKey { get; set; } = string.Empty;
        public LicenseStatus Status { get; set; } = LicenseStatus.UNVALIDATED;
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? LastValidatedAt { get; set; }
        public DateTimeOffset LastKnownSystemTime { get; set; } = DateTimeOffset.UtcNow;
        public string? LicenseFile { get; set; }

        public string? MachineFingerprint { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        public bool IsValid()
        {
            var now = DateTimeOffset.UtcNow;

            if (now < LastKnownSystemTime.AddMinutes(-2))
                return false;

            return Status == LicenseStatus.ACTIVE && now <= ExpiresAt;
        }
    }
}