namespace RadianciaKS.Domain.Models
{
    public class OrderItemModifier : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public decimal AdditionalPrice { get; set; } = 0m;

        public Guid OrderItemId { get; set; }
        public OrderItem OrderItem { get; set; } = null!;
    }
}