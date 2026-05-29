using Google.Authenticator; // Kütüphaneyi ekle
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BugraLife.DBContext;
using BugraLife.Models;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace BugraLife.Controllers
{
    [Authorize] // Sadece giriş yapmışlar girebilir
    public class SettingsController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public SettingsController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // AYARLAR SAYFASI
        public IActionResult Index()
        {
            int userId = Convert.ToInt32(User.FindFirst("UserId")?.Value);
            var user = _context.LoginUser.Find(userId);

            var model = new SettingsViewModel
            {
                Username = user.loginuser_username,
                NameSurname = user.loginuser_namesurname,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled
            };

            // Eğer 2FA kapalıysa, kurulum için QR kodu oluştur
            if (!user.IsTwoFactorEnabled)
            {
                // Her seferinde veya ilk seferde bir SecretKey oluşturuyoruz
                // Eğer veritabanında null ise yeni oluştur
                string secretKey = user.TwoFactorSecretKey ?? Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10);

                // Key'i veritabanına geçici olarak (veya kalıcı) kaydetmemiz lazım ki doğrulayabilelim
                if (user.TwoFactorSecretKey == null)
                {
                    user.TwoFactorSecretKey = secretKey;
                    _context.SaveChanges();
                }

                // Google Auth Kurulumu
                TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
                var setupInfo = tfa.GenerateSetupCode("BugraLife App", user.loginuser_username, secretKey, false, 3);

                model.QrCodeImageUrl = setupInfo.QrCodeSetupImageUrl;
                model.EntryKey = setupInfo.ManualEntryKey;
            }

            return View(model);
        }

        // PROFİL GÜNCELLEME
        [HttpPost]
        public IActionResult UpdateProfile(SettingsViewModel model)
        {
            int userId = Convert.ToInt32(User.FindFirst("UserId")?.Value);
            var user = _context.LoginUser.Find(userId);

            // 1. Kullanıcı Adı Güncelleme
            user.loginuser_username = model.Username;
            user.loginuser_namesurname = model.NameSurname;

            // 2. Şifre Değiştirme (Eğer alanlar doluysa)
            if (!string.IsNullOrEmpty(model.CurrentPassword) && !string.IsNullOrEmpty(model.NewPassword))
            {
                // --- YENİ KONTROL: Şifreler Eşleşiyor mu? ---
                if (model.NewPassword != model.ConfirmPassword)
                {
                    TempData["Error"] = "Yeni girilen şifreler birbiriyle uyuşmuyor!";
                    return RedirectToAction("Index");
                }
                // ---------------------------------------------

                string hashedCurrent = Sifrele(model.CurrentPassword); // Senin Base64 metodun

                // Mevcut şifre doğru mu?
                if (user.login_password != hashedCurrent)
                {
                    TempData["Error"] = "Mevcut şifreniz hatalı!";
                    return RedirectToAction("Index");
                }

                // Her şey tamamsa şifreyi güncelle
                user.login_password = Sifrele(model.NewPassword);
            }

            _context.SaveChanges();
            TempData["Success"] = "Bilgiler güncellendi.";
            return RedirectToAction("Index");
        }

        // 2FA AKTİF ETME (Doğrulama Sonrası)
        [HttpPost]
        public IActionResult Enable2FA(string code)
        {
            int userId = Convert.ToInt32(User.FindFirst("UserId")?.Value);
            var user = _context.LoginUser.Find(userId);

            TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
            // Kullanıcının girdiği kodu, veritabanındaki gizli key ile doğrula
            bool isValid = tfa.ValidateTwoFactorPIN(user.TwoFactorSecretKey, code);

            if (isValid)
            {
                user.IsTwoFactorEnabled = true;
                _context.SaveChanges();
                TempData["Success"] = "2FA Başarıyla Aktif Edildi!";
            }
            else
            {
                TempData["Error"] = "Girdiğiniz kod hatalı.";
            }

            return RedirectToAction("Index");
        }

        // 2FA KAPATMA
        [HttpPost]
        public IActionResult Disable2FA()
        {
            int userId = Convert.ToInt32(User.FindFirst("UserId")?.Value);
            var user = _context.LoginUser.Find(userId);

            user.IsTwoFactorEnabled = false;
            user.TwoFactorSecretKey = null; // Keyi sıfırla
            _context.SaveChanges();

            TempData["Success"] = "2FA Kapatıldı.";
            return RedirectToAction("Index");
        }

        // Senin mevcut şifreleme metodun (Örnek)
        private string Sifrele(string text)
        {
            using (var sha256 = SHA256.Create())
            {
                // 1. Şifreyi byte'a çevir ve hashle
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));

                // 2. Hashlenmiş byte'ları Base64 string formatına çevir (Senin yapın bu)
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}