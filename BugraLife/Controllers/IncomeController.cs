using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;
using System.Globalization; // Kültür ayarı için şart

namespace BugraLife.Controllers
{
    [Authorize]
    public class IncomeController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public IncomeController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME - BURASI TAMAM
        public async Task<IActionResult> Index(bool showAll = false)
        {
            var query = _context.Incomes
                .Include(x => x.IncomeType)
                .Include(x => x.PaymentType)
                .Include(x => x.Person)
                .Where(x => x.is_bankmovement == false);

            if (!showAll)
            {
                var now = DateTime.Now;
                query = query.Where(x => x.income_date.Month == now.Month && x.income_date.Year == now.Year);
            }

            var list = await query.OrderByDescending(x => x.income_date).ToListAsync();

            ViewBag.ShowAll = showAll;
            ViewBag.IncomeTypes = await _context.IncomeTypes.Where(x => x.is_bank == false).OrderBy(x => x.incometype_order).ToListAsync();
            ViewBag.PaymentTypes = await _context.PaymentTypes.Where(x => x.is_bank == false).OrderBy(x => x.paymenttype_order).ToListAsync();
            ViewBag.Persons = await _context.Persons.Where(x => x.is_bank == false).OrderBy(x => x.person_order).ToListAsync();

            return View(list);
        }

        // 2. EKLEME
        [HttpPost]
        [ValidateAntiForgeryToken]
        // DİKKAT: 'string income_amount' eklendi
        public async Task<IActionResult> Create(Income income, string income_amount)
        {
            // --- PARA BİRİMİ DÖNÜŞTÜRME ---
            try
            {
                if (string.IsNullOrEmpty(income_amount)) income_amount = "0";
                // Türk formatına (noktalı binlik) göre decimal'e çevir
                income.income_amount = decimal.Parse(income_amount, new CultureInfo("tr-TR"));
            }
            catch
            {
                return Json(new { success = false, message = "Tutar formatı hatalı!" });
            }
            // -------------------------------

            if (income.income_amount >= 0)
            {
                // Bakiye Artır (+)
                var account = await _context.PaymentTypes.FindAsync(income.paymenttype_id);
                if (account != null) account.paymenttype_balance += income.income_amount;

                // Kaydet
                income.is_bankmovement = false;
                _context.Incomes.Add(income);
                await _context.SaveChangesAsync();

                var newIncome = await GetIncomeDetails(income.income_id);
                return Json(new { success = true, message = "Gelir eklendi.", data = newIncome });
            }
            return Json(new { success = false, message = "Eksik bilgi." });
        }

        // 3. GÜNCELLEME
        [HttpPost]
        [ValidateAntiForgeryToken]
        // DİKKAT: 'string income_amount' eklendi
        public async Task<IActionResult> Edit(Income income, string income_amount)
        {
            // --- PARA BİRİMİ DÖNÜŞTÜRME ---
            try
            {
                if (string.IsNullOrEmpty(income_amount)) income_amount = "0";
                income.income_amount = decimal.Parse(income_amount, new CultureInfo("tr-TR"));
            }
            catch
            {
                return Json(new { success = false, message = "Tutar formatı hatalı!" });
            }
            // -------------------------------

            if (income.income_id > 0)
            {
                var oldIncome = await _context.Incomes.AsNoTracking().FirstOrDefaultAsync(x => x.income_id == income.income_id);

                if (oldIncome != null)
                {
                    // Eski tutarı düş (-)
                    var oldAccount = await _context.PaymentTypes.FindAsync(oldIncome.paymenttype_id);
                    if (oldAccount != null) oldAccount.paymenttype_balance -= oldIncome.income_amount;

                    // Yeni tutarı ekle (+)
                    var newAccount = await _context.PaymentTypes.FindAsync(income.paymenttype_id);
                    if (newAccount != null) newAccount.paymenttype_balance += income.income_amount;
                }

                // Güncelle
                income.is_bankmovement = false;
                _context.Incomes.Update(income);
                await _context.SaveChangesAsync();

                var updatedIncome = await GetIncomeDetails(income.income_id);
                return Json(new { success = true, message = "Gelir güncellendi.", data = updatedIncome });
            }
            return Json(new { success = false, message = "Güncelleme başarısız." });
        }

        // 4. SİLME - BURASI AYNI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var income = await _context.Incomes.FindAsync(id);
            if (income != null)
            {
                // Bakiyeden düş (-)
                var account = await _context.PaymentTypes.FindAsync(income.paymenttype_id);
                if (account != null) account.paymenttype_balance -= income.income_amount;

                _context.Incomes.Remove(income);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Gelir silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }

        // YARDIMCI METOD - BURASI AYNI
        private async Task<object> GetIncomeDetails(int id)
        {
            var item = await _context.Incomes
                .Include(x => x.Person)
                .Include(x => x.IncomeType)
                .Include(x => x.PaymentType)
                .FirstOrDefaultAsync(x => x.income_id == id);

            var trCulture = new CultureInfo("tr-TR");

            return new
            {
                id = item.income_id,
                dateStr = item.income_date.ToString("dd.MM.yyyy"),
                dateRaw = item.income_date.ToString("yyyy-MM-dd"),
                person = item.Person != null ? item.Person.person_name : "-",
                personId = item.person_id,
                type = item.IncomeType != null ? item.IncomeType.incometype_name : "-",
                typeId = item.incometype_id,
                payment = item.PaymentType != null ? item.PaymentType.paymenttype_name : "-",
                paymentId = item.paymenttype_id,
                desc = item.income_description,
                amountRaw = item.income_amount.ToString("N2", trCulture)
            };
        }
    }
}