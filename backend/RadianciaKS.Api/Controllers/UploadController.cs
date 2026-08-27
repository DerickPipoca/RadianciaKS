using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager")]
    public class UploadController : ControllerBase
    {
        private readonly IImageStorageService _imageStorageService;

        public UploadController(IImageStorageService imageStorageService)
        {
            _imageStorageService = imageStorageService;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromForm] string folderName = "images")
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Nenhum arquivo válido foi enviado para upload.");
            }

            var imageUrl = await _imageStorageService.UploadImageAsync(file, folderName);

            return Ok(new { url = imageUrl });
        }
    }
}