using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class ExpenseTypeController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public ExpenseTypeController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            var list = await _context.ExpenseTypes.ToListAsync();

            // Otomatik Sıra No Hesaplama
            int nextOrder = 1;
            if (list.Any())
            {
                nextOrder = list.Max(x => int.TryParse(x.expensetype_order, out int v) ? v : 0) + 1;
            }
            ViewBag.NextOrder = nextOrder;

            // Listeyi sayısal sıraya göre dizip gönderelim
            var sortedList = list.OrderBy(x => int.TryParse(x.expensetype_order, out int v) ? v : 0).ToList();

            return View(sortedList);
        }

        // 2. EKLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExpenseType expenseType)
        {
            if (ModelState.IsValid)
            {
                bool exists = await _context.ExpenseTypes.AnyAsync(x => x.expensetype_name == expenseType.expensetype_name);
                if (exists)
                {
                    return Json(new { success = false, message = "Bu gider türü zaten mevcut!" });
                }

                _context.ExpenseTypes.Add(expenseType);
                await _context.SaveChangesAsync();

                // Eklenen veriyi geri dönüyoruz (Tabloya basmak için)
                return Json(new { success = true, message = "Gider türü eklendi!", data = expenseType });
            }
            return Json(new { success = false, message = "Form verileri geçersiz." });
        }

        // 3. GÜNCELLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExpenseType expenseType)
        {
            if (ModelState.IsValid)
            {
                bool exists = await _context.ExpenseTypes.AnyAsync(x => x.expensetype_name == expenseType.expensetype_name && x.expensetype_id != expenseType.expensetype_id);
                if (exists)
                {
                    return Json(new { success = false, message = "Bu isimde başka bir kayıt zaten var!" });
                }

                _context.ExpenseTypes.Update(expenseType);
                await _context.SaveChangesAsync();

                // Güncellenen veriyi geri dönüyoruz
                return Json(new { success = true, message = "Güncelleme başarılı!", data = expenseType });
            }
            return Json(new { success = false, message = "Güncelleme başarısız." });
        }

        // 4. SİLME
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.ExpenseTypes.FindAsync(id);
            if (item != null)
            {
                _context.ExpenseTypes.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Kayıt silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }
    }
}