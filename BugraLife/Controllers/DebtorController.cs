using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class DebtorController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public DebtorController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            var debtors = await _context.Debtors.OrderBy(x => x.debtor_name).ToListAsync();
            return View(debtors);
        }

        // 2. EKLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Debtor debtor)
        {
            if (ModelState.IsValid)
            {
                // Aynı isimde kayıt kontrolü
                bool exists = await _context.Debtors.AnyAsync(x => x.debtor_name == debtor.debtor_name);
                if (exists)
                {
                    return Json(new { success = false, message = "Bu isimde bir borçlu zaten kayıtlı!" });
                }

                _context.Debtors.Add(debtor);
                await _context.SaveChangesAsync();

                // Eklenen veriyi geri dönüyoruz (Tabloya basmak için)
                return Json(new { success = true, message = "Borçlu/Alacaklı başarıyla eklendi!", data = debtor });
            }
            return Json(new { success = false, message = "Form verileri geçersiz." });
        }

        // 3. GÜNCELLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Debtor debtor)
        {
            if (ModelState.IsValid)
            {
                // Kendisi hariç aynı isim kontrolü
                bool exists = await _context.Debtors.AnyAsync(x => x.debtor_name == debtor.debtor_name && x.debtor_id != debtor.debtor_id);
                if (exists)
                {
                    return Json(new { success = false, message = "Bu isimde başka bir kayıt zaten mevcut!" });
                }

                _context.Debtors.Update(debtor);
                await _context.SaveChangesAsync();

                // Güncellenen veriyi geri dönüyoruz
                return Json(new { success = true, message = "Borçlu/Alacaklı bilgisi güncellendi!", data = debtor });
            }
            return Json(new { success = false, message = "Güncelleme başarısız." });
        }

        // 4. SİLME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var debtor = await _context.Debtors.FindAsync(id);
            if (debtor != null)
            {
                _context.Debtors.Remove(debtor);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Kayıt silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }
    }
}