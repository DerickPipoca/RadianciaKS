using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs.CashShift;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CashShiftController : ControllerBase
    {
        private readonly ICashShiftService _cashShiftService;

        public CashShiftController(ICashShiftService cashShiftService)
        {
            _cashShiftService = cashShiftService;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentOpenShift()
        {
            var shift = await _cashShiftService.GetCurrentOpenShift();
            if (shift == null) return NoContent();
            return Ok(shift);
        }

        [HttpPost("open")]
        [Authorize]
        public async Task<IActionResult> OpenShift([FromBody] OpenCashShiftDto dto)
        {
            var shift = await _cashShiftService.OpenShift(dto);
            return Ok(shift);
        }

        [HttpPost("close")]
        [Authorize]
        public async Task<IActionResult> CloseShift([FromBody] CloseCashShiftDto dto)
        {
            var shift = await _cashShiftService.CloseShift(dto);
            return Ok(shift);
        }
    }
}