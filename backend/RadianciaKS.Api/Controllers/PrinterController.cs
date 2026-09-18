using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.Services;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrinterController : ControllerBase
    {
        private readonly IPrintService _printService;
        private readonly IStoreSettingsService _storeSettingsService;

        public PrinterController(IPrintService printService, IStoreSettingsService storeSettingsService)
        {
            _printService = printService;
            _storeSettingsService = storeSettingsService;
        }

        [HttpPost("receipt")]
        public async Task<IActionResult> PrintReceipt([FromBody] OrderResponseDto order)
        {
            try
            {
                // Alterado temporariamente para salvar na raiz do projeto da API
                string printerPath = "/dev/usb/lp0";

                var settings = await _storeSettingsService.GetSettings();
                byte[]? logoBytes = null;


                if (!string.IsNullOrWhiteSpace(settings.SmallLogoPath))
                {
                    string cleanPath = settings.SmallLogoPath.TrimStart('/', '\\');
                    string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", cleanPath);

                    if (System.IO.File.Exists(fullPath))
                    {
                        logoBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                    }
                }

                var success = await _printService.PrintReceiptAsync(order, printerPath, logoBytes);

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