using BugraLife.DBContext;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BugraLife.Models;
using BugraLife.Services;
using Google.Authenticator;

public class LoginController : Controller
{
    private readonly BugraLifeDBContext _context;
    private readonly LoginAttemptTracker _attemptTracker;

    public LoginController(BugraLifeDBContext context, LoginAttemptTracker attemptTracker)
    {
        _context = context;
        _attemptTracker = attemptTracker;
    }

    // İstemci IP'sini kilit anahtarı olarak kullanıyoruz.
    private string GetClientKey()
        => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // Kilit süresini kullanıcıya okunaklı göster (2 dk / 10 dk gibi).
    private static string FormatDuration(TimeSpan t)
    {
        int totalMinutes = (int)Math.Ceiling(t.TotalMinutes);
        if (totalMinutes >= 1) return $"{totalMinutes} dakika";
        return $"{Math.Ceiling(t.TotalSeconds)} saniye";
    }

    [HttpGet]
    public IActionResult Index()
    {
        // Eğer kullanıcı zaten giriş yapmışsa direkt ana sayfaya at
        if (User.Identity!.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string username, string password, bool rememberMe)
    {
        var clientKey = GetClientKey();

        // --- RATE-LIMIT KONTROLÜ: IP kilitli mi? ---
        var (locked, remaining) = _attemptTracker.IsLocked(clientKey);
        if (locked)
        {
            ViewBag.Error = $"Çok fazla hatalı deneme yaptınız. Lütfen {FormatDuration(remaining)} sonra tekrar deneyin.";
            return View();
        }

        string hashedPassword = Sifrele(password);

        var user = _context.LoginUser.FirstOrDefault(x =>
            x.loginuser_username == username &&
            x.login_password == hashedPassword);

        if (user != null)
        {
            // Başarılı giriş: hata sayacını sıfırla.
            _attemptTracker.Reset(clientKey);

            // --- 2FA KONTROLÜ BAŞLIYOR ---
            if (user.IsTwoFactorEnabled)
            {
                // Kullanıcıyı geçici olarak hafızada tutuyoruz (Giriş yapmadı ama şifresi doğru)
                TempData["PendingUserId"] = user.loginuser_id;
                TempData["RememberMe"] = rememberMe;

                // Doğrulama sayfasına git
                return RedirectToAction("Verify2FA");
            }
            // ------------------------------

            // 2FA yoksa normal giriş yap
            await LoginUserInternal(user, rememberMe);
            return RedirectToAction("Index", "Home");
        }
        else
        {
            // Başarısız giriş: denemeyi kaydet, gerekiyorsa kilit uygula.
            var (nowLocked, lockDuration) = _attemptTracker.RegisterFailure(clientKey);
            if (nowLocked)
            {
                ViewBag.Error = $"Çok fazla hatalı deneme! Giriş {FormatDuration(lockDuration)} boyunca kilitlendi.";
            }
            else
            {
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı!";
            }
            return View();
        }
    }

    // 2FA DOĞRULAMA EKRANI (GET)
    public IActionResult Verify2FA()
    {
        if (TempData["PendingUserId"] == null) return RedirectToAction("Index");

        // TempData redirect sonrası silinir, tekrar set edelim (Keep)
        TempData.Keep("PendingUserId");
        TempData.Keep("RememberMe");

        return View();
    }

    // 2FA DOĞRULAMA (POST)
    [HttpPost]
    public async Task<IActionResult> Verify2FA(string code)
    {
        if (TempData["PendingUserId"] == null) return RedirectToAction("Index");

        var clientKey = GetClientKey();

        // --- RATE-LIMIT: 2FA kod deneme kilidi de aynı sayaçla ---
        var (locked, remaining) = _attemptTracker.IsLocked(clientKey);
        if (locked)
        {
            ViewBag.Error = $"Çok fazla hatalı deneme yaptınız. Lütfen {FormatDuration(remaining)} sonra tekrar deneyin.";
            TempData.Keep("PendingUserId");
            TempData.Keep("RememberMe");
            return View();
        }

        int userId = (int)TempData["PendingUserId"];
        bool rememberMe = (bool)TempData["RememberMe"];

        var user = _context.LoginUser.Find(userId);

        TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
        bool isValid = tfa.ValidateTwoFactorPIN(user.TwoFactorSecretKey, code);

        if (isValid)
        {
            // Kod doğru: sayacı sıfırla ve gerçekten giriş yap.
            _attemptTracker.Reset(clientKey);
            await LoginUserInternal(user, rememberMe);
            return RedirectToAction("Index", "Home");
        }
        else
        {
            var (nowLocked, lockDuration) = _attemptTracker.RegisterFailure(clientKey);
            ViewBag.Error = nowLocked
                ? $"Çok fazla hatalı kod! Giriş {FormatDuration(lockDuration)} boyunca kilitlendi."
                : "Kod hatalı!";
            TempData.Keep("PendingUserId"); // Tekrar denemesi için tut
            TempData.Keep("RememberMe");
            return View();
        }
    }

    // Ortak Giriş Metodu (Kod tekrarını önlemek için)
    private async Task LoginUserInternal(LoginUser user, bool rememberMe)
    {
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.loginuser_username),
        new Claim(ClaimTypes.GivenName, user.loginuser_namesurname),
        new Claim("UserId", user.loginuser_id.ToString())
    };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties { IsPersistent = rememberMe };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
    }

    // Çıkış Yapma Metodu
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Login");
    }

    private string Sifrele(string sifre)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sifre));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}