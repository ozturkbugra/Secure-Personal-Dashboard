using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;
using System.Globalization; // Bunu unutma

namespace BugraLife.Controllers
{
    [Authorize]
    public class ExpenseController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public ExpenseController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME (Filtreli) - BURASI AYNI
        public async Task<IActionResult> Index(bool showAll = false)
        {
            var query = _context.Expenses
                .Include(x => x.ExpenseType)
                .Include(x => x.PaymentType)
                .Include(x => x.Person)
                .Where(x => x.is_bankmovement == false);

            if (!showAll)
            {
                var now = DateTime.Now;
                query = query.Where(x => x.expense_date.Month == now.Month && x.expense_date.Year == now.Year);
            }

            var list = await query.OrderByDescending(x => x.expense_date).ToListAsync();

            ViewBag.ShowAll = showAll;
            ViewBag.ExpenseTypes = await _context.ExpenseTypes.Where(x => x.is_bank == false).OrderBy(x => x.expensetype_order).ToListAsync();
            ViewBag.PaymentTypes = await _context.PaymentTypes.Where(x => x.is_bank == false).OrderBy(x => x.paymenttype_order).ToListAsync();
            ViewBag.Persons = await _context.Persons.Where(x => x.is_bank == false).OrderBy(x => x.person_order).ToListAsync();

            return View(list);
        }

