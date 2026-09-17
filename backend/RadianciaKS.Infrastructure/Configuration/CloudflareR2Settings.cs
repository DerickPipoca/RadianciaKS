namespace RadianciaKS.Infrastructure.Configuration
{
    public class CloudflareR2Settings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
    }
}