using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequestDto dto)
        {
            var order = await _orderService.CreateOrder(dto);
            return CreatedAtAction(nameof(CreateOrder), new { id = order.Id }, order);
        }

        [HttpPost("{orderId}/items")]
        public async Task<IActionResult> AddItemToOrder(Guid orderId, [FromBody] OrderItemRequestDto itemDto)
        {
            var order = await _orderService.AddItemToOrder(orderId, itemDto);
            return CreatedAtAction(nameof(AddItemToOrder), new { id = order.Id }, order);
        }

        [HttpPost("{orderId}/checkout")]
        public async Task<IActionResult> CheckoutOrder(Guid orderId, [FromBody] CheckoutRequestDto checkoutDto)
        {
            var order = await _orderService.CheckoutOrder(orderId, checkoutDto);
            return Ok(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderQueryParameters queryParameters)
        {
            var orders = await _orderService.GetAllOrders(queryParameters);

            return Ok(orders);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _orderService.GetOrderById(orderId);

            return Ok(order);
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetDashboardMetrics([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var startUtc = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
            var metrics = await _orderService.GetDashboardMetricsAsync(startUtc, endUtc);
            return Ok(metrics);
        }

        [HttpPut("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid orderId)
        {
            var order = await _orderService.CancelOrder(orderId);
            return Ok(order);
        }

        [HttpPut("{orderId}/deliver")]
        public async Task<IActionResult> DeliverOrder(Guid orderId)
        {
            var order = await _orderService.DeliverOrder(orderId);
            return Ok(order);
        }

        [HttpDelete("{orderId}/items/{itemId}")]
        public async Task<IActionResult> RemoveItemFromOrder(Guid orderId, Guid itemId)
        {
            var order = await _orderService.RemoveItemFromOrder(orderId, itemId);
            return Ok(order);
        }
    }
}