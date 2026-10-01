using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.CashShift;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CashShiftController : ControllerBase
    {
        private readonly ICashShiftService _cashShiftService;
        private readonly ICashShiftExportService _cashShiftExportService;

        public CashShiftController(ICashShiftService cashShiftService, ICashShiftExportService cashShiftExportService)
        {
            _cashShiftService = cashShiftService;
            _cashShiftExportService = cashShiftExportService;
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

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetHistory([FromQuery] BaseQueryParameters queryParameters)
        {
            queryParameters ??= new BaseQueryParameters();
            if (queryParameters.PageNumber <= 0) queryParameters.PageNumber = 1;
            if (queryParameters.PageSize <= 0) queryParameters.PageSize = 30;

            var history = await _cashShiftService.GetCashShiftHistoryAsync(queryParameters);
            return Ok(history);
        }

        [HttpGet("{id:guid}/export-excel")]
        public async Task<IActionResult> ExportToExcel([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var fileResult = await _cashShiftExportService.ExportCashShiftToExcelAsync(id, cancellationToken);
            return File(fileResult.Content, fileResult.ContentType, fileResult.FileName);
        }
    }
}