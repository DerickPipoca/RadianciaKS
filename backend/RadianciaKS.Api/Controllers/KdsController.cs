using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KdsController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public KdsController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingItems()
        {
            var items = await _orderService.GetPendingKdsItemsAsync();
            return Ok(items);
        }

        [HttpPut("{orderId}/items/{itemId}/status")]
        public async Task<IActionResult> UpdateItemStatus(Guid orderId, Guid itemId, [FromBody] KdsStatus status)
        {
            var result = await _orderService.UpdateItemStatus(orderId, itemId, status);
            return Ok(result);
        }
    }
}