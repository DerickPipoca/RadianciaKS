using RadianciaKS.Application.DTOs.Product;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IProductService
    {
        Task<ProductResponseDto> CreateProduct(ProductRequestDto dto);
        Task<IEnumerable<ProductResponseDto>> GetAllProducts();
    }
}