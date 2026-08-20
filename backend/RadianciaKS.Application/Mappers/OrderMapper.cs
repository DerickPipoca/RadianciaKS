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

        [MapperIgnoreSource(nameof(Order.TenantId))]
        [MapperIgnoreSource(nameof(Order.Active))]
        [MapperIgnoreSource(nameof(Order.PaidById))]
        [MapperIgnoreSource(nameof(Order.CashShift))]
        [MapperIgnoreTarget(nameof(OrderResponseDto.ChangeAmount))]
        [MapProperty(nameof(Order.Employee), nameof(OrderResponseDto.CreatedByName), Use = nameof(MapCreatedByName))]
        [MapProperty(nameof(Order.PaidBy), nameof(OrderResponseDto.PaidByName), Use = nameof(MapPaidByName))]
        public partial OrderResponseDto ToDto(Order order);

        [MapperIgnoreTarget(nameof(Order.Id))]
        [MapperIgnoreTarget(nameof(Order.TenantId))]
        [MapperIgnoreTarget(nameof(Order.CreatedAt))]
        [MapperIgnoreTarget(nameof(Order.Active))]
        [MapperIgnoreTarget(nameof(Order.OrderStatus))]
        [MapperIgnoreTarget(nameof(Order.TotalAmount))]
        [MapperIgnoreTarget(nameof(Order.ReceiptUrl))]
        [MapperIgnoreTarget(nameof(Order.Employee))]
        [MapperIgnoreTarget(nameof(Order.PaymentStatus))]
        [MapperIgnoreTarget(nameof(Order.PaidById))]
        [MapperIgnoreTarget(nameof(Order.PaidBy))]
        [MapperIgnoreTarget(nameof(Order.CashShiftId))]
        [MapperIgnoreTarget(nameof(Order.CashShift))]
        public partial Order ToEntity(OrderRequestDto dto);

    }
}