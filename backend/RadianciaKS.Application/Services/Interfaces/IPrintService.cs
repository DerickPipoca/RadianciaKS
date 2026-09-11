using RadianciaKS.Application.DTOs.Order;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IPrintService
    {
        Task<bool> PrintReceiptAsync(OrderResponseDto order, string printerName = "GenericPrinter");
    }
}