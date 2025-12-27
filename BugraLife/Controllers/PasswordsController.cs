using BugraLife.DBContext;
using BugraLife.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BugraLife.Controllers
{
    [Authorize]
    public class PasswordsController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public PasswordsController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            var passwords = await _context.WebSitePasswords
                                          .Include(w => w.WebSite)
                                          .OrderByDescending(x => x.created_at)
                                          .ToListAsync();

            ViewBag.WebSites = await _context.WebSites.OrderBy(x => x.website_name).ToListAsync();

            return View(passwords);
        }

        // 2. EKLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WebSitePassword password)
        {
            if (ModelState.IsValid)
            {
                password.created_at = DateTime.Now;
                _context.WebSitePasswords.Add(password);
                await _context.SaveChangesAsync();

                // Eklenen veriyi detaylarıyla çek (Site Adı lazım)
                var newItem = await GetPasswordDetails(password.websitepassword_id);

                return Json(new { success = true, message = "Şifre başarıyla eklendi!", data = newItem });
            }
            return Json(new { success = false, message = "Lütfen tüm alanları doldurun." });
        }

        // 3. GÜNCELLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WebSitePassword password)
        {
            var existing = await _context.WebSitePasswords.FindAsync(password.websitepassword_id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Kayıt bulunamadı." });
            }

            if (ModelState.IsValid)
            {
                existing.website_id = password.website_id;
                existing.websitepassword_username = password.websitepassword_username;
                existing.websitepassword_password = password.websitepassword_password;
                existing.websitepassword_description = password.websitepassword_description;
                existing.updated_at = DateTime.Now; // Güncellenme tarihi

                _context.Update(existing);
                await _context.SaveChangesAsync();

                // Güncellenen veriyi detaylarıyla çek
                var updatedItem = await GetPasswordDetails(password.websitepassword_id);

                return Json(new { success = true, message = "Şifre güncellendi!", data = updatedItem });
            }
            return Json(new { success = false, message = "Güncelleme başarısız!" });
        }

        // 4. SİLME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var password = await _context.WebSitePasswords.FindAsync(id);
            if (password != null)
            {
                _context.WebSitePasswords.Remove(password);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Şifre silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }

        // YARDIMCI METOD
        private async Task<object> GetPasswordDetails(int id)
        {
            var item = await _context.WebSitePasswords
                .Include(w => w.WebSite)
                .FirstOrDefaultAsync(x => x.websitepassword_id == id);

            return new
            {
                id = item.websitepassword_id,
                websiteId = item.website_id,
                websiteName = item.WebSite != null ? item.WebSite.website_name : "Belirtilmemiş",
                websiteUrl = item.WebSite != null ? item.WebSite.website_url : "",
                username = item.websitepassword_username,
                password = item.websitepassword_password,
                desc = item.websitepassword_description,
                dateStr = item.created_at.ToString("dd.MM.yyyy")
            };
        }
    }
}