using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs.Category;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Mappers;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IApplicationDbContext _context;
        private readonly IValidator<CategoryRequestDto> _validator;
        private readonly CategoryMapper _mapper;

        public CategoryService(IApplicationDbContext applicationDbContext, IValidator<CategoryRequestDto> validator)
        {
            _context = applicationDbContext;
            _validator = validator;
            _mapper = new CategoryMapper();
        }

        public async Task<CategoryResponseDto> CreateCategory(CategoryRequestDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);
            var categoryToAdd = _mapper.ToEntity(dto);
            var category = _context.Categories.Add(categoryToAdd);
            await _context.SaveChangesAsync();
            return _mapper.ToDto(category.Entity);
        }

        public async Task DeleteCategory(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                throw new ArgumentException("A categoria informada não existe.");

            category.Active = false;

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<CategoryResponseDto>> GetAllCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return categories.Select(c => _mapper.ToDto(c));
        }

        public async Task<CategoryResponseDto> GetCategoryById(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                throw new ArgumentException("A categoria informada não existe.");

            return _mapper.ToDto(category);
        }

        public async Task<CategoryResponseDto> UpdateCategory(Guid id, CategoryRequestDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                throw new ArgumentException("A categoria informada não existe.");

            category.Update(
                dto.Name,
                dto.ImagePath,
                dto.Priority
            );

            await _context.SaveChangesAsync();
            return _mapper.ToDto(category);
        }
    }
}