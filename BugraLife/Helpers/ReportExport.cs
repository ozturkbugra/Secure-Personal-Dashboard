using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BugraLife.Helpers
{
    /// <summary>
    /// Raporlar için ortak Excel (ClosedXML) ve PDF (QuestPDF) üretici.
    /// Tüm hücreler controller tarafında tr-TR formatında string'e çevrilip gönderilir;
    /// böylece her rapor aynı görünümde export edilir. Alışveriş Sepeti export'uyla
    /// aynı tema rengini (#667eea) kullanır.
    /// </summary>
    public static class ReportExport
    {
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        private const string HeaderBg = "#667eea";

        public static string SafeName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "rapor" : name.Trim();
        }

        public static byte[] Excel(string title, string subtitle, string[] headers,
            List<string[]> rows, List<(string label, string value)>? summary = null)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Rapor");
            int lastCol = Math.Max(1, headers.Length);

            ws.Cell(1, 1).Value = title;
            ws.Range(1, 1, 1, lastCol).Merge().Style.Font.SetBold().Font.FontSize = 14;

            int headerRow = 2;
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                ws.Cell(2, 1).Value = subtitle;
                ws.Range(2, 1, 2, lastCol).Merge().Style.Font.FontColor = XLColor.FromHtml("#666666");
                headerRow = 4;
            }
            else headerRow = 3;

            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cell(headerRow, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.SetBold();
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBg);
                cell.Style.Font.FontColor = XLColor.White;
            }

            void SetCellValue(IXLCell cell, string val)
            {
                if (string.IsNullOrWhiteSpace(val)) { cell.Value = ""; return; }
                bool isMoney = val.EndsWith(" ₺");
                string raw = isMoney ? val.Substring(0, val.Length - 2).Trim() : val;
                
                if (DateTime.TryParseExact(raw, "dd.MM.yyyy", Tr, DateTimeStyles.None, out var d)) {
                    cell.Value = d; cell.Style.DateFormat.Format = "dd.MM.yyyy"; return;
                }
                
                if (raw.StartsWith("+")) raw = raw.Substring(1);
                
                if (decimal.TryParse(raw, NumberStyles.Any, Tr, out var num)) {
                    cell.Value = num;
                    cell.Style.NumberFormat.Format = isMoney ? "#,##0.00 \"₺\"" : "#,##0.00";
                } else {
                    cell.Value = val;
                }
            }

            int row = headerRow + 1;
            foreach (var r in rows)
            {
                for (int c = 0; c < r.Length; c++)
                    SetCellValue(ws.Cell(row, c + 1), r[c]);
                row++;
            }

            if (summary != null && summary.Count > 0)
            {
                row++;
                foreach (var (label, value) in summary)
                {
                    ws.Cell(row, 1).Value = label;
                    ws.Cell(row, 1).Style.Font.SetBold();
                    SetCellValue(ws.Cell(row, Math.Min(2, lastCol)), value);
                    ws.Cell(row, Math.Min(2, lastCol)).Style.Font.SetBold();
                    row++;
                }
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public static byte[] Pdf(string title, string subtitle, string[] headers,
            List<string[]> rows, List<(string label, string value)>? summary = null)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(title).FontSize(18).Bold().FontColor("#4b3fa0");
                        if (!string.IsNullOrWhiteSpace(subtitle))
                            col.Item().Text(subtitle).FontSize(9).FontColor("#666666");
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            foreach (var _ in headers) c.RelativeColumn();
                        });

                        foreach (var h in headers)
                            table.Cell().Background(HeaderBg).Padding(4).Text(h).FontColor("#ffffff").Bold();

                        foreach (var r in rows)
                            foreach (var cell in r)
                                table.Cell().BorderBottom(0.5f).BorderColor("#dddddd").Padding(4).Text(cell ?? "");
                    });

                    if (summary != null && summary.Count > 0)
                    {
                        page.Footer().Column(col =>
                        {
                            foreach (var (label, value) in summary)
                                col.Item().AlignRight().Text($"{label}: {value}").Bold();
                        });
                    }
                    else
                    {
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("BugraLife · ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    }
                });
            });

            return doc.GeneratePdf();
        }

        // tr-TR para / tarih kısayolları
        public static string Money(decimal v) => v.ToString("N2", Tr) + " ₺";
        public static string Qty(decimal v) => v.ToString("0.###", Tr);
        public static string D(DateTime d) => d.ToString("dd.MM.yyyy", Tr);
    }
}
