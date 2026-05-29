using BugraLife.DBContext;
using BugraLife.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "BugraLife_Auth"; // Sabit bir isim ver
        options.LoginPath = "/Login/Index";
        options.ExpireTimeSpan = TimeSpan.FromDays(365);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true; // Kritik: GDPR/Çerez politikasýna takýlmasýn
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddDbContext<BugraLifeDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Keys")))
    .SetApplicationName("BugraLifeApp");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    static string Sifrele(string sifre)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sifre));

            return Convert.ToBase64String(hashedBytes);
        }
    }

    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BugraLifeDBContext>();

        context.Database.EnsureCreated();

        if (!context.LoginUser.Any())
        {
            string rawPassword = "123456";
            string hashedPassword = Sifrele(rawPassword);

            var adminUser = new LoginUser
            {
                loginuser_username = "bugra",
                loginuser_namesurname = "Buðra Öztürk",
                login_password = hashedPassword,
                IsTwoFactorEnabled = false,
                TwoFactorSecretKey = null
            };

            context.LoginUser.Add(adminUser);
            context.SaveChanges();

        }

        if (!context.IncomeTypes.Where(x => x.is_bank == true).Any())
        {
            var it = new IncomeType
            {
                incometype_name = "BANKA HAREKETÝ",
                is_bank = true,
                incometype_order = 0,
            };
            context.IncomeTypes.Add(it);
            context.SaveChanges();
        }

        if (!context.ExpenseTypes.Where(x=> x.is_bank == true).Any())
        {
            var et = new ExpenseType
            {
                expensetype_name = "BANKA HAREKETÝ",
                is_bank = true,
                expensetype_order = "0",
                description = "",
            };

            context.ExpenseTypes.Add(et);
            context.SaveChanges();
        }

        if (!context.PaymentTypes.Where(x=> x.is_bank == true).Any())
        {
            var pt = new PaymentType
            {
                paymenttype_name = "BANKA HAREKETÝ",
                is_bank = true,
                paymenttype_order = 0,
                is_creditcard = false,
                paymenttype_balance = 0,
            };

            context.PaymentTypes.Add(pt);
            context.SaveChanges();
        }

        if (!context.Persons.Where(x=> x.is_bank == true).Any())
        {
            var p = new Person
            {
                person_name = "BANKA HAREKETÝ",
                is_bank = true,
                person_order = 0,
            };

            context.Persons.Add(p);
            context.SaveChanges();
        }



    }
    catch (Exception ex)
    {
        Console.WriteLine("Veri eklerken hata oluþtu: " + ex.Message);
    }
}

var supportedCultures = new[] { new CultureInfo("tr-TR") };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};
app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");






app.Run();

