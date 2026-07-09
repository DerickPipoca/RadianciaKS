using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.Product;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IProductService
    {
        Task<ProductResponseDto> CreateProduct(ProductRequestDto dto);
        Task<string> UploadImage(IFormFile file);
        Task<PagedResponse<ProductResponseDto>> GetAllProducts(ProductQueryParameters queryParameters);
        Task<ProductResponseDto> GetProductById(Guid id);
        Task<ProductResponseDto> UpdateProduct(Guid id, ProductRequestDto dto);
        Task DeleteProduct(Guid id);
    }
}