using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs.Category;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryRequestDto dto)
        {
            var category = await _categoryService.CreateCategory(dto);
            return CreatedAtAction(nameof(CreateCategory), new { id = category.Id }, category);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategories();

            return Ok(categories);
        }
    }
}