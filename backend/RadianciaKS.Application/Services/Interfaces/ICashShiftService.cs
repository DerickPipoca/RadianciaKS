using RadianciaKS.Application.DTOs.CashShift;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface ICashShiftService
    {
        Task<CashShiftResponseDto?> GetCurrentOpenShift();
        Task<CashShiftResponseDto> OpenShift(OpenCashShiftDto dto);
        Task<CashShiftResponseDto> CloseShift(CloseCashShiftDto dto);
    }
}