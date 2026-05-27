using RadianciaKS.Application.DTOs.Payment;
using RadianciaKS.Domain.Models;
using Riok.Mapperly.Abstractions;

namespace RadianciaKS.Application.Mappers
{
    [Mapper]
    public partial class PaymentMapper
    {
        [MapperIgnoreSource("Id")]
        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("CreatedAt")]
        [MapperIgnoreSource("Active")]
        [MapperIgnoreSource("OrderId")]
        [MapperIgnoreSource("Order")]
        public partial PaymentResponseDto ToDto(Payment order);

        [MapperIgnoreTarget("Id")]
        [MapperIgnoreTarget("TenantId")]
        [MapperIgnoreTarget("CreatedAt")]
        [MapperIgnoreTarget("Active")]
        [MapperIgnoreTarget("OrderId")]
        [MapperIgnoreTarget("Order")]
        public partial Payment ToEntity(PaymentRequestDto dto);

    }
}