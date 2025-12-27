using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.DBContext;
using BugraLife.Models;
using Microsoft.AspNetCore.Authorization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class FixedExpenseController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public FixedExpenseController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            var list = await _context.FixedExpenses
                .Include(x => x.ExpenseType)
                .Where(x => x.is_active)
                .OrderBy(x => x.payment_day)
                .ToListAsync();

            ViewBag.ExpenseTypes = await _context.ExpenseTypes
                .Where(x => x.is_home == true)
                .OrderBy(x => x.expensetype_name)
                .ToListAsync();

            return View(list);
        }

        // 2. EKLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FixedExpense fixedExpense)
        {
            if (ModelState.IsValid)
            {
                fixedExpense.is_active = true;
                _context.Add(fixedExpense);
                await _context.SaveChangesAsync();

                // Eklenen veriyi detaylarıyla çek (Tabloya basmak için)
                var newItem = await GetFixedExpenseDetails(fixedExpense.fixedexpense_id);

                return Json(new { success = true, message = "Sabit gider başarıyla tanımlandı.", data = newItem });
            }
            return Json(new { success = false, message = "Lütfen tüm alanları doldurunuz." });
        }

        // 3. GÜNCELLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FixedExpense fixedExpense)
        {
            var existing = await _context.FixedExpenses.FindAsync(fixedExpense.fixedexpense_id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Kayıt bulunamadı." });
            }

            if (ModelState.IsValid)
            {
                existing.expensetype_id = fixedExpense.expensetype_id;
                existing.payment_day = fixedExpense.payment_day;
                existing.frequency_count = fixedExpense.frequency_count;

                _context.Update(existing);
                await _context.SaveChangesAsync();

                // Güncellenen veriyi detaylarıyla çek
                var updatedItem = await GetFixedExpenseDetails(fixedExpense.fixedexpense_id);

                return Json(new { success = true, message = "Sabit gider güncellendi.", data = updatedItem });
            }
            return Json(new { success = false, message = "Form verileri geçersiz." });
        }

        // 4. SİLME (Sayfa Yenilemeden - Soft Delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.FixedExpenses.FindAsync(id);
            if (item == null)
            {
                return Json(new { success = false, message = "Kayıt bulunamadı." });
            }

            // Silmek yerine pasife çekiyoruz
            item.is_active = false;
            _context.Update(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Sabit gider takibi iptal edildi." });
        }

        // YARDIMCI METOD: JSON dönüşü için detayları hazırlar
        private async Task<object> GetFixedExpenseDetails(int id)
        {
            var item = await _context.FixedExpenses
                .Include(x => x.ExpenseType)
                .FirstOrDefaultAsync(x => x.fixedexpense_id == id);

            return new
            {
                id = item.fixedexpense_id,
                type = item.ExpenseType != null ? item.ExpenseType.expensetype_name : "-",
                typeId = item.expensetype_id,
                day = item.payment_day,
                freq = item.frequency_count
            };
        }
    }
}