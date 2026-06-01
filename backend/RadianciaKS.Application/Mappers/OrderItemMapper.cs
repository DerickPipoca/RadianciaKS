using RadianciaKS.Application.DTOs.Modifier;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Domain.Models;
using Riok.Mapperly.Abstractions;

namespace RadianciaKS.Application.Mappers
{
    [Mapper]
    public partial class OrderItemMapper
    {
        [MapperIgnoreSource("OrderId")]
        [MapperIgnoreSource("Order")]
        [MapperIgnoreSource("ProductId")]
        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("Active")]
        [MapperIgnoreSource("CreatedAt")]
        public partial OrderItemResponseDto ToDto(OrderItem item);

        [MapperIgnoreSource("OrderItemId")]
        [MapperIgnoreSource("OrderItem")]
        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("Active")]
        [MapperIgnoreSource("CreatedAt")]
        public partial OrderItemModifierResponseDto ModifierToDto(OrderItemModifier modifier);

        [MapperIgnoreTarget("Id")]
        [MapperIgnoreTarget("TenantId")]
        [MapperIgnoreTarget("CreatedAt")]
        [MapperIgnoreTarget("Active")]
        [MapperIgnoreTarget("UnitPrice")]
        [MapperIgnoreTarget("Order")]
        [MapperIgnoreTarget("Product")]
        [MapperIgnoreTarget("OrderId")]
        [MapperIgnoreTarget("KdsStatus")]
        [MapperIgnoreTarget("SelectedModifiers")]
        [MapperIgnoreSource("SelectedModifierIds")]
        public partial OrderItem ToEntity(OrderItemRequestDto dto);

    }
}