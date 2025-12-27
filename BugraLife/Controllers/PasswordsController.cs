using BugraLife.DBContext;
using BugraLife.Models;
using Google.Authenticator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BugraLife.Controllers
{
    [Authorize]
    public class PasswordsController : Controller
    {
        private readonly BugraLifeDBContext _context;
        // TempData anahtarı için sabit bir isim kullanalım
        private const string ACCESS_KEY = "CanAccessPasswords";

        public PasswordsController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            // Kullanıcı ID al
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Logout", "Login");

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.LoginUser.FindAsync(userId);
            if (user == null) return RedirectToAction("Logout", "Login");

            // --- GÜVENLİK KONTROLÜ (TEMPDATA) ---
            if (user.IsTwoFactorEnabled == true)
            {
                // Eğer TempData boşsa, doğrulama yok demektir -> Verify sayfasına git
                if (TempData[ACCESS_KEY] == null)
                {
                    return RedirectToAction("Verify2FA");
                }

                // EĞER BURADAYSAK GİRİŞ İZNİ VARDIR.
                // Keep kullanıyoruz ki kullanıcı sayfada F5 yaparsa veya AJAX isteği atarsa yetki hemen silinmesin.
                // Ancak kullanıcı "Ana Sayfa"ya gidip dönerse Keep çalışmadığı için yetki silinmiş olacak.
                TempData.Keep(ACCESS_KEY);
            }
            // -------------------------------------

            var passwords = await _context.WebSitePasswords
                                          .Include(w => w.WebSite)
                                          .OrderByDescending(x => x.created_at)
                                          .ToListAsync();

            ViewBag.WebSites = await _context.WebSites.OrderBy(x => x.website_name).ToListAsync();

            return View(passwords);
        }

        // --- 2FA EKRANI ---
        [HttpGet]
        public IActionResult Verify2FA()
        {
            // Zaten yetki varsa direkt Index'e at
            if (TempData[ACCESS_KEY] != null)
            {
                TempData.Keep(ACCESS_KEY); // Yönlendirirken kaybolmasın
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Verify2FA(string code)
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Logout", "Login");

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.LoginUser.FindAsync(userId);

            TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
            bool isValid = tfa.ValidateTwoFactorPIN(user.TwoFactorSecretKey, code);

            if (isValid)
            {
                // DOĞRULAMA BAŞARILI
                // TempData'ya yetkiyi veriyoruz.
                TempData[ACCESS_KEY] = true;

                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Error = "Kod hatalı!";
                return View();
            }
        }

        // 2. EKLEME (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WebSitePassword password)
        {
            // AJAX işleminde de yetki kontrolü yapmalıyız
            // Yetki varsa Keep diyerek ömrünü uzatıyoruz
            if (TempData[ACCESS_KEY] == null)
                return Json(new { success = false, message = "Oturum izniniz doldu, sayfayı yenileyip tekrar giriş yapın." });

            TempData.Keep(ACCESS_KEY); // İşlem sonrası yetki devam etsin

            if (ModelState.IsValid)
            {
                password.created_at = DateTime.Now;
                _context.WebSitePasswords.Add(password);
                await _context.SaveChangesAsync();
                var newItem = await GetPasswordDetails(password.websitepassword_id);
                return Json(new { success = true, message = "Şifre başarıyla eklendi!", data = newItem });
            }
            return Json(new { success = false, message = "Eksik alanlar." });
        }

        // 3. GÜNCELLEME (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WebSitePassword password)
        {
            if (TempData[ACCESS_KEY] == null)
                return Json(new { success = false, message = "Oturum izniniz doldu." });

            TempData.Keep(ACCESS_KEY);

            var existing = await _context.WebSitePasswords.FindAsync(password.websitepassword_id);
            if (existing == null) return Json(new { success = false, message = "Kayıt yok." });

            if (ModelState.IsValid)
            {
                existing.website_id = password.website_id;
                existing.websitepassword_username = password.websitepassword_username;
                existing.websitepassword_password = password.websitepassword_password;
                existing.websitepassword_description = password.websitepassword_description;
                existing.updated_at = DateTime.Now;

                _context.Update(existing);
                await _context.SaveChangesAsync();
                var updatedItem = await GetPasswordDetails(password.websitepassword_id);
                return Json(new { success = true, message = "Şifre güncellendi!", data = updatedItem });
            }
            return Json(new { success = false, message = "Hata oluştu." });
        }

        // 4. SİLME (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (TempData[ACCESS_KEY] == null)
                return Json(new { success = false, message = "Oturum izniniz doldu." });

            TempData.Keep(ACCESS_KEY);

            var password = await _context.WebSitePasswords.FindAsync(id);
            if (password != null)
            {
                _context.WebSitePasswords.Remove(password);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Silindi." });
            }
            return Json(new { success = false, message = "Bulunamadı." });
        }

        // Yardımcı Metot
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