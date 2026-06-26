namespace RadianciaKS.Application.DTOs.StoreSettings
{
    public class StoreSettingsResponseDto
    {
        public Guid Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string CNPJ { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? ReceiptFooter { get; set; }
        public string? SmallLogoPath { get; set; }
        public string? BigLogoPath { get; set; }
        public decimal ServiceCharge { get; set; }
    }
}