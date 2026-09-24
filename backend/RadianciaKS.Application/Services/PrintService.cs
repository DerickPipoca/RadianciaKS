using System.Globalization;
using System.Net.Sockets;
using System.Text;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using RadianciaKS.Application.DTOs.Modifier;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.DTOs.Payment;
using RadianciaKS.Application.DTOs.StoreSettings;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace RadianciaKS.Application.Services
{
    public class PrintService : IPrintService
    {
        private readonly EPSON _epson;
        private const int PrinterColumns = 48;
        private readonly IStoreSettingsService _storeSettingsService;
        private readonly CultureInfo _culturePtBr = new("pt-BR");

        public PrintService(IStoreSettingsService storeSettingsService)
        {
            _epson = new EPSON();
            _storeSettingsService = storeSettingsService;
        }

        public async Task<bool> PrintReceiptAsync(OrderResponseDto order, string printerPath, byte[]? logoBytes = null)
        {
            try
            {
                var receiptBytes = await DrawReceipt(order, logoBytes);
                var isLinuxDevice = printerPath.StartsWith("/dev/", StringComparison.OrdinalIgnoreCase);

                if (File.Exists(printerPath) || (!isLinuxDevice && Path.HasExtension(printerPath)))
                {
                    var directory = Path.GetDirectoryName(printerPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    await File.WriteAllBytesAsync(printerPath, receiptBytes);
                    return true;
                }

                using var client = new TcpClient();
                await client.ConnectAsync("host.docker.internal", 9100);
                await using var stream = client.GetStream();
                await stream.WriteAsync(receiptBytes);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"[PrintService] Falha na impressão: {ex.Message}");
            }
        }

        private async Task<byte[]> DrawReceipt(OrderResponseDto order, byte[]? logoBytes)
        {
            var settings = await _storeSettingsService.GetSettings() ?? new StoreSettingsResponseDto
            {
                StoreName = "ERRO AO RECEBER DADOS DA LOJA!",
                ServiceCharge = 0m
            };

            var receipt = new List<byte[]>
            {
                _epson.Initialize(),
                _epson.CenterAlign()
            };

            AppendHeader(receipt, settings, logoBytes);
            AppendItems(receipt, order.Items);
            AppendPayments(receipt, order.Payments);
            AppendTotals(receipt, order, settings.ServiceCharge);
            AppendFooter(receipt, order, settings);

            return ByteSplicer.Combine(receipt.ToArray());
        }

        private void AppendHeader(List<byte[]> receipt, StoreSettingsResponseDto settings, byte[]? logoBytes)
        {
            if (logoBytes != null && logoBytes.Length > 0)
            {
                byte[] rasterLogo = ConvertImageToEscPosRaster(logoBytes, maxWidth: 350);
                if (rasterLogo.Length > 0)
                {
                    receipt.Add(rasterLogo);
                }
            }

            receipt.Add(_epson.SetStyles(PrintStyle.Bold | PrintStyle.DoubleWidth | PrintStyle.DoubleHeight));
            receipt.Add(_epson.PrintLine(Sanitize(settings.StoreName)));
            receipt.Add(_epson.SetStyles(PrintStyle.None));
            receipt.Add(_epson.PrintLine(new string('-', PrinterColumns)));

            receipt.Add(_epson.CenterAlign());
            receipt.Add(_epson.PrintLine("PEDIDOS"));
            receipt.Add(_epson.PrintLine(""));
            receipt.Add(_epson.LeftAlign());
        }

        private void AppendItems(List<byte[]> receipt, IEnumerable<OrderItemResponseDto> items)
        {
            foreach (var item in items)
            {
                decimal itemTotal = item.UnitPrice * item.Quantity;
                string priceFormatted = $"R$ {itemTotal.ToString("F2", _culturePtBr)}";

                receipt.Add(_epson.PrintLine(FormatLine($"{item.Quantity}x {Sanitize(item.ProductName)}", priceFormatted)));

                AppendModifiers(receipt, item.SelectedModifiers);
                AppendPromotionalSavings(receipt, item);
            }

            receipt.Add(_epson.LeftAlign());
            receipt.Add(_epson.PrintLine(new string('-', PrinterColumns)));
        }

        private void AppendModifiers(List<byte[]> receipt, IEnumerable<OrderItemModifierResponseDto>? modifiers)
        {
            if (modifiers == null || !modifiers.Any()) return;

            var groupedModifiers = modifiers
                .GroupBy(m => string.IsNullOrWhiteSpace(m.GroupName) ? "Modificadores" : m.GroupName);

            foreach (var group in groupedModifiers)
            {
                receipt.Add(_epson.PrintLine($"  {Sanitize(group.Key)}:"));
                foreach (var mod in group)
                {
                    receipt.Add(_epson.PrintLine(FormatLine($"  + {Sanitize(mod.Name)}", "(Incl.)")));
                }
            }
        }

        private void AppendPromotionalSavings(List<byte[]> receipt, OrderItemResponseDto item)
        {
            decimal baseOriginal = item.OriginalUnitPrice ?? item.UnitPrice;
            decimal modifiersOriginal = item.SelectedModifiers?
                .Sum(m => m.OriginalAdditionalPrice ?? m.AdditionalPrice) ?? 0m;

            decimal unitOriginalTotal = baseOriginal + modifiersOriginal;
            decimal unitChargedTotal = item.UnitPrice;

            if (unitOriginalTotal <= unitChargedTotal) return;

            decimal unitSavings = unitOriginalTotal - unitChargedTotal;
            decimal totalSavings = unitSavings * item.Quantity;

            string suffix = item.Quantity > 1 ? " un" : "";
            string promoText = $"  (Era: R$ {unitOriginalTotal.ToString("F2", _culturePtBr)}{suffix} | Economia: R$ {totalSavings.ToString("F2", _culturePtBr)})";

            if (promoText.Length > PrinterColumns)
            {
                promoText = $"  (Era: R$ {unitOriginalTotal.ToString("F2", _culturePtBr)}{suffix} | Econ: R$ {totalSavings.ToString("F2", _culturePtBr)})";
            }

            receipt.Add(_epson.PrintLine(Sanitize(promoText)));
        }

        private void AppendPayments(List<byte[]> receipt, IEnumerable<PaymentResponseDto>? payments)
        {
            receipt.Add(_epson.CenterAlign());
            receipt.Add(_epson.PrintLine("PAGAMENTOS"));
            receipt.Add(_epson.PrintLine(""));
            receipt.Add(_epson.LeftAlign());

            if (payments != null && payments.Any())
            {
                foreach (var payment in payments)
                {
                    string paymentAmount = $"R$ {payment.Amount.ToString("F2", _culturePtBr)}";
                    string methodName = Sanitize(GetPaymentMethodName(payment.Method));
                    receipt.Add(_epson.PrintLine(FormatLine($"+ {methodName}", paymentAmount)));
                }
            }
            else
            {
                receipt.Add(_epson.CenterAlign());
                receipt.Add(_epson.SetStyles(PrintStyle.Bold));
                receipt.Add(_epson.PrintLine("NAO PAGO"));
                receipt.Add(_epson.SetStyles(PrintStyle.None));
                receipt.Add(_epson.LeftAlign());
            }

            receipt.Add(_epson.LeftAlign());
            receipt.Add(_epson.PrintLine(new string('-', PrinterColumns)));
        }

        private void AppendTotals(List<byte[]> receipt, OrderResponseDto order, decimal serviceChargePercent)
        {
            if (order.ServiceFeeAmount > 0)
            {
                receipt.Add(_epson.RightAlign());
                receipt.Add(_epson.SetStyles(PrintStyle.Bold));
                receipt.Add(_epson.PrintLine(Sanitize($"Taxa ({serviceChargePercent}%): R$ {order.ServiceFeeAmount.ToString("F2", _culturePtBr)}")));
            }

            receipt.Add(_epson.RightAlign());
            receipt.Add(_epson.SetStyles(PrintStyle.DoubleHeight | PrintStyle.DoubleWidth | PrintStyle.Bold));
            receipt.Add(_epson.PrintLine($"TOTAL: R$ {order.TotalAmount.ToString("F2", _culturePtBr)}"));
            receipt.Add(_epson.SetStyles(PrintStyle.None));
            receipt.Add(_epson.PrintLine(""));
        }

        private void AppendFooter(List<byte[]> receipt, OrderResponseDto order, StoreSettingsResponseDto settings)
        {
            if (!string.IsNullOrWhiteSpace(order.ReceiptUrl))
            {
                receipt.Add(_epson.CenterAlign());
                receipt.Add(_epson.PrintLine("Consulte sua Nota Fiscal:"));
                receipt.Add(_epson.PrintLine(""));
                receipt.Add(_epson.PrintQRCode(order.ReceiptUrl));
                receipt.Add(_epson.PrintLine(""));
            }

            string paidBy = string.IsNullOrEmpty(order.PaidByName) ? "N/A" : Sanitize(order.PaidByName);
            string createdBy = string.IsNullOrEmpty(order.CreatedByName) ? "N/A" : Sanitize(order.CreatedByName);

            receipt.Add(_epson.CenterAlign());
            receipt.Add(_epson.PrintLine($"{order.CreatedAt:dd/MM/yyyy | HH:mm}"));
            receipt.Add(_epson.PrintLine(Sanitize($"Garcom: {createdBy} | Caixa: {paidBy}")));

            if (!string.IsNullOrWhiteSpace(settings.Address))
                receipt.Add(_epson.PrintLine(Sanitize($"Endereco: {settings.Address}")));

            if (!string.IsNullOrWhiteSpace(settings.CNPJ))
                receipt.Add(_epson.PrintLine($"CNPJ: {settings.CNPJ}"));

            if (!string.IsNullOrEmpty(settings.ReceiptFooter))
                receipt.Add(_epson.PrintLine(Sanitize(settings.ReceiptFooter)));

            receipt.Add(_epson.PrintLine("SISTEMA RADIANCIA KS"));
            receipt.Add(_epson.PrintLine("Acesse: www.radianciasistemas.com.br"));

            receipt.Add(_epson.PrintLine(""));
            receipt.Add(_epson.PrintLine(""));
            receipt.Add(_epson.PrintLine(""));
            receipt.Add(_epson.PrintLine(""));
            receipt.Add(_epson.PrintLine(""));
            receipt.Add(_epson.FullCut());
        }

        private string FormatLine(string left, string right)
        {
            int maxLeftLength = PrinterColumns - right.Length - 1;

            if (left.Length > maxLeftLength)
            {
                left = left.Substring(0, Math.Max(0, maxLeftLength - 3)) + "...";
            }

            int spaceCount = PrinterColumns - (left.Length + right.Length);
            if (spaceCount < 1) spaceCount = 1;

            return left + new string(' ', spaceCount) + right;
        }

        private string GetPaymentMethodName(PaymentMethod methodId)
        {
            return methodId switch
            {
                PaymentMethod.Cash => "Dinheiro",
                PaymentMethod.Pix => "PIX",
                PaymentMethod.CreditCard => "Credito",
                PaymentMethod.DebitCard => "Debito",
                _ => "Outro"
            };
        }

        private static byte[] ConvertImageToEscPosRaster(byte[] imageBytes, int maxWidth = 350)
        {
            try
            {
                using var image = Image.Load<Rgba32>(imageBytes);

                if (image.Width > maxWidth)
                {
                    double ratio = (double)maxWidth / image.Width;
                    int newHeight = (int)(image.Height * ratio);
                    image.Mutate(ctx => ctx.Resize(maxWidth, newHeight));
                }

                int width = image.Width;
                int height = image.Height;
                int bytesPerLine = (width + 7) / 8;

                byte[] rasterPayload = new byte[bytesPerLine * height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var pixel = image[x, y];

                        if (pixel.A > 128)
                        {
                            double luminance = (0.299 * pixel.R) + (0.587 * pixel.G) + (0.114 * pixel.B);
                            if (luminance < 165)
                            {
                                int byteIndex = (y * bytesPerLine) + (x / 8);
                                int bitIndex = 7 - (x % 8);
                                rasterPayload[byteIndex] |= (byte)(1 << bitIndex);
                            }
                        }
                    }
                }

                byte xL = (byte)(bytesPerLine & 0xFF);
                byte xH = (byte)((bytesPerLine >> 8) & 0xFF);
                byte yL = (byte)(height & 0xFF);
                byte yH = (byte)((height >> 8) & 0xFF);

                byte[] header = new byte[] { 0x1D, 0x76, 0x30, 0x00, xL, xH, yL, yH };

                byte[] result = new byte[header.Length + rasterPayload.Length];
                Buffer.BlockCopy(header, 0, result, 0, header.Length);
                Buffer.BlockCopy(rasterPayload, 0, result, header.Length, rasterPayload.Length);

                return result;
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private static string Sanitize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}