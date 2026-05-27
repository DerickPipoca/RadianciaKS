using RadianciaKS.Application.DTOs.Payment;

namespace RadianciaKS.Application.DTOs.Order
{
    public class CheckoutRequestDto
    {
        public List<PaymentRequestDto> Payments { get; set; } = [];
    }
}