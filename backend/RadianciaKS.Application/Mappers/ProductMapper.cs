using RadianciaKS.Application.DTOs.Product;
using Riok.Mapperly.Abstractions;
using RadianciaKS.Domain.Models;
using RadianciaKS.Application.DTOs.Modifier;

namespace RadianciaKS.Application.Mappers
{
    [Mapper]
    public partial class ProductMapper
    {
        [MapperIgnoreTarget("IsPromotional")]
        [MapperIgnoreTarget("PromotionalPrice")]
        [MapperIgnoreSource("TenantId")]
        public partial ProductResponseDto ToDto(Product product);

        [MapperIgnoreSource("ProductId")]
        [MapperIgnoreSource("Product")]
        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("Active")]
        [MapperIgnoreSource("CreatedAt")]
        public partial ModifierGroupResponseDto GroupToDto(ModifierGroup group);

        [MapperIgnoreSource("ModifierGroupId")]
        [MapperIgnoreSource("ModifierGroup")]
        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("Active")]
        [MapperIgnoreSource("CreatedAt")]
        [MapperIgnoreTarget("IsPromotional")]
        [MapperIgnoreTarget("PromotionalPrice")]
        public partial ModifierOptionResponseDto OptionToDto(ModifierOption option);

        [MapperIgnoreTarget("Id")]
        [MapperIgnoreTarget("TenantId")]
        [MapperIgnoreTarget("CreatedAt")]
        [MapperIgnoreTarget("Active")]
        [MapperIgnoreTarget("Category")]
        [MapperIgnoreTarget("ModifierGroups")]
        public partial Product ToEntity(ProductRequestDto dto);
    }
}