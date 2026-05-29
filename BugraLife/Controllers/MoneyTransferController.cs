using BugraLife.DBContext;
using BugraLife.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class MoneyTransferController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public MoneyTransferController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // SAYFA: Transfer Formu
        public async Task<IActionResult> Index()
        {
            // HEM KAYNAK HEM HEDEF OLABİLECEK HESAPLAR (Kredi Kartı OLMAYANLAR)
            var accounts = await _context.PaymentTypes
                .Where(x => x.is_creditcard == false && x.is_bank == false)
                .OrderBy(x => x.paymenttype_order)
                .ToListAsync();

            ViewBag.Accounts = accounts;

            return View();
        }

        // İŞLEM: Transferi Gerçekleştir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeTransfer(int sourceAccountId, int targetAccountId, string amount, string commission, DateTime date, string description)
        {
            decimal decimalAmount = 0;
            decimal decimalCommission = 0;

            // --- DÜZELTME BAŞLANGICI ---
            // Replace yapmıyoruz, direkt tr-TR formatında parse ediyoruz.
            // Bu sayede "1.000,50" doğru şekilde 1000.50 olarak algılanır.
            try
            {
                if (string.IsNullOrEmpty(amount)) amount = "0";
                if (string.IsNullOrEmpty(commission)) commission = "0";

                var trCulture = new CultureInfo("tr-TR");
                decimalAmount = decimal.Parse(amount, trCulture);
                decimalCommission = decimal.Parse(commission, trCulture);
            }
            catch
            {
                return Json(new { success = false, message = "Tutar formatı hatalı! Lütfen kontrol ediniz." });
            }
            // --- DÜZELTME BİTİŞİ ---

            if (decimalAmount <= 0)
            {
                return Json(new { success = false, message = "Transfer tutarı 0'dan büyük olmalıdır." });
            }

            if (sourceAccountId == targetAccountId)
            {
                return Json(new { success = false, message = "Kaynak ve hedef hesap aynı olamaz." });
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var sourceAccount = await _context.PaymentTypes.FindAsync(sourceAccountId);
                    var targetAccount = await _context.PaymentTypes.FindAsync(targetAccountId);

                    // Sabit ID'leri güvenli çekme
                    var personid = await _context.Persons.Where(x => x.is_bank == true).Select(x => x.person_id).FirstOrDefaultAsync();
                    var incometypeid = await _context.IncomeTypes.Where(x => x.is_bank == true).Select(x => x.incometype_id).FirstOrDefaultAsync();

                    // Transfer ve Komisyon Gider Türlerini Bulma
                    var transferTypeId = await _context.ExpenseTypes
                        .Where(x => x.is_bank == true && x.is_commission == false)
                        .Select(x => x.expensetype_id)
                        .FirstOrDefaultAsync();

                    var commissionTypeId = await _context.ExpenseTypes
                        .Where(x => x.is_commission == true)
                        .Select(x => x.expensetype_id)
                        .FirstOrDefaultAsync();

                    // Eğer veritabanında özel tür yoksa varsayılanı ata
                    if (commissionTypeId == 0) commissionTypeId = transferTypeId;

                    if (sourceAccount == null || targetAccount == null)
                    {
                        return Json(new { success = false, message = "Hesap bilgileri bulunamadı." });
                    }

                    // AÇIKLAMA
                    string autoDescription = $"{sourceAccount.paymenttype_name} » {targetAccount.paymenttype_name} Transferi";
                    string finalDescription = string.IsNullOrEmpty(description) ? autoDescription : description;
                    string commissionDesc = $"{sourceAccount.paymenttype_name} » {targetAccount.paymenttype_name} İşlem Masrafı";

                    // ADIM 1: ANA TRANSFER (KAYNAKTAN ÇIKIŞ)
                    var expense = new Expense
                    {
                        paymenttype_id = sourceAccountId,
                        expense_amount = decimalAmount,
                        expense_date = date,
                        expense_description = finalDescription,
                        is_bankmovement = true,
                        expensetype_id = transferTypeId,
                        person_id = personid
                    };

                    sourceAccount.paymenttype_balance -= decimalAmount;
                    _context.Expenses.Add(expense);

                    // ADIM 2: HEDEF HESABA GİRİŞ
                    var income = new Income
                    {
                        paymenttype_id = targetAccountId,
                        income_amount = decimalAmount,
                        income_date = date,
                        income_description = finalDescription,
                        is_bankmovement = true,
                        incometype_id = incometypeid,
                        person_id = personid
                    };

                    targetAccount.paymenttype_balance += decimalAmount;
                    _context.Incomes.Add(income);

                    // ADIM 3: KOMİSYON VARSA DÜŞ
                    if (decimalCommission > 0)
                    {
                        var commExpense = new Expense
                        {
                            paymenttype_id = sourceAccountId,
                            expense_amount = decimalCommission,
                            expense_date = date,
                            expense_description = commissionDesc,
                            is_bankmovement = false,
                            expensetype_id = commissionTypeId,
                            person_id = personid
                        };

                        sourceAccount.paymenttype_balance -= decimalCommission;
                        _context.Expenses.Add(commExpense);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Transfer işlemi başarıyla tamamlandı." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Hata: " + ex.Message });
                }
            }
        }
    }
}