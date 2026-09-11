using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrinterController : ControllerBase
    {
        private readonly IPrintService _printService;

        public PrinterController(IPrintService printService)
        {
            _printService = printService;
        }

        [HttpPost("receipt")]
        public async Task<IActionResult> PrintReceipt([FromBody] OrderResponseDto order)
        {
            try
            {
                // Alterado temporariamente para salvar na raiz do projeto da API
                string printerPath = "cupom_teste.bin";

                var success = await _printService.PrintReceiptAsync(order, printerPath);

                if (success)
                {
                    return Ok(new { message = "Cupom gerado com sucesso no arquivo cupom_teste.bin!" });
                }

                return BadRequest(new { message = "Não foi possível completar a rotina." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}