using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;
using System.Globalization; // Bunu eklemeyi unutma (Kültür ayarı için)

namespace BugraLife.Controllers
{
    [Authorize]
    public class CreditCardPaymentController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public CreditCardPaymentController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // SAYFA: Ödeme Formu
        public async Task<IActionResult> Index()
        {
            // 1. ÖDENECEK KARTLAR (Sadece Kredi Kartları)
            ViewBag.CreditCards = await _context.PaymentTypes
                .Where(x => x.is_creditcard == true)
                .OrderBy(x => x.paymenttype_order)
                .ToListAsync();

            // 2. KAYNAK HESAPLAR (Kredi Kartı Olmayanlar: Nakit, Banka Hesabı vb.)
            ViewBag.SourceAccounts = await _context.PaymentTypes
                .Where(x => x.is_creditcard == false && x.is_bank == false)
                .OrderBy(x => x.paymenttype_order)
                .ToListAsync();

            return View();
        }

        // İŞLEM: Ödemeyi Gerçekleştir
        [HttpPost]
        [ValidateAntiForgeryToken]
        // DİKKAT: amount parametresini 'string' olarak alıyoruz.
        public async Task<IActionResult> MakePayment(int targetCardId, int sourceAccountId, string amount, DateTime date, string description)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // --- PARA BİRİMİ DÖNÜŞTÜRME (String -> Decimal) ---
                    decimal parsedAmount = 0;
                    try
                    {
                        if (string.IsNullOrEmpty(amount)) amount = "0";

                        // Türkçe kültüründe (tr-TR): Nokta binlik ayraçtır, Virgül kuruş ayraçtır.
                        // "20.134,06" -> 20134.06 olarak çevrilir.
                        parsedAmount = decimal.Parse(amount, new CultureInfo("tr-TR"));
                    }
                    catch
                    {
                        return Json(new { success = false, message = "Lütfen geçerli bir tutar giriniz." });
                    }

                    if (parsedAmount <= 0)
                    {
                        return Json(new { success = false, message = "Tutar 0'dan büyük olmalıdır." });
                    }
                    // ----------------------------------------------------

                    // Hesapları Bul
                    var targetCard = await _context.PaymentTypes.FindAsync(targetCardId); // Borcu ödenecek kart
                    var sourceAccount = await _context.PaymentTypes.FindAsync(sourceAccountId); // Para çıkacak hesap

                    // Sabit ID'leri çekiyoruz (Null kontrolü yapmanı öneririm)
                    var personid = await _context.Persons.Where(x => x.is_bank == true).Select(x => x.person_id).FirstOrDefaultAsync();
                    var expensetypeid = await _context.ExpenseTypes.Where(x => x.is_bank == true).Select(x => x.expensetype_id).FirstOrDefaultAsync();
                    var incometypeid = await _context.IncomeTypes.Where(x => x.is_bank == true).Select(x => x.incometype_id).FirstOrDefaultAsync();

                    if (targetCard == null || sourceAccount == null)
                    {
                        return Json(new { success = false, message = "Hesap bilgileri bulunamadı." });
                    }

                    // ADIM 1: KAYNAK HESAPTAN PARA ÇIKIŞI (GİDER)
                    var expense = new Expense
                    {
                        paymenttype_id = sourceAccountId,
                        expense_amount = parsedAmount, // Çevirdiğimiz tutarı kullanıyoruz
                        expense_date = date,
                        expense_description = string.IsNullOrEmpty(description) ? $"{targetCard.paymenttype_name} Borç Ödemesi" : description,
                        is_bankmovement = true,
                        expensetype_id = expensetypeid,
                        person_id = personid
                    };

                    sourceAccount.paymenttype_balance -= parsedAmount;
                    _context.Expenses.Add(expense);

                    // ADIM 2: KREDİ KARTINA PARA GİRİŞİ (GELİR / BORÇ DÜŞME)
                    var income = new Income
                    {
                        paymenttype_id = targetCardId,
                        income_amount = parsedAmount, // Çevirdiğimiz tutarı kullanıyoruz
                        income_date = date,
                        income_description = string.IsNullOrEmpty(description) ? $"{sourceAccount.paymenttype_name} Hesabından Ödeme" : description,
                        is_bankmovement = true,
                        incometype_id = incometypeid,
                        person_id = personid
                    };

                    targetCard.paymenttype_balance += parsedAmount;
                    _context.Incomes.Add(income);

                    // KAYDET
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Kredi kartı borcu başarıyla ödendi." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
                }
            }
        }
    }
}