using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs.StoreSettings;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreSettingsController : ControllerBase
    {
        private readonly IStoreSettingsService _storeSettingsService;
        public StoreSettingsController(IStoreSettingsService storeSettingsService)
        {
            _storeSettingsService = storeSettingsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStoreSettings()
        {
            var storeSettings = await _storeSettingsService.GetSettings();

            return Ok(storeSettings);
        }

        [HttpPut()]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateProduct([FromBody] StoreSettingsRequestDto dto)
        {
            var storeSettings = await _storeSettingsService.UpdateSettings(dto);

            return Ok(storeSettings);
        }

        [HttpPost("upload-image")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] bool isBig)
        {
            try
            {
                var imageUrl = await _storeSettingsService.UploadLogo(file, isBig);
                return Ok(new { url = imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}