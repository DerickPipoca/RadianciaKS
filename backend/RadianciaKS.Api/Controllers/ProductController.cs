using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs.Product;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductRequestDto dto)
        {
            var product = await _productService.CreateProduct(dto);
            return CreatedAtAction(nameof(CreateProduct), new { id = product.Id }, product);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _productService.GetAllProducts();

            return Ok(products);
        }
    }
}