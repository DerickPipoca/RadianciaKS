using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.Product;
using RadianciaKS.Application.Extensions;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Mappers;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IApplicationDbContext _context;
        private readonly IValidator<ProductRequestDto> _validator;
        private readonly IImageStorageService _imageStorageService;
        private readonly ProductMapper _mapper;

        public ProductService(IApplicationDbContext applicationDbContext, IValidator<ProductRequestDto> validator, IImageStorageService imageStorageService)
        {
            _context = applicationDbContext;
            _validator = validator;
            _imageStorageService = imageStorageService;
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

        public async Task DeleteProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new ArgumentException("O Produto informado não existe.");

            product.Active = false;

            await _context.SaveChangesAsync();
        }

        public async Task<PagedResponse<ProductResponseDto>> GetAllProducts(ProductQueryParameters queryParameters)
        {
            var query = _context.Products
                            .Include(p => p.Category)
                            .Include(p => p.ModifierGroups).ThenInclude(m => m.Options)
                            .AsQueryable();

            if (queryParameters.CategoryId != null)
            {
                query = query.Where(p => p.CategoryId == queryParameters.CategoryId);
            }

            if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
            {
                var term = queryParameters.SearchTerm.ToLower();
                query = query.Where(p => p.Name != null && p.Name.ToLower().Contains(term));
            }

            query = query.ApplySorting(queryParameters.SortBy, queryParameters.IsDescending, (sortBy, descending) => sortBy switch
            {
                "name" => descending ? query.OrderByDescending(p => p.Name.ToLower()) : query.OrderBy(p => p.Name.ToLower()),
                "categoryName" => descending ? query.OrderByDescending(p => p.Category.Name.ToLower()) : query.OrderBy(p => p.Category.Name.ToLower()),
                "price" => descending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                _ => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
            });

            var totalRecords = await query.CountAsync();

            var products = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync();

            return new PagedResponse<ProductResponseDto>
            {
                Data = products.Select(c => _mapper.ToDto(c)),
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize,
                TotalRecords = totalRecords
            };
        }

        public async Task<ProductResponseDto> GetProductById(Guid id)
        {
            var product = await _context.Products
                            .Include(p => p.Category)
                            .Include(p => p.ModifierGroups).ThenInclude(m => m.Options)
                            .FirstOrDefaultAsync(x => x.Id == id);
            if (product == null)
                throw new ArgumentException("O Produto informado não existe.");
            return _mapper.ToDto(product);
        }

        public async Task<ProductResponseDto> UpdateProduct(Guid id, ProductRequestDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new ArgumentException("O Produto informado não existe.");

            var category = await _context.Categories.FindAsync(dto.CategoryId);
            if (category == null)
                throw new ArgumentException("A Categoria informada não existe.");

            product.Update(
                dto.Name,
                dto.ImagePath,
                dto.Description,
                dto.Price,
                dto.CategoryId);

            await _context.SaveChangesAsync();
            return _mapper.ToDto(product);
        }

        public async Task<string> UploadImage(IFormFile file)
        {
            var imageUrl = await _imageStorageService.UploadImageAsync(file, "products");
            return imageUrl;
        }
    }
}