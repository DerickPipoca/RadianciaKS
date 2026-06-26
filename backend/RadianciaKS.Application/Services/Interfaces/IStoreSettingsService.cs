using Microsoft.AspNetCore.Http;
using RadianciaKS.Application.DTOs.StoreSettings;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IStoreSettingsService
    {
        Task<StoreSettingsResponseDto> GetSettings();
        Task<StoreSettingsResponseDto> UpdateSettings(StoreSettingsRequestDto dto);
        Task<string> UploadLogo(IFormFile file, bool isBig);
    }
}