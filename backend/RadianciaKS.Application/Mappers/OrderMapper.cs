using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Domain.Models;
using Riok.Mapperly.Abstractions;

namespace RadianciaKS.Application.Mappers
{
    [Mapper]
    public partial class OrderMapper
    {
        [UseMapper]
        private readonly OrderItemMapper _itemMapper = new();
        [UseMapper]
        private readonly PaymentMapper _paymentMapper = new();

        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("CreatedAt")]
        [MapperIgnoreSource("Active")]
        [MapperIgnoreTarget("ChangeAmount")]
        [MapperIgnoreSource("Employee")]
        public partial OrderResponseDto ToDto(Order order);

        [MapperIgnoreTarget("Id")]
        [MapperIgnoreTarget("TenantId")]
        [MapperIgnoreTarget("CreatedAt")]
        [MapperIgnoreTarget("Active")]
        [MapperIgnoreTarget("OrderStatus")]
        [MapperIgnoreTarget("TotalAmount")]
        [MapperIgnoreTarget("ReceiptUrl")]
        [MapperIgnoreTarget("Employee")]
        [MapperIgnoreTarget("PaymentStatus")]
        public partial Order ToEntity(OrderRequestDto dto);

    }
}