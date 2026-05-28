using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KdsController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public KdsController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPut("{orderId}/items/{itemId}/status")]
        public async Task<IActionResult> UpdateItemStatus(Guid orderId, Guid itemId, [FromBody] KdsStatus status)
        {
            var result = await _orderService.UpdateItemStatusAsync(orderId, itemId, status);
            return Ok(result);
        }
    }
}