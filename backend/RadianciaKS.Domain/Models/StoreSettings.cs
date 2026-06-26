namespace RadianciaKS.Domain.Models
{
    public class StoreSettings : EntityBase
    {
        public string StoreName { get; set; } = string.Empty;
        public string CNPJ { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? ReceiptFooter { get; set; }
        public string? SmallLogoPath { get; set; }
        public string? BigLogoPath { get; set; }
        public decimal ServiceCharge { get; set; }

        public void Update(string storeName,
        string cnpj, string? address,
        string? phone, string? receiptFooter,
        string? smallLogoPath, string? bigLogoPath,
        decimal serviceCharge)
        {
            StoreName = storeName;
            CNPJ = cnpj;
            Address = address;
            Phone = phone;
            ReceiptFooter = receiptFooter;
            SmallLogoPath = smallLogoPath;
            BigLogoPath = bigLogoPath;
            ServiceCharge = serviceCharge;
        }
    }
}