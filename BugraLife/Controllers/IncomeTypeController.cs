using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class IncomeTypeController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public IncomeTypeController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            var list = await _context.IncomeTypes.OrderBy(x => x.incometype_order).ToListAsync();

            // Otomatik Sıra No Hesaplama
            int nextOrder = 1;
            if (list.Any())
            {
                nextOrder = list.Max(x => x.incometype_order) + 1;
            }
            ViewBag.NextOrder = nextOrder;

            return View(list);
        }

        // 2. EKLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IncomeType incomeType)
        {
            if (ModelState.IsValid)
            {
                bool exists = await _context.IncomeTypes.AnyAsync(x => x.incometype_name == incomeType.incometype_name);
                if (exists)
                {
                    return Json(new { success = false, message = "Bu gelir türü zaten kayıtlı!" });
                }

                _context.IncomeTypes.Add(incomeType);
                await _context.SaveChangesAsync();

                // Eklenen veriyi geri dön (Tabloya basmak için)
                return Json(new { success = true, message = "Gelir türü başarıyla eklendi!", data = incomeType });
            }
            return Json(new { success = false, message = "Form verileri geçersiz." });
        }

        // 3. GÜNCELLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(IncomeType incomeType)
        {
            if (ModelState.IsValid)
            {
                bool exists = await _context.IncomeTypes.AnyAsync(x => x.incometype_name == incomeType.incometype_name && x.incometype_id != incomeType.incometype_id);
                if (exists)
                {
                    return Json(new { success = false, message = "Bu isimde başka bir kayıt zaten mevcut!" });
                }

                _context.IncomeTypes.Update(incomeType);
                await _context.SaveChangesAsync();

                // Güncellenen veriyi geri dön
                return Json(new { success = true, message = "Güncelleme başarılı!", data = incomeType });
            }
            return Json(new { success = false, message = "Güncelleme başarısız." });
        }

        // 4. SİLME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.IncomeTypes.FindAsync(id);
            if (item != null)
            {
                _context.IncomeTypes.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Kayıt silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }
    }
}