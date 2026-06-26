using RadianciaKS.Application.DTOs.StoreSettings;
using RadianciaKS.Domain.Models;
using Riok.Mapperly.Abstractions;

namespace RadianciaKS.Application.Mappers
{
    [Mapper]
    public partial class StoreSettingsMapper
    {
        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("CreatedAt")]
        [MapperIgnoreSource("Active")]
        public partial StoreSettingsResponseDto ToDto(StoreSettings storeSettings);

        [MapperIgnoreTarget("Id")]
        [MapperIgnoreTarget("TenantId")]
        [MapperIgnoreTarget("CreatedAt")]
        [MapperIgnoreTarget("Active")]
        public partial StoreSettings ToEntity(StoreSettingsRequestDto dto);
    }
}