using RadianciaKS.Application.DTOs.Category;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryResponseDto> CreateCategory(CategoryRequestDto dto);
        Task<IEnumerable<CategoryResponseDto>> GetAllCategories();
    }
}