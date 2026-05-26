using RadianciaKS.Application.DTOs.Product;
using Riok.Mapperly.Abstractions;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Application.Mappers
{
    [Mapper]
    public partial class ProductMapper
    {
        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("CreatedAt")]
        public partial ProductResponseDto ToDto(Product product);

        [MapperIgnoreTarget("Id")]
        [MapperIgnoreTarget("TenantId")]
        [MapperIgnoreTarget("CreatedAt")]
        [MapperIgnoreTarget("Active")]
        [MapperIgnoreTarget("Category")]
        public partial Product ToEntity(ProductRequestDto dto);
    }
}