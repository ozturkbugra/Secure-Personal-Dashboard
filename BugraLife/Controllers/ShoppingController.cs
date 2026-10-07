using BugraLife.DBContext;
using BugraLife.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class ShoppingController : Controller
    {
        private readonly BugraLifeDBContext _context;
        private static readonly CultureInfo Tr = ShoppingCalc.Tr;

        public ShoppingController(BugraLifeDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? listId)
        {
            var lists = await _context.ShoppingLists
                .Include(l => l.Items)
                .Where(l => l.shoppinglist_active)
                .OrderByDescending(l => l.shoppinglist_created) // son eklenen başta
                .ToListAsync();

            foreach (var l in lists)
                l.Items = l.Items
                    .OrderBy(i => i.shoppingitem_isbought)
                    .ThenBy(i => i.shoppingitem_order)
                    .ThenBy(i => i.shoppingitem_created)
                    .ToList();

            ViewBag.LastPrices = await GetLastPricesAsync();
            ViewBag.SelectedListId = listId ?? lists.FirstOrDefault()?.shoppinglist_id;
            return View(lists);
        }

        // ---------------- LİSTELER ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateList(string name, string? date)
        {
            if (string.IsNullOrWhiteSpace(name)) return Json(new { success = false, message = "Liste adı zorunlu." });
            int order = (await _context.ShoppingLists.MaxAsync(x => (int?)x.shoppinglist_order) ?? 0) + 1;
            var list = new ShoppingList { shoppinglist_name = name.Trim(), shoppinglist_order = order, shoppinglist_date = ParseDate(date) };
            _context.ShoppingLists.Add(list);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Liste eklendi.", data = new { list.shoppinglist_id, list.shoppinglist_name } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditList(int id, string name, string? date)
        {
            var list = await _context.ShoppingLists.FindAsync(id);
            if (list == null) return Json(new { success = false, message = "Liste bulunamadı." });
            if (string.IsNullOrWhiteSpace(name)) return Json(new { success = false, message = "Liste adı zorunlu." });
            list.shoppinglist_name = name.Trim();
            list.shoppinglist_date = ParseDate(date);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Liste güncellendi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteList(int id)
        {
            var list = await _context.ShoppingLists.Include(l => l.Items).FirstOrDefaultAsync(l => l.shoppinglist_id == id);
            if (list == null) return Json(new { success = false, message = "Liste bulunamadı." });
            _context.ShoppingItems.RemoveRange(list.Items);
            _context.ShoppingLists.Remove(list);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Liste silindi." });
        }

        // ---------------- ÜRÜNLER ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int listId, string name, string? amount, string? unit, string? price, string? description)
        {
            if (string.IsNullOrWhiteSpace(name)) return Json(new { success = false, message = "Ürün adı zorunlu." });
            if (!await _context.ShoppingLists.AnyAsync(l => l.shoppinglist_id == listId))
                return Json(new { success = false, message = "Liste bulunamadı." });

            int order = (await _context.ShoppingItems.Where(i => i.shoppinglist_id == listId)
                .MaxAsync(i => (int?)i.shoppingitem_order) ?? 0) + 1;

            var item = new ShoppingItem
            {
                shoppinglist_id = listId,
                shoppingitem_name = name.Trim(),
                shoppingitem_amount = ParseNum(amount),
                shoppingitem_unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim(),
                shoppingitem_description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                shoppingitem_price = ParseNum(price),
                shoppingitem_order = order
            };
            _context.ShoppingItems.Add(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true, data = ItemDto(item), lastPrice = await GetLastUnitPriceAsync(item) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(int id, string name, string? amount, string? unit, string? price, string? description)
        {
            var item = await _context.ShoppingItems.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "Ürün bulunamadı." });
            if (string.IsNullOrWhiteSpace(name)) return Json(new { success = false, message = "Ürün adı zorunlu." });

            item.shoppingitem_name = name.Trim();
            item.shoppingitem_amount = ParseNum(amount);
            item.shoppingitem_unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
            item.shoppingitem_description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            item.shoppingitem_price = ParseNum(price);
            await _context.SaveChangesAsync();

            return Json(new { success = true, data = ItemDto(item), lastPrice = await GetLastUnitPriceAsync(item) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBought(int id, bool isBought)
        {
            var item = await _context.ShoppingItems.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "Ürün bulunamadı." });

            item.shoppingitem_isbought = isBought;

            // Alındı + fiyat var + daha önce yazılmadıysa → fiyat geçmişine (birim fiyatla) ekle
            if (isBought && item.shoppingitem_price.HasValue && !item.shoppingitem_historylogged)
            {
                _context.ShoppingPriceHistories.Add(new ShoppingPriceHistory
                {
                    product_name = item.shoppingitem_name,
                    price = item.shoppingitem_price.Value,
                    unit = ShoppingCalc.NormUnit(item.shoppingitem_unit),
                    unit_price = ShoppingCalc.UnitPrice(item.shoppingitem_price, item.shoppingitem_amount),
                    date = DateTime.Now
                });
                item.shoppingitem_historylogged = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.ShoppingItems.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "Ürün bulunamadı." });
            _context.ShoppingItems.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearBought(int listId)
        {
            var bought = await _context.ShoppingItems
                .Where(i => i.shoppinglist_id == listId && i.shoppingitem_isbought)
                .ToListAsync();
            _context.ShoppingItems.RemoveRange(bought);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"{bought.Count} ürün temizlendi.", count = bought.Count });
        }

        // ---------------- EXPORT ----------------
        public async Task<IActionResult> ExportExcel(int listId)
        {
            var (list, items) = await GetListForExport(listId);
            if (list == null) return NotFound();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Liste");

            ws.Cell(1, 1).Value = list.shoppinglist_name + (list.shoppinglist_date.HasValue ? "  (" + list.shoppinglist_date.Value.ToString("dd.MM.yyyy", Tr) + ")" : "");
            ws.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.FontSize = 14;

            var headers = new[] { "Ürün", "Miktar", "Fiyat", "Birim Fiyat", "Alındı", "Açıklama" };
            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cell(3, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.SetBold();
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#667eea");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int row = 4;
            decimal total = 0;
            foreach (var i in items)
            {
                var up = ShoppingCalc.UnitPrice(i.shoppingitem_price, i.shoppingitem_amount);
                ws.Cell(row, 1).Value = i.shoppingitem_name;
                ws.Cell(row, 2).Value = AmountText(i);
                if (i.shoppingitem_price.HasValue) ws.Cell(row, 3).Value = i.shoppingitem_price.Value;
                ws.Cell(row, 4).Value = up.HasValue ? ShoppingCalc.UnitPriceText(up.Value, i.shoppingitem_unit) : "";
                ws.Cell(row, 5).Value = i.shoppingitem_isbought ? "✓" : "";
                ws.Cell(row, 6).Value = i.shoppingitem_description ?? "";
                if (i.shoppingitem_price.HasValue) { ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 \"₺\""; total += i.shoppingitem_price.Value; }
                row++;
            }

            ws.Cell(row + 1, 2).Value = "TOPLAM";
            ws.Cell(row + 1, 2).Style.Font.SetBold();
            ws.Cell(row + 1, 3).Value = total;
            ws.Cell(row + 1, 3).Style.Font.SetBold();
            ws.Cell(row + 1, 3).Style.NumberFormat.Format = "#,##0.00 \"₺\"";

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{SafeName(list.shoppinglist_name)}.xlsx");
        }

        public async Task<IActionResult> ExportPdf(int listId)
        {
            var (list, items) = await GetListForExport(listId);
            if (list == null) return NotFound();

            decimal total = items.Where(i => i.shoppingitem_price.HasValue).Sum(i => i.shoppingitem_price!.Value);
            string listName = list.shoppinglist_name;
            string dateStr = (list.shoppinglist_date.HasValue ? "Tarih: " + list.shoppinglist_date.Value.ToString("dd.MM.yyyy", Tr) + "  ·  " : "")
                + "Oluşturma: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm", Tr);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(listName).FontSize(18).Bold().FontColor("#4b3fa0");
                        col.Item().Text(dateStr).FontSize(9).FontColor("#666");
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22);   // alındı
                            c.RelativeColumn(3);    // ürün
                            c.RelativeColumn(2);    // miktar
                            c.RelativeColumn(2);    // fiyat
                            c.RelativeColumn(2);    // birim fiyat
                        });

                        void H(string t) => table.Cell().Background("#667eea").Padding(5).Text(t).FontColor("#fff").Bold();
                        H(""); H("Ürün"); H("Miktar"); H("Fiyat"); H("Birim Fiyat");

                        foreach (var i in items)
                        {
                            var up = ShoppingCalc.UnitPrice(i.shoppingitem_price, i.shoppingitem_amount);
                            string chk = i.shoppingitem_isbought ? "X" : "";
                            table.Cell().BorderBottom(0.5f).BorderColor("#ddd").Padding(5).Text(chk);
                            table.Cell().BorderBottom(0.5f).BorderColor("#ddd").Padding(5).Text(i.shoppingitem_name);
                            table.Cell().BorderBottom(0.5f).BorderColor("#ddd").Padding(5).Text(AmountText(i));
                            table.Cell().BorderBottom(0.5f).BorderColor("#ddd").Padding(5).Text(i.shoppingitem_price.HasValue ? i.shoppingitem_price.Value.ToString("N2", Tr) + " ₺" : "");
                            table.Cell().BorderBottom(0.5f).BorderColor("#ddd").Padding(5).Text(up.HasValue ? ShoppingCalc.UnitPriceText(up.Value, i.shoppingitem_unit) : "");
                        }

                        table.Cell().ColumnSpan(3).Padding(6).AlignRight().Text("TOPLAM").Bold();
                        table.Cell().ColumnSpan(2).Padding(6).Text(total.ToString("N2", Tr) + " ₺").Bold();
                    });

                    page.Footer().AlignCenter().Text(x => { x.Span("BugraLife · "); x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
                });
            });

            var bytes = doc.GeneratePdf();
            return File(bytes, "application/pdf", $"{SafeName(list.shoppinglist_name)}.pdf");
        }

        private async Task<(ShoppingList? list, List<ShoppingItem> items)> GetListForExport(int listId)
        {
            var list = await _context.ShoppingLists.Include(l => l.Items).FirstOrDefaultAsync(l => l.shoppinglist_id == listId);
            if (list == null) return (null, new());
            var items = list.Items
                .OrderBy(i => i.shoppingitem_isbought)
                .ThenBy(i => i.shoppingitem_order)
                .ThenBy(i => i.shoppingitem_created)
                .ToList();
            return (list, items);
        }

        // ---------------- YARDIMCILAR ----------------
        private static DateTime? ParseDate(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return null;
            // HTML date input: yyyy-MM-dd
            if (DateTime.TryParse(v.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;
            if (DateTime.TryParse(v.Trim(), Tr, DateTimeStyles.None, out var d2)) return d2;
            return null;
        }

        private static decimal? ParseNum(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return null;
            if (decimal.TryParse(v.Trim(), NumberStyles.Number, Tr, out var d)) return d;
            if (decimal.TryParse(v.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d2)) return d2;
            return null;
        }

        private static string AmountText(ShoppingItem i)
        {
            if (i.shoppingitem_amount.HasValue)
            {
                var a = i.shoppingitem_amount.Value;
                string num = (a == Math.Floor(a)) ? ((long)a).ToString(Tr) : a.ToString("0.###", Tr);
                return (num + " " + (i.shoppingitem_unit ?? "")).Trim();
            }
            return i.shoppingitem_quantity ?? "";
        }

        private static string SafeName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "liste" : name;
        }

        // (name|unit) -> en son birim fiyat
        private async Task<Dictionary<string, decimal>> GetLastPricesAsync()
        {
            var latest = await _context.ShoppingPriceHistories
                .AsNoTracking()
                .GroupBy(h => new { p = h.product_name, u = h.unit })
                .Select(g => g.OrderByDescending(x => x.date).FirstOrDefault())
                .ToListAsync();

            var dict = new Dictionary<string, decimal>();
            foreach (var h in latest)
            {
                if (h == null) continue;
                string key = ShoppingCalc.Key(h.product_name, h.unit);
                dict[key] = h.unit_price ?? h.price;
            }
            return dict;
        }

        private async Task<decimal?> GetLastUnitPriceAsync(ShoppingItem item)
        {
            string pName = item.shoppingitem_name.Trim().ToLower();
            string pUnit = (item.shoppingitem_unit ?? "").Trim().ToLower();

            var match = await _context.ShoppingPriceHistories
                .AsNoTracking()
                .Where(h => h.product_name.ToLower() == pName && (h.unit ?? "").ToLower() == pUnit)
                .OrderByDescending(h => h.date)
                .FirstOrDefaultAsync();

            return match == null ? null : (match.unit_price ?? match.price);
        }

        private static object ItemDto(ShoppingItem i)
        {
            var up = ShoppingCalc.UnitPrice(i.shoppingitem_price, i.shoppingitem_amount);
            return new
            {
                i.shoppingitem_id,
                i.shoppingitem_name,
                amount = i.shoppingitem_amount,
                amountText = AmountText(i),
                unit = i.shoppingitem_unit,
                i.shoppingitem_description,
                price = i.shoppingitem_price,
                unitPrice = up,
                unitPriceText = up.HasValue ? ShoppingCalc.UnitPriceText(up.Value, i.shoppingitem_unit) : null,
                i.shoppingitem_isbought
            };
        }
    }
}
