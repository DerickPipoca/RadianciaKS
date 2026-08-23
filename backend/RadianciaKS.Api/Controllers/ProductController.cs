using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.Product;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductRequestDto dto)
        {
            var product = await _productService.CreateProduct(dto);
            return CreatedAtAction(nameof(CreateProduct), new { id = product.Id }, product);
        }

        [HttpPost("{id}/duplicate")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DuplicateProduct(Guid id)
        {
            var duplicatedProduct = await _productService.DuplicateProduct(id);
            return Ok(duplicatedProduct);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts([FromQuery] ProductQueryParameters queryParameters)
        {
            var products = await _productService.GetAllProducts(queryParameters);

            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await _productService.GetProductById(id);

            return Ok(product);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] ProductRequestDto dto)
        {
            var product = await _productService.UpdateProduct(id, dto);

            return Ok(product);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            await _productService.DeleteProduct(id);

            return NoContent();
        }

        [HttpPost("upload-image")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                var imageUrl = await _productService.UploadImage(file);

                return Ok(new { url = imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}