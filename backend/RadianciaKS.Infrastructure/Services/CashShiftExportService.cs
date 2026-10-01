using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.SS.UserModel.Charts;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using RadianciaKS.Application.DTOs.CashShift;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Services
{
    public class CashShiftExportService : ICashShiftExportService
    {
        private readonly IApplicationDbContext _context;

        public CashShiftExportService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ExportFileDto> ExportCashShiftToExcelAsync(Guid cashShiftId, CancellationToken cancellationToken = default)
        {
            var cashShift = await _context.CashShifts
                .AsNoTracking()
                .FirstOrDefaultAsync(cs => cs.Id == cashShiftId, cancellationToken);

            if (cashShift == null)
            {
                throw new KeyNotFoundException($"Turno de caixa {cashShiftId} não encontrado.");
            }

            var orders = await _context.Orders
                .Include(o => o.Payments)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Where(o => o.CashShiftId == cashShiftId && o.PaymentStatus == PaymentStatus.Paid)
                .OrderBy(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var workbook = new XSSFWorkbook();
            var styles = CreateStyles(workbook);

            BuildSummarySheet(workbook, cashShift, orders, styles);
            BuildOrdersSheet(workbook, orders, styles);
            BuildItemsSheet(workbook, orders, styles);

            using var memoryStream = new MemoryStream();
            workbook.Write(memoryStream);

            var dateStr = (cashShift.ClosedAt ?? cashShift.CreatedAt).ToString("yyyyMMdd_HHmm");
            var fileName = $"Fechamento_Caixa_{dateStr}.xlsx";

            return new ExportFileDto
            {
                Content = memoryStream.ToArray(),
                FileName = fileName,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        private void BuildSummarySheet(XSSFWorkbook workbook, CashShift shift, List<Order> orders, ExcelStyles styles)
        {
            var sheet = (XSSFSheet)workbook.CreateSheet("Resumo do Turno");
            sheet.DisplayGridlines = true;

            // 1. Larguras de colunas fixas e proporcionais
            sheet.SetColumnWidth(0, 3 * 256);   // A: Margem esquerda
            sheet.SetColumnWidth(1, 24 * 256);  // B: Título / Forma Pagamento
            sheet.SetColumnWidth(2, 14 * 256);  // C: Transações
            sheet.SetColumnWidth(3, 18 * 256);  // D: Valor R$
            sheet.SetColumnWidth(4, 12 * 256);  // E: % Part.
            sheet.SetColumnWidth(5, 4 * 256);   // F: Espaçamento central
            sheet.SetColumnWidth(6, 14 * 256);  // G: Canvas Gráficos
            sheet.SetColumnWidth(7, 14 * 256);  // H: Canvas Gráficos
            sheet.SetColumnWidth(8, 14 * 256);  // I: Canvas Gráficos
            sheet.SetColumnWidth(9, 14 * 256);  // J: Canvas Gráficos
            sheet.SetColumnWidth(10, 14 * 256); // K: Canvas Gráficos

            // 2. Banner Superior Institucional
            var titleRow = sheet.CreateRow(1);
            titleRow.HeightInPoints = 32;
            var titleCell = titleRow.CreateCell(1);
            titleCell.SetCellValue("RADIÂNCIA KS  •  RELATÓRIO DE FECHAMENTO DE CAIXA");
            titleCell.CellStyle = styles.MainHeaderStyle;
            sheet.AddMergedRegion(new CellRangeAddress(1, 1, 1, 10));

            // 3. Faixa de Metadados da Sessão
            var metaRow1 = sheet.CreateRow(3);
            metaRow1.HeightInPoints = 18;
            metaRow1.CreateCell(1).SetCellValue("Sessão ID:");
            metaRow1.GetCell(1).CellStyle = styles.BoldLabelStyle;
            metaRow1.CreateCell(2).SetCellValue(shift.Id.ToString());

            metaRow1.CreateCell(6).SetCellValue("Abertura:");
            metaRow1.GetCell(6).CellStyle = styles.BoldLabelStyle;
            metaRow1.CreateCell(7).SetCellValue(shift.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"));

            var metaRow2 = sheet.CreateRow(4);
            metaRow2.HeightInPoints = 18;
            metaRow2.CreateCell(1).SetCellValue("Status:");
            metaRow2.GetCell(1).CellStyle = styles.BoldLabelStyle;
            metaRow2.CreateCell(2).SetCellValue(shift.Status == CashShiftStatus.Closed ? "Fechado" : "Aberto");

            metaRow2.CreateCell(6).SetCellValue("Fechamento:");
            metaRow2.GetCell(6).CellStyle = styles.BoldLabelStyle;
            metaRow2.CreateCell(7).SetCellValue(shift.ClosedAt?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Em aberto");

            // 4. Cálculos Financeiros
            var allPayments = orders.SelectMany(o => o.Payments).ToList();
            var totalGross = orders.Sum(o => o.TotalAmount);
            var cashSales = allPayments.Where(p => IsCashPayment(p.Method)).Sum(p => p.Amount);
            var expectedDrawer = shift.InitialBalance + cashSales;
            var reportedDrawer = shift.FinalReportedBalance ?? 0m;
            var diffDrawer = shift.Status == CashShiftStatus.Closed ? (reportedDrawer - expectedDrawer) : 0m;
            var ticketAvg = orders.Count > 0 ? (totalGross / orders.Count) : 0m;

            // 5. Cartões de KPIs Estruturados (B5:K7, 2 colunas cada)
            CreateMergedKpiCard(sheet, 6, 1, 2, "FATURAMENTO BRUTO", (double)totalGross, styles.CardHeaderStyle, styles.CardCurrencyStyle);
            CreateMergedKpiCard(sheet, 6, 3, 4, "TOTAL PEDIDOS", orders.Count, styles.CardHeaderStyle, styles.CardIntStyle);
            CreateMergedKpiCard(sheet, 6, 5, 6, "TICKET MÉDIO", (double)ticketAvg, styles.CardHeaderStyle, styles.CardCurrencyStyle);
            CreateMergedKpiCard(sheet, 6, 7, 8, "GAVETA ESPERADA", (double)expectedDrawer, styles.CardHeaderStyle, styles.CardCurrencyStyle);

            var diffStyle = diffDrawer >= 0 ? styles.CardCurrencyGreenStyle : styles.CardCurrencyRedStyle;
            CreateMergedKpiCard(sheet, 6, 9, 10, "DIVERGÊNCIA", (double)diffDrawer, styles.CardHeaderStyle, diffStyle);

            // 6. Tabela: Formas de Pagamento (Cols B a E, Linha 10)
            var secPayRow = sheet.CreateRow(9);
            secPayRow.CreateCell(1).SetCellValue("DETALHAMENTO POR FORMA DE PAGAMENTO");
            secPayRow.GetCell(1).CellStyle = styles.SectionHeaderStyle;

            var payHeaderRow = sheet.CreateRow(10);
            payHeaderRow.HeightInPoints = 22;
            string[] payCols = ["Forma de Pagamento", "Transações", "Total (R$)", "% Faturado"];
            for (int i = 0; i < payCols.Length; i++)
            {
                var c = payHeaderRow.CreateCell(1 + i);
                c.SetCellValue(payCols[i]);
                c.CellStyle = styles.TableHeaderStyle;
            }

            var paymentsGrouped = allPayments
                .GroupBy(p => TranslatePaymentMethod(p.Method))
                .Select(g => new
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(p => p.Amount),
                    Percentage = totalGross > 0 ? (double)(g.Sum(p => p.Amount) / totalGross) : 0
                })
                .OrderByDescending(g => g.Total)
                .ToList();

            int payStartRow = 11;
            int payCurrentRow = payStartRow;

            foreach (var item in paymentsGrouped)
            {
                var row = sheet.CreateRow(payCurrentRow++);
                row.HeightInPoints = 20;

                row.CreateCell(1).SetCellValue(item.Method);
                row.GetCell(1).CellStyle = styles.DataCellStyle;

                row.CreateCell(2).SetCellValue(item.Count);
                row.GetCell(2).CellStyle = styles.CenterCellStyle;

                row.CreateCell(3).SetCellValue((double)item.Total);
                row.GetCell(3).CellStyle = styles.CurrencyCellStyle;

                row.CreateCell(4).SetCellValue(item.Percentage);
                row.GetCell(4).CellStyle = styles.PercentCellStyle;
            }

            int payEndRow = Math.Max(payCurrentRow - 1, payStartRow);

            // 7. Tabela: Vendas por Horário (Cols B a E, Linha 22)
            int hourSecRowIdx = Math.Max(payEndRow + 3, 21);

            var secHourRow = sheet.CreateRow(hourSecRowIdx);
            secHourRow.CreateCell(1).SetCellValue("FLUXO DE VENDAS POR FAIXA DE HORÁRIO");
            secHourRow.GetCell(1).CellStyle = styles.SectionHeaderStyle;

            var hourHeaderRow = sheet.CreateRow(hourSecRowIdx + 1);
            hourHeaderRow.HeightInPoints = 22;
            string[] hourCols = ["Horário", "Pedidos", "Total (R$)", "% Faturado"];
            for (int i = 0; i < hourCols.Length; i++)
            {
                var c = hourHeaderRow.CreateCell(1 + i);
                c.SetCellValue(hourCols[i]);
                c.CellStyle = styles.TableHeaderStyle;
            }

            var hourlyGrouped = orders
                .GroupBy(o => o.CreatedAt.ToString("HH:00"))
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Hour = g.Key,
                    Count = g.Count(),
                    Total = (double)g.Sum(o => o.TotalAmount),
                    Percentage = totalGross > 0 ? (double)(g.Sum(o => o.TotalAmount) / totalGross) : 0
                })
                .ToList();

            int hourStartRow = hourSecRowIdx + 2;
            int hourCurrentRow = hourStartRow;

            foreach (var h in hourlyGrouped)
            {
                var row = sheet.CreateRow(hourCurrentRow++);
                row.HeightInPoints = 20;

                row.CreateCell(1).SetCellValue(h.Hour);
                row.GetCell(1).CellStyle = styles.CenterCellStyle;

                row.CreateCell(2).SetCellValue(h.Count);
                row.GetCell(2).CellStyle = styles.CenterCellStyle;

                row.CreateCell(3).SetCellValue(h.Total);
                row.GetCell(3).CellStyle = styles.CurrencyCellStyle;

                row.CreateCell(4).SetCellValue(h.Percentage);
                row.GetCell(4).CellStyle = styles.PercentCellStyle;
            }

            int hourEndRow = Math.Max(hourCurrentRow - 1, hourStartRow);

            // 8. Gráficos NPOI em Canvas Único Compartilhado (Cols G a K)
            var drawing = (XSSFDrawing)sheet.CreateDrawingPatriarch();

            if (paymentsGrouped.Count > 0)
            {
                var pieAnchor = drawing.CreateAnchor(0, 0, 0, 0, 6, 9, 11, 20);
                var pieChart = (XSSFChart)drawing.CreateChart(pieAnchor);

                var pieLegend = pieChart.GetOrCreateLegend();
                pieLegend.Position = LegendPosition.Right;

                var pieData = pieChart.ChartDataFactory.CreatePieChartData<string, double>();
                var categories = DataSources.FromStringCellRange(sheet, new CellRangeAddress(payStartRow, payEndRow, 1, 1));
                var values = DataSources.FromNumericCellRange(sheet, new CellRangeAddress(payStartRow, payEndRow, 3, 3));

                pieData.AddSeries(categories, values);
                pieChart.Plot(pieData);
            }

            if (hourlyGrouped.Count > 0)
            {
                CreateLineChart(drawing, sheet, hourStartRow, hourEndRow, 1, 3, 6, hourSecRowIdx, 11, hourSecRowIdx + 13);
            }
        }

        private void CreateLineChart(XSSFDrawing drawing, XSSFSheet sheet, int startRow, int endRow, int catCol, int valCol, int c1, int r1, int c2, int r2)
        {
            var anchor = drawing.CreateAnchor(0, 0, 0, 0, c1, r1, c2, r2);
            var chart = (XSSFChart)drawing.CreateChart(anchor);

            var legend = chart.GetOrCreateLegend();
            legend.Position = LegendPosition.TopRight;

            var lineData = chart.ChartDataFactory.CreateLineChartData<string, double>();
            var categories = DataSources.FromStringCellRange(sheet, new CellRangeAddress(startRow, endRow, catCol, catCol));
            var values = DataSources.FromNumericCellRange(sheet, new CellRangeAddress(startRow, endRow, valCol, valCol));

            var series = lineData.AddSeries(categories, values);
            series.SetTitle("Faturamento por Hora (R$)");

            var bottomAxis = chart.ChartAxisFactory.CreateCategoryAxis(AxisPosition.Bottom);
            var leftAxis = chart.ChartAxisFactory.CreateValueAxis(AxisPosition.Left);
            leftAxis.Crosses = AxisCrosses.AutoZero;

            chart.Plot(lineData, bottomAxis, leftAxis);
        }

        private void BuildOrdersSheet(XSSFWorkbook workbook, List<Order> orders, ExcelStyles styles)
        {
            var sheet = (XSSFSheet)workbook.CreateSheet("Pedidos");
            sheet.DisplayGridlines = true;

            var headerRow = sheet.CreateRow(0);
            headerRow.HeightInPoints = 22;
            string[] headers = ["Data / Hora", "ID Pedido", "Mesa", "Taxa Serviço", "Total Bruto", "Pagamentos Realizados"];

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = styles.TableHeaderStyle;
            }

            int rowIdx = 1;
            foreach (var order in orders)
            {
                var row = sheet.CreateRow(rowIdx++);
                row.HeightInPoints = 20;

                row.CreateCell(0).SetCellValue(order.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"));
                row.GetCell(0).CellStyle = styles.CenterCellStyle;

                row.CreateCell(1).SetCellValue(order.Id.ToString()[..6].ToUpper());
                row.GetCell(1).CellStyle = styles.CenterCellStyle;

                row.CreateCell(2).SetCellValue(string.IsNullOrWhiteSpace(order.TableNumber) ? "Balcão" : order.TableNumber);
                row.GetCell(2).CellStyle = styles.CenterCellStyle;

                row.CreateCell(3).SetCellValue((double)order.ServiceFeeAmount);
                row.GetCell(3).CellStyle = styles.CurrencyCellStyle;

                row.CreateCell(4).SetCellValue((double)order.TotalAmount);
                row.GetCell(4).CellStyle = styles.CurrencyCellStyle;

                var paymentDetails = order.Payments.Count > 0
                    ? string.Join(", ", order.Payments.Select(p => $"{TranslatePaymentMethod(p.Method)}: {p.Amount:C2}"))
                    : "Não registrado";

                row.CreateCell(5).SetCellValue(paymentDetails);
                row.GetCell(5).CellStyle = order.Payments.Count > 0 ? styles.DataCellStyle : styles.CenterCellStyle;
            }

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }
        }

        private void BuildItemsSheet(XSSFWorkbook workbook, List<Order> orders, ExcelStyles styles)
        {
            var sheet = (XSSFSheet)workbook.CreateSheet("Itens Vendidos");
            sheet.DisplayGridlines = true;

            var headerRow = sheet.CreateRow(0);
            headerRow.HeightInPoints = 22;
            string[] headers = ["Produto", "Quantidade", "Preço Médio", "Subtotal (R$)", "Participação"];

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = styles.TableHeaderStyle;
            }

            var allItems = orders.SelectMany(o => o.Items).ToList();
            var totalItemsGross = allItems.Sum(i => i.Quantity * i.UnitPrice);

            var groupedItems = allItems
                .GroupBy(i => i.Product?.Name ?? "Item Desconhecido")
                .Select(g => new
                {
                    Name = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Total = g.Sum(x => x.Quantity * x.UnitPrice),
                    AvgUnitPrice = g.Sum(x => x.Quantity) > 0 ? g.Sum(x => x.Quantity * x.UnitPrice) / g.Sum(x => x.Quantity) : 0m,
                    Percentage = totalItemsGross > 0 ? (double)(g.Sum(x => x.Quantity * x.UnitPrice) / totalItemsGross) : 0
                })
                .OrderByDescending(g => g.Total)
                .ToList();

            int rowIdx = 1;
            foreach (var item in groupedItems)
            {
                var row = sheet.CreateRow(rowIdx++);
                row.HeightInPoints = 20;

                row.CreateCell(0).SetCellValue(item.Name);
                row.GetCell(0).CellStyle = styles.DataCellStyle;

                row.CreateCell(1).SetCellValue(item.Quantity);
                row.GetCell(1).CellStyle = styles.CenterCellStyle;

                row.CreateCell(2).SetCellValue((double)item.AvgUnitPrice);
                row.GetCell(2).CellStyle = styles.CurrencyCellStyle;

                row.CreateCell(3).SetCellValue((double)item.Total);
                row.GetCell(3).CellStyle = styles.CurrencyCellStyle;

                row.CreateCell(4).SetCellValue(item.Percentage);
                row.GetCell(4).CellStyle = styles.PercentCellStyle;
            }

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }
        }

        private void CreateMergedKpiCard(XSSFSheet sheet, int startRow, int colStart, int colEnd, string title, double value, ICellStyle headerStyle, ICellStyle valStyle)
        {
            var r1 = sheet.GetRow(startRow) ?? sheet.CreateRow(startRow);
            r1.HeightInPoints = 16;
            var r2 = sheet.GetRow(startRow + 1) ?? sheet.CreateRow(startRow + 1);
            r2.HeightInPoints = 24;

            for (int c = colStart; c <= colEnd; c++)
            {
                r1.CreateCell(c).CellStyle = headerStyle;
                r2.CreateCell(c).CellStyle = valStyle;
            }

            r1.GetCell(colStart).SetCellValue(title);
            r2.GetCell(colStart).SetCellValue(value);

            sheet.AddMergedRegion(new CellRangeAddress(startRow, startRow, colStart, colEnd));
            sheet.AddMergedRegion(new CellRangeAddress(startRow + 1, startRow + 1, colStart, colEnd));

            var fullCardRegion = new CellRangeAddress(startRow, startRow + 1, colStart, colEnd);
            RegionUtil.SetBorderTop(BorderStyle.Thin, fullCardRegion, sheet);
            RegionUtil.SetBorderBottom(BorderStyle.Thin, fullCardRegion, sheet);
            RegionUtil.SetBorderLeft(BorderStyle.Thin, fullCardRegion, sheet);
            RegionUtil.SetBorderRight(BorderStyle.Thin, fullCardRegion, sheet);
        }

        private static bool IsCashPayment(PaymentMethod method)
        {
            return method == PaymentMethod.Cash || (int)method == 0;
        }

        private static string TranslatePaymentMethod(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Cash => "Dinheiro",
                PaymentMethod.CreditCard => "Cartão de Crédito",
                PaymentMethod.DebitCard => "Cartão de Débito",
                PaymentMethod.Pix => "PIX",
                _ => (int)method switch
                {
                    0 => "Dinheiro",
                    1 => "Dinheiro",
                    2 => "Cartão de Crédito",
                    3 => "Cartão de Débito",
                    4 => "PIX",
                    _ => $"Outro ({(int)method})"
                }
            };
        }

        private ExcelStyles CreateStyles(XSSFWorkbook wb)
        {
            var fontBase = wb.CreateFont();
            fontBase.FontName = "Segoe UI";
            fontBase.FontHeightInPoints = 10;

            var fontBold = wb.CreateFont();
            fontBold.FontName = "Segoe UI";
            fontBold.FontHeightInPoints = 10;
            fontBold.IsBold = true;

            var fontMainHeader = wb.CreateFont();
            fontMainHeader.FontName = "Segoe UI";
            fontMainHeader.FontHeightInPoints = 13;
            fontMainHeader.IsBold = true;
            fontMainHeader.Color = IndexedColors.White.Index;

            var fontTableHeader = wb.CreateFont();
            fontTableHeader.FontName = "Segoe UI";
            fontTableHeader.FontHeightInPoints = 10;
            fontTableHeader.IsBold = true;
            fontTableHeader.Color = IndexedColors.White.Index;

            var fontSecTitle = wb.CreateFont();
            fontSecTitle.FontName = "Segoe UI";
            fontSecTitle.FontHeightInPoints = 10;
            fontSecTitle.IsBold = true;
            fontSecTitle.Color = IndexedColors.Orange.Index;

            var fontCardHeader = wb.CreateFont();
            fontCardHeader.FontName = "Segoe UI";
            fontCardHeader.FontHeightInPoints = 9;
            fontCardHeader.IsBold = true;
            fontCardHeader.Color = IndexedColors.Grey50Percent.Index;

            var fontCardVal = wb.CreateFont();
            fontCardVal.FontName = "Segoe UI";
            fontCardVal.FontHeightInPoints = 13;
            fontCardVal.IsBold = true;

            var fontCardValGreen = wb.CreateFont();
            fontCardValGreen.FontName = "Segoe UI";
            fontCardValGreen.FontHeightInPoints = 13;
            fontCardValGreen.IsBold = true;
            fontCardValGreen.Color = IndexedColors.Green.Index;

            var fontCardValRed = wb.CreateFont();
            fontCardValRed.FontName = "Segoe UI";
            fontCardValRed.FontHeightInPoints = 13;
            fontCardValRed.IsBold = true;
            fontCardValRed.Color = IndexedColors.Red.Index;

            var dataFormat = wb.CreateDataFormat();
            short currencyFmt = dataFormat.GetFormat("R$ #,##0.00");
            short percentFmt = dataFormat.GetFormat("0.0%");

            var mainHeaderStyle = wb.CreateCellStyle();
            mainHeaderStyle.SetFont(fontMainHeader);
            mainHeaderStyle.FillForegroundColor = IndexedColors.Orange.Index;
            mainHeaderStyle.FillPattern = FillPattern.SolidForeground;
            mainHeaderStyle.Alignment = HorizontalAlignment.Center;
            mainHeaderStyle.VerticalAlignment = VerticalAlignment.Center;

            var tableHeaderStyle = wb.CreateCellStyle();
            tableHeaderStyle.SetFont(fontTableHeader);
            tableHeaderStyle.FillForegroundColor = IndexedColors.Orange.Index;
            tableHeaderStyle.FillPattern = FillPattern.SolidForeground;
            tableHeaderStyle.Alignment = HorizontalAlignment.Center;
            tableHeaderStyle.VerticalAlignment = VerticalAlignment.Center;
            SetBorders(tableHeaderStyle);

            var sectionHeaderStyle = wb.CreateCellStyle();
            sectionHeaderStyle.SetFont(fontSecTitle);
            sectionHeaderStyle.Alignment = HorizontalAlignment.Left;
            sectionHeaderStyle.VerticalAlignment = VerticalAlignment.Center;

            var dataStyle = wb.CreateCellStyle();
            dataStyle.SetFont(fontBase);
            dataStyle.VerticalAlignment = VerticalAlignment.Center;
            SetBorders(dataStyle);

            var centerStyle = wb.CreateCellStyle();
            centerStyle.CloneStyleFrom(dataStyle);
            centerStyle.Alignment = HorizontalAlignment.Center;

            var currencyStyle = wb.CreateCellStyle();
            currencyStyle.CloneStyleFrom(dataStyle);
            currencyStyle.DataFormat = currencyFmt;
            currencyStyle.Alignment = HorizontalAlignment.Right;

            var percentStyle = wb.CreateCellStyle();
            percentStyle.CloneStyleFrom(dataStyle);
            percentStyle.DataFormat = percentFmt;
            percentStyle.Alignment = HorizontalAlignment.Right;

            var boldLabelStyle = wb.CreateCellStyle();
            boldLabelStyle.SetFont(fontBold);
            boldLabelStyle.VerticalAlignment = VerticalAlignment.Center;

            var cardHeaderStyle = wb.CreateCellStyle();
            cardHeaderStyle.SetFont(fontCardHeader);
            cardHeaderStyle.Alignment = HorizontalAlignment.Center;
            cardHeaderStyle.VerticalAlignment = VerticalAlignment.Center;
            cardHeaderStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            cardHeaderStyle.FillPattern = FillPattern.SolidForeground;

            var cardValStyle = wb.CreateCellStyle();
            cardValStyle.SetFont(fontCardVal);
            cardValStyle.DataFormat = currencyFmt;
            cardValStyle.Alignment = HorizontalAlignment.Center;
            cardValStyle.VerticalAlignment = VerticalAlignment.Center;
            cardValStyle.FillForegroundColor = IndexedColors.White.Index;
            cardValStyle.FillPattern = FillPattern.SolidForeground;

            var cardIntStyle = wb.CreateCellStyle();
            cardIntStyle.CloneStyleFrom(cardValStyle);
            cardIntStyle.DataFormat = 0;

            var cardGreenStyle = wb.CreateCellStyle();
            cardGreenStyle.CloneStyleFrom(cardValStyle);
            cardGreenStyle.SetFont(fontCardValGreen);

            var cardRedStyle = wb.CreateCellStyle();
            cardRedStyle.CloneStyleFrom(cardValStyle);
            cardRedStyle.SetFont(fontCardValRed);

            return new ExcelStyles(
                mainHeaderStyle,
                tableHeaderStyle,
                sectionHeaderStyle,
                dataStyle,
                centerStyle,
                currencyStyle,
                percentStyle,
                boldLabelStyle,
                cardHeaderStyle,
                cardValStyle,
                cardIntStyle,
                cardGreenStyle,
                cardRedStyle
            );
        }

        private static void SetBorders(ICellStyle style)
        {
            style.BorderBottom = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.BottomBorderColor = IndexedColors.Grey40Percent.Index;
            style.TopBorderColor = IndexedColors.Grey40Percent.Index;
            style.LeftBorderColor = IndexedColors.Grey40Percent.Index;
            style.RightBorderColor = IndexedColors.Grey40Percent.Index;
        }

        private record ExcelStyles(
            ICellStyle MainHeaderStyle,
            ICellStyle TableHeaderStyle,
            ICellStyle SectionHeaderStyle,
            ICellStyle DataCellStyle,
            ICellStyle CenterCellStyle,
            ICellStyle CurrencyCellStyle,
            ICellStyle PercentCellStyle,
            ICellStyle BoldLabelStyle,
            ICellStyle CardHeaderStyle,
            ICellStyle CardCurrencyStyle,
            ICellStyle CardIntStyle,
            ICellStyle CardCurrencyGreenStyle,
            ICellStyle CardCurrencyRedStyle
        );
    }
}