using System.Globalization;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.Services
{
    public class PrintService : IPrintService
    {
        private readonly EPSON _epson;
        private const int PrinterColumns = 48;
        private readonly IStoreSettingsService _storeSettingsService;

        public PrintService(IStoreSettingsService storeSettingsService)
        {
            _epson = new EPSON();
            _storeSettingsService = storeSettingsService;
        }

        public async Task<bool> PrintReceiptAsync(OrderResponseDto order, string printerPath)
        {
            try
            {
                var receiptBytes = await DrawReceipt(order);

                await File.WriteAllBytesAsync(printerPath, receiptBytes);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"[PrintService] Falha na impressão: {ex.Message}");
            }
        }

        private async Task<byte[]> DrawReceipt(OrderResponseDto order)
        {
            var settings = await _storeSettingsService.GetSettings();
            var receipt = new List<byte[]>
            {
                _epson.Initialize(),
                _epson.CenterAlign(),
                _epson.SetStyles(PrintStyle.Bold | PrintStyle.DoubleWidth | PrintStyle.DoubleHeight),
                _epson.PrintLine(settings.StoreName),
                _epson.SetStyles(PrintStyle.None),
                _epson.PrintLine(new string('-', PrinterColumns)),

                _epson.CenterAlign(),
                _epson.PrintLine("Pedidos"),
                _epson.PrintLine(""),
                _epson.LeftAlign()
            };

            foreach (var item in order.Items)
            {
                decimal itemTotal = item.UnitPrice * item.Quantity;
                string priceFormatted = $"R$ {itemTotal.ToString("F2", CultureInfo.InvariantCulture)}";

                receipt.Add(_epson.PrintLine(FormatLine($"{item.Quantity}x {item.ProductName}", priceFormatted)));

                if (item.SelectedModifiers != null && item.SelectedModifiers.Any())
                {
                    foreach (var mod in item.SelectedModifiers)
                    {
                        receipt.Add(_epson.PrintLine($"  + {mod.Name}"));
                    }
                }
            }

            receipt.Add(_epson.PrintLine(new string('-', PrinterColumns)));
            receipt.Add(_epson.CenterAlign());
            receipt.Add(_epson.PrintLine("Pagamentos"));
            receipt.Add(_epson.PrintLine(""));
            receipt.Add(_epson.LeftAlign());

            if (order.Payments != null && order.Payments.Any())
            {
                foreach (var payment in order.Payments)
                {
                    string paymentAmount = $"R$ {payment.Amount.ToString("F2", CultureInfo.InvariantCulture)}";
                    receipt.Add(_epson.PrintLine(FormatLine($"+ {GetPaymentMethodName(payment.Method)}", paymentAmount)));
                }
            }
            else
            {
                receipt.Add(_epson.CenterAlign());
                receipt.Add(_epson.SetStyles(PrintStyle.Bold));
                receipt.Add(_epson.PrintLine("NÃO PAGO"));
                receipt.Add(_epson.SetStyles(PrintStyle.None));
            }

            receipt.Add(_epson.LeftAlign());
            receipt.Add(_epson.PrintLine(new string('-', PrinterColumns)));

            decimal serviceChargeValue = order.ServiceFeeAmount;

            receipt.Add(_epson.RightAlign());
            receipt.Add(_epson.SetStyles(PrintStyle.Bold));
            receipt.Add(_epson.PrintLine($"Taxa ({settings.ServiceCharge}%): R$ {serviceChargeValue.ToString("F2", CultureInfo.InvariantCulture)}"));

            receipt.Add(_epson.SetStyles(PrintStyle.DoubleHeight | PrintStyle.DoubleWidth | PrintStyle.Bold));
            receipt.Add(_epson.PrintLine($"TOTAL: R$ {order.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)}"));
            receipt.Add(_epson.SetStyles(PrintStyle.None));
            receipt.Add(_epson.PrintLine(""));

            receipt.Add(_epson.CenterAlign());
            receipt.Add(_epson.PrintLine("Consulte sua Nota Fiscal:"));
            receipt.Add(_epson.PrintLine(""));

            string qrCodeUrl = !string.IsNullOrEmpty(order.ReceiptUrl) ? order.ReceiptUrl : "https://sua-url-fiscal.com/nfce/pendente";
            receipt.Add(_epson.PrintQRCode(qrCodeUrl));
            receipt.Add(_epson.PrintLine(""));

            string paidBy = string.IsNullOrEmpty(order.PaidByName) ? "n/a" : order.PaidByName;

            receipt.Add(_epson.CenterAlign());
            receipt.Add(_epson.PrintLine($"{order.CreatedAt:dd/MM/yyyy | HH:mm}"));
            receipt.Add(_epson.PrintLine($"Garçom: {order.CreatedByName} | Cobrador: {paidBy}"));
            receipt.Add(_epson.PrintLine($"Endereço: {settings.Address}"));
            receipt.Add(_epson.PrintLine($"CNPJ: {settings.CNPJ}"));

            if (!string.IsNullOrEmpty(settings.ReceiptFooter))
            {
                receipt.Add(_epson.PrintLine(settings.ReceiptFooter));
            }

            receipt.Add(_epson.PrintLine("SISTEMA RADIÂNCIA"));

            receipt.Add(_epson.PrintLine(""));
            receipt.Add(_epson.PrintLine(""));
            receipt.Add(_epson.FullCut());

            return ByteSplicer.Combine(receipt.ToArray());
        }

        private string FormatLine(string left, string right)
        {
            int maxLeftLength = PrinterColumns - right.Length - 1;

            if (left.Length > maxLeftLength)
            {
                left = left.Substring(0, maxLeftLength - 3) + "...";
            }

            int spaceCount = PrinterColumns - (left.Length + right.Length);
            return left + new string(' ', spaceCount) + right;
        }

        private string GetPaymentMethodName(PaymentMethod methodId)
        {
            return methodId switch
            {
                PaymentMethod.Cash => "Dinheiro",
                PaymentMethod.Pix => "PIX",
                PaymentMethod.CreditCard => "Crédito",
                PaymentMethod.DebitCard => "Débito",
                _ => "Outro"
            };
        }
    }
}