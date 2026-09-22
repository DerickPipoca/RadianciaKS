namespace RadianciaKS.Infrastructure.Licensing.Configuration
{
    public class KeygenSettings
    {
        public const string SectionName = "Keygen";

        public string AccountId { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.keygen.sh/v1/accounts/";
        public int TimeoutSeconds { get; set; } = 15;
        public int HeartbeatHours { get; set; } = 6;
    }
}