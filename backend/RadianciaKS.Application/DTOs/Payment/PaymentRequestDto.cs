using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.DTOs.Payment
{
    public class PaymentRequestDto
    {
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
    }
}