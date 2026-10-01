using RadianciaKS.Application.DTOs.CashShift;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface ICashShiftExportService
    {
        Task<ExportFileDto> ExportCashShiftToExcelAsync(Guid cashShiftId, CancellationToken cancellationToken = default);
    }
}