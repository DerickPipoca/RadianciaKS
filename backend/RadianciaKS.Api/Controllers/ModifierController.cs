using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs.Modifier;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModifierController : ControllerBase
    {
        private readonly IModifierService _modifierService;

        public ModifierController(IModifierService modifierService)
        {
            _modifierService = modifierService;
        }

        [HttpPost("groups")]
        public async Task<IActionResult> CreateGroup([FromBody] ModifierGroupRequestDto dto)
        {
            var group = await _modifierService.CreateGroupAsync(dto);
            return CreatedAtAction(nameof(GetGroupsByProduct), new { productId = dto.ProductId }, group);
        }

        [HttpPost("groups/{groupId}/options")]
        public async Task<IActionResult> AddOptionToGroup(Guid groupId, [FromBody] ModifierOptionRequestDto dto)
        {
            var option = await _modifierService.AddOptionToGroupAsync(groupId, dto);
            return Ok(option);
        }

        [HttpGet("products/{productId}")]
        public async Task<IActionResult> GetGroupsByProduct(Guid productId)
        {
            var groups = await _modifierService.GetGroupsByProductAsync(productId);
            return Ok(groups);
        }

        [HttpDelete("groups/{groupId}")]
        public async Task<IActionResult> DeleteGroup(Guid groupId)
        {
            await _modifierService.DeleteGroupAsync(groupId);
            return NoContent();
        }

        [HttpDelete("options/{optionId}")]
        public async Task<IActionResult> DeleteOption(Guid optionId)
        {
            await _modifierService.DeleteOptionAsync(optionId);
            return NoContent();
        }
    }
}