        // 2. EKLEME
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense, string expense_amount,
            bool is_installment, List<string> InstallmentAmounts, List<DateTime> InstallmentDates, List<string> InstallmentDescriptions)
        {
            // 500 Hatasını önlemek için: 
            // .NET, "Person", "ExpenseType" gibi objelerin formdan dolu gelmesini bekler, 
            // gelmeyince ModelState.IsValid false olur ve patlar. Bunların doğrulamasını devreden çıkarıyoruz.
            ModelState.Remove("Person");
            ModelState.Remove("ExpenseType");
            ModelState.Remove("PaymentType");
            ModelState.Remove("expense_amount");

            try
            {
                if (is_installment)
                {
                    if (InstallmentAmounts == null || InstallmentAmounts.Count == 0)
                        return Json(new { success = false, message = "Taksit bilgileri bulunamadı." });

                    var newExpensesList = new List<object>();

                    // Döngüyle her bir taksiti ayrı gider olarak kaydediyoruz
                    for (int i = 0; i < InstallmentAmounts.Count; i++)
                    {
                        decimal instAmount = 0;
                        try
                        {
                            instAmount = decimal.Parse(InstallmentAmounts[i], new CultureInfo("tr-TR"));
                        }
                        catch { return Json(new { success = false, message = $"{i + 1}. taksit tutar formatı hatalı!" }); }

                        var newExpense = new Expense
                        {
                            expense_date = InstallmentDates[i],
                            expense_amount = instAmount,
                            expense_description = InstallmentDescriptions[i], // "1/9 Taksit - Açıklama"
                            person_id = expense.person_id,
                            expensetype_id = expense.expensetype_id,
                            paymenttype_id = expense.paymenttype_id,
                            is_bankmovement = false
                        };

                        // Her taksit için bakiyeyi düşüyoruz
                        var account = await _context.PaymentTypes.FindAsync(newExpense.paymenttype_id);
                        if (account != null) account.paymenttype_balance -= newExpense.expense_amount;

                        _context.Expenses.Add(newExpense);
                        await _context.SaveChangesAsync();

                        // Önyüze tabloya basılması için listeye ekliyoruz
                        newExpensesList.Add(await GetExpenseDetails(newExpense.expense_id));
                    }

                    return Json(new { success = true, message = "Taksitli giderler başarıyla eklendi.", dataList = newExpensesList, isMultiple = true });
                }
                else
                {
                    // --- NORMAL TEKLİ EKLEME ---

                    // Dropdown'lardan birini boş bırakırsan DB'ye kayıt atarken patlamasın diye manuel kontrol
                    if (expense.person_id == 0 || expense.expensetype_id == 0 || expense.paymenttype_id == 0)
                    {
                        return Json(new { success = false, message = "Lütfen Kişi, Gider Türü ve Ödeme Tipi seçimlerini eksiksiz yapın." });
                    }

                    try
                    {
                        if (string.IsNullOrEmpty(expense_amount)) expense_amount = "0";
                        expense.expense_amount = decimal.Parse(expense_amount, new CultureInfo("tr-TR"));
                    }
                    catch
                    {
                        return Json(new { success = false, message = "Tutar formatı hatalı!" });
                    }

                    if (expense.expense_amount >= 0)
                    {
                        var account = await _context.PaymentTypes.FindAsync(expense.paymenttype_id);
                        if (account != null) account.paymenttype_balance -= expense.expense_amount;

                        expense.is_bankmovement = false;
                        _context.Expenses.Add(expense);
                        await _context.SaveChangesAsync();

                        var newExpense = await GetExpenseDetails(expense.expense_id);
                        return Json(new { success = true, message = "Gider eklendi.", data = newExpense, isMultiple = false });
                    }

                    return Json(new { success = false, message = "Tutar 0'dan küçük olamaz." });
                }
            }
            catch (Exception ex)
            {
                // Eğer sunucu tarafında (SQL veya Kod) bir hata olursa 500 dönüp kilitlenmek yerine
                // hatanın tam detayını (InnerException) ekrana Toast mesajı olarak basacak.
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Sistem Hatası: " + errorMsg });
            }
        }
        // 3. GÜNCELLEME
        [HttpPost]
        [ValidateAntiForgeryToken]
        // DİKKAT: 'string expense_amount' parametresi eklendi
        public async Task<IActionResult> Edit(Expense expense, string expense_amount)
        {
            // --- PARA BİRİMİ DÖNÜŞTÜRME ---
            try
            {
                if (string.IsNullOrEmpty(expense_amount)) expense_amount = "0";
                expense.expense_amount = decimal.Parse(expense_amount, new CultureInfo("tr-TR"));
            }
            catch
            {
                return Json(new { success = false, message = "Tutar formatı hatalı!" });
            }
            // -------------------------------

            if (expense.expense_id > 0)
            {
                var oldExpense = await _context.Expenses.AsNoTracking().FirstOrDefaultAsync(x => x.expense_id == expense.expense_id);

                if (oldExpense != null)
                {
                    // Eski tutarı iade et
                    var oldAccount = await _context.PaymentTypes.FindAsync(oldExpense.paymenttype_id);
                    if (oldAccount != null) oldAccount.paymenttype_balance += oldExpense.expense_amount;

                    // Yeni tutarı düş
                    var newAccount = await _context.PaymentTypes.FindAsync(expense.paymenttype_id);
                    if (newAccount != null) newAccount.paymenttype_balance -= expense.expense_amount;
                }

                // Güncelle
                expense.is_bankmovement = false;
                _context.Expenses.Update(expense);
                await _context.SaveChangesAsync();

                var updatedExpense = await GetExpenseDetails(expense.expense_id);
                return Json(new { success = true, message = "Gider güncellendi.", data = updatedExpense });
            }
            return Json(new { success = false, message = "Güncelleme başarısız." });
        }

        // 4. SİLME - BURASI AYNI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null)
            {
                var account = await _context.PaymentTypes.FindAsync(expense.paymenttype_id);
                if (account != null) account.paymenttype_balance += expense.expense_amount;

                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Gider silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }

        // YARDIMCI METOD - BURASI AYNI
        private async Task<object> GetExpenseDetails(int id)
        {
            var item = await _context.Expenses
                .Include(x => x.Person)
                .Include(x => x.ExpenseType)
                .Include(x => x.PaymentType)
                .FirstOrDefaultAsync(x => x.expense_id == id);

            var trCulture = new CultureInfo("tr-TR");

            return new
            {
                id = item.expense_id,
                dateStr = item.expense_date.ToString("dd.MM.yyyy"),
                dateRaw = item.expense_date.ToString("yyyy-MM-dd"),
                person = item.Person != null ? item.Person.person_name : "-",
                personId = item.person_id,
                type = item.ExpenseType != null ? item.ExpenseType.expensetype_name : "-",
                typeId = item.expensetype_id,
                payment = item.PaymentType != null ? item.PaymentType.paymenttype_name : "-",
                paymentId = item.paymenttype_id,
                desc = item.expense_description,
                amountRaw = item.expense_amount.ToString("N2", trCulture)
            };
        }
    }
}