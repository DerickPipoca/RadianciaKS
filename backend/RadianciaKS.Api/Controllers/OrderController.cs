using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrders();

            return Ok(orders);
        }
    }
}