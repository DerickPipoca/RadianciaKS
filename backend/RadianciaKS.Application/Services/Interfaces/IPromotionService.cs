using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.Product;
using RadianciaKS.Application.DTOs.Promotion;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IPromotionService
    {
        Task<IEnumerable<PromotionResponseDto>> GetActivePromotions();
        Task<PagedResponse<PromotionResponseDto>> GetAllPromotions(ProductQueryParameters queryParameters);
        Task<PromotionResponseDto> GetPromotionById(Guid id);
        Task<PromotionResponseDto> CreatePromotion(PromotionRequestDto dto);
        Task<PromotionResponseDto> UpdatePromotion(Guid id, PromotionRequestDto dto);
        Task<bool> ToggleRunningStatus(Guid id);
        Task<bool> DeletePromotion(Guid id);
    }
}