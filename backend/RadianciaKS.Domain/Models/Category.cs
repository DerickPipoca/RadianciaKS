namespace RadianciaKS.Domain.Models
{
    public class Category : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public int? Priority { get; set; }

        public void Update(string name, string? imagePath, int? priority)
        {
            Name = name;
            ImagePath = imagePath;
            Priority = priority;
        }
    }
}