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
            ViewBag.ExpenseTypes = await _context.ExpenseTypes.Where(x => x.is_bank == false).OrderBy(x => x.expensetype_name).ToListAsync();
            ViewBag.PaymentTypes = await _context.PaymentTypes.Where(x => x.is_bank == false).OrderBy(x => x.paymenttype_order).ToListAsync();
            ViewBag.Persons = await _context.Persons.Where(x => x.is_bank == false).OrderBy(x => x.person_order).ToListAsync();

            return View(list);
        }

        // 2. EKLEME
        [HttpPost]
        [ValidateAntiForgeryToken]
        // DİKKAT: 'string expense_amount' parametresi eklendi
        public async Task<IActionResult> Create(Expense expense, string expense_amount)
        {
            // --- PARA BİRİMİ DÖNÜŞTÜRME ---
            try
            {
                if (string.IsNullOrEmpty(expense_amount)) expense_amount = "0";
                // Türk formatına (noktalı binlik) göre decimal'e çevir
                expense.expense_amount = decimal.Parse(expense_amount, new CultureInfo("tr-TR"));
            }
            catch
            {
                return Json(new { success = false, message = "Tutar formatı hatalı!" });
            }
            // -------------------------------

            // ModelState validasyonu amount harici diğer alanlar için (örn: Description, Date vs.)
            // amount'u elle doldurduğumuz için ModelState.Remove yapabiliriz veya direkt devam edebiliriz.
            // Ama en garantisi try-catch bloğu ile manuel set etmekti, onu yaptık.

            if (expense.expense_amount >= 0) // Basit bir kontrol
            {
                // Bakiye Düş
                var account = await _context.PaymentTypes.FindAsync(expense.paymenttype_id);
                if (account != null) account.paymenttype_balance -= expense.expense_amount;

                // Kaydet
                expense.is_bankmovement = false;
                _context.Expenses.Add(expense);
                await _context.SaveChangesAsync();

                var newExpense = await GetExpenseDetails(expense.expense_id);
                return Json(new { success = true, message = "Gider eklendi.", data = newExpense });
            }
            return Json(new { success = false, message = "Eksik veya hatalı bilgi." });
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