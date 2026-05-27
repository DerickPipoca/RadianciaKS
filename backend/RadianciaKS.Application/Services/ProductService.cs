using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs.Product;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Mappers;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IApplicationDbContext _context;
        private readonly IValidator<ProductRequestDto> _validator;
        private readonly ProductMapper _mapper;

        public ProductService(IApplicationDbContext applicationDbContext, IValidator<ProductRequestDto> validator)
        {
            _context = applicationDbContext;
            _validator = validator;
            _mapper = new ProductMapper();
        }

        public async Task<ProductResponseDto> CreateProduct(ProductRequestDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);

            var category = await _context.Categories.FindAsync(dto.CategoryId);
            if (category == null)
                throw new ArgumentException("A Categoria informada não existe.");

            var productToAdd = _mapper.ToEntity(dto);

            productToAdd.Category = category;

            var product = _context.Products.Add(productToAdd);
            await _context.SaveChangesAsync();
            return _mapper.ToDto(product.Entity);
        }

        public async Task<IEnumerable<ProductResponseDto>> GetAllProducts()
        {
            var products = await _context.Products
                            .Include(p => p.Category)
                            .ToListAsync();
            return products.Select(p => _mapper.ToDto(p));
        }
    }
}