using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.DTOs.Order
{
    public class OrderQueryParameters : BaseQueryParameters
    {
        public OrderStatus? OrderStatus { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}