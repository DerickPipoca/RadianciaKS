using RadianciaKS.Application.DTOs.Category;
using RadianciaKS.Domain.Models;
using Riok.Mapperly.Abstractions;

namespace RadianciaKS.Application.Mappers
{
    [Mapper]
    public partial class CategoryMapper
    {
        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("CreatedAt")]
        [MapperIgnoreSource("Active")]
        public partial CategoryResponseDto ToDto(Category category);

        [MapperIgnoreTarget("Id")]
        [MapperIgnoreTarget("TenantId")]
        [MapperIgnoreTarget("CreatedAt")]
        [MapperIgnoreTarget("Active")]
        public partial Category ToEntity(CategoryRequestDto dto);
    }
}