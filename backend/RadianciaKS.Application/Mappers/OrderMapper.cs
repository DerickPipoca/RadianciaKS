using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Domain.Models;
using Riok.Mapperly.Abstractions;

namespace RadianciaKS.Application.Mappers
{
    [Mapper]
    public partial class OrderMapper
    {
        private string MapCreatedByName(Employee? employee) => employee?.Name ?? "Sistema";
        private string? MapPaidByName(Employee? employee) => employee?.Name;

        [UseMapper]
        private readonly OrderItemMapper _itemMapper = new();
        [UseMapper]
        private readonly PaymentMapper _paymentMapper = new();

        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("Active")]
        [MapperIgnoreSource("PaidById")]
        [MapperIgnoreTarget("ChangeAmount")]
        [MapProperty(nameof(Order.Employee), nameof(OrderResponseDto.CreatedByName), Use = nameof(MapCreatedByName))]
        [MapProperty(nameof(Order.PaidBy), nameof(OrderResponseDto.PaidByName), Use = nameof(MapPaidByName))]
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
        [MapperIgnoreTarget("PaidById")]
        [MapperIgnoreTarget("PaidBy")]
        public partial Order ToEntity(OrderRequestDto dto);

    }
}