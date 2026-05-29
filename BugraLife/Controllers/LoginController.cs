using BugraLife.DBContext;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BugraLife.Models;
using Google.Authenticator;

public class LoginController : Controller
{
    private readonly BugraLifeDBContext _context;

    public LoginController(BugraLifeDBContext context)
    {
        _context = context;
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
        string hashedPassword = Sifrele(password);

        var user = _context.LoginUser.FirstOrDefault(x =>
            x.loginuser_username == username &&
            x.login_password == hashedPassword);

        if (user != null)
        {
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
            ViewBag.Error = "Kullanıcı adı veya şifre hatalı!";
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

        int userId = (int)TempData["PendingUserId"];
        bool rememberMe = (bool)TempData["RememberMe"];

        var user = _context.LoginUser.Find(userId);

        TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
        bool isValid = tfa.ValidateTwoFactorPIN(user.TwoFactorSecretKey, code);

        if (isValid)
        {
            // Kod doğru, şimdi gerçekten giriş yap
            await LoginUserInternal(user, rememberMe);
            return RedirectToAction("Index", "Home");
        }
        else
        {
            ViewBag.Error = "Kod hatalı!";
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