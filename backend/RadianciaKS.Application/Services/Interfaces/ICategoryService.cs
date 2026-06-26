using Microsoft.AspNetCore.Http;
using RadianciaKS.Application.DTOs.Category;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryResponseDto> CreateCategory(CategoryRequestDto dto);
        Task<IEnumerable<CategoryResponseDto>> GetAllCategories();
        Task<CategoryResponseDto> GetCategoryById(Guid id);
        Task<CategoryResponseDto> UpdateCategory(Guid id, CategoryRequestDto dto);
        Task DeleteCategory(Guid id);
        Task<string> UploadImage(IFormFile file);
    }
}