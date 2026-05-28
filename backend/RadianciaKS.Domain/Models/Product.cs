namespace RadianciaKS.Domain.Models
{
    public class Product : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public void Update(string name, string? imagePath, string? description, decimal price, Guid categoryId)
        {
            Name = name;
            ImagePath = imagePath;
            Description = description;
            Price = price;
            CategoryId = categoryId;
        }
    }
}