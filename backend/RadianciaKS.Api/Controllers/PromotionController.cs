using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.Product;
using RadianciaKS.Application.DTOs.Promotion;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;
        public PromotionController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        [HttpGet("actives")]
        public async Task<IActionResult> GetAllActivePromotions()
        {
            var promotions = await _promotionService.GetActivePromotions();

            return Ok(promotions);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPromotions([FromQuery] ProductQueryParameters queryParameters)
        {
            var promotions = await _promotionService.GetAllPromotions(queryParameters);

            return Ok(promotions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPromotionById(Guid id)
        {
            var promotion = await _promotionService.GetPromotionById(id);
            return Ok(promotion);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePromotion([FromBody] PromotionRequestDto dto)
        {
            var promotion = await _promotionService.CreatePromotion(dto);
            return CreatedAtAction(nameof(GetPromotionById), new { id = promotion.Id }, promotion);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePromotion(Guid id, [FromBody] PromotionRequestDto dto)
        {
            var promotion = await _promotionService.UpdatePromotion(id, dto);
            return Ok(promotion);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromotion(Guid id)
        {
            await _promotionService.DeletePromotion(id);
            return NoContent();
        }

        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> ToggleRunningStatus(Guid id)
        {
            var status = await _promotionService.ToggleRunningStatus(id);
            return Ok(status);
        }
    }
}