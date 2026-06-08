using RadianciaKS.Application.DTOs.Product;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IProductService
    {
        Task<ProductResponseDto> CreateProduct(ProductRequestDto dto);
        Task<IEnumerable<ProductResponseDto>> GetAllProducts();
        Task<ProductResponseDto> GetProductById(Guid id);
        Task<ProductResponseDto> UpdateProduct(Guid id, ProductRequestDto dto);
        Task DeleteProduct(Guid id);
    }
}