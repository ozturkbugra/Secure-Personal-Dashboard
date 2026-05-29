using BugraLife.DBContext;
using BugraLife.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BugraLife.Controllers
{
    [Authorize]
    public class WebSiteController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public WebSiteController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            var sites = await _context.WebSites.ToListAsync();
            return View(sites);
        }

        // 2. EKLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WebSite website)
        {
            if (ModelState.IsValid)
            {
                _context.WebSites.Add(website);
                await _context.SaveChangesAsync();

                // Eklenen veriyi geri dönüyoruz
                return Json(new { success = true, message = "Web sitesi eklendi!", data = website });
            }
            return Json(new { success = false, message = "Form verileri geçersiz!" });
        }

        // 3. GÜNCELLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WebSite website)
        {
            if (ModelState.IsValid)
            {
                _context.Update(website);
                await _context.SaveChangesAsync();

                // Güncellenen veriyi geri dönüyoruz
                return Json(new { success = true, message = "Web sitesi güncellendi!", data = website });
            }
            return Json(new { success = false, message = "Güncelleme başarısız!" });
        }

        // 4. SİLME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var website = await _context.WebSites.FindAsync(id);
            if (website != null)
            {
                _context.WebSites.Remove(website);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Kayıt silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }
    }
}