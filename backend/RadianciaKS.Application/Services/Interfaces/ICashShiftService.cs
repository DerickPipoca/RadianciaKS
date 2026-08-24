using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.CashShift;
using RadianciaKS.Application.DTOs.DashboardMetrics;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface ICashShiftService
    {
        Task<CashShiftResponseDto?> GetCurrentOpenShift();
        Task<PagedResponse<CashShiftHistoryDto>> GetCashShiftHistoryAsync(BaseQueryParameters parameters);
        Task<CashShiftResponseDto> OpenShift(OpenCashShiftDto dto);
        Task<CashShiftResponseDto> CloseShift(CloseCashShiftDto dto);
    }
}