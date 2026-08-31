using RadianciaKS.Application.DTOs.Promotion;
using RadianciaKS.Domain.Models;
using Riok.Mapperly.Abstractions;

namespace RadianciaKS.Application.Mappers
{
    [Mapper]
    public partial class PromotionMapper
    {
        [MapperIgnoreTarget("Id")]
        [MapperIgnoreTarget("TenantId")]
        [MapperIgnoreTarget("CreatedAt")]
        [MapperIgnoreTarget("Active")]
        [MapperIgnoreTarget("Running")]
        [MapperIgnoreTarget("BaseProduct")]
        [MapperIgnoreTarget("PromotionModifiers")]
        [MapperIgnoreSource("PromotionModifiers")]
        public partial Promotion ToEntity(PromotionRequestDto dto);
    }
}