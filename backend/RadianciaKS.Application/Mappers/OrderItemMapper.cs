using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Domain.Models;
using Riok.Mapperly.Abstractions;

namespace RadianciaKS.Application.Mappers
{
    [Mapper]
    public partial class OrderItemMapper
    {
        [MapperIgnoreSource("UnitPrice")]
        [MapperIgnoreSource("OrderId")]
        [MapperIgnoreSource("Order")]
        [MapperIgnoreSource("ProductId")]
        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("Active")]
        [MapperIgnoreSource("CreatedAt")]
        public partial OrderItemResponseDto ToDto(OrderItem item);

        [MapperIgnoreTarget("Id")]
        [MapperIgnoreTarget("TenantId")]
        [MapperIgnoreTarget("CreatedAt")]
        [MapperIgnoreTarget("Active")]
        [MapperIgnoreTarget("UnitPrice")]
        [MapperIgnoreTarget("Order")]
        [MapperIgnoreTarget("Product")]
        [MapperIgnoreTarget("OrderId")]
        [MapperIgnoreTarget("KdsStatus")]
        public partial OrderItem ToEntity(OrderItemRequestDto dto);

    }
}