using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class PaymentTypeController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public PaymentTypeController(BugraLifeDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _context.PaymentTypes.OrderBy(x => x.paymenttype_order).ToListAsync();

            int nextOrder = 1;
            if (list.Any())
            {
                nextOrder = list.Max(x => x.paymenttype_order) + 1;
            }
            ViewBag.NextOrder = nextOrder;

            return View(list);
        }

        // 2. EKLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentType paymentType)
        {
            if (ModelState.IsValid)
            {
                // 1. İsim Kontrolü
                bool exists = await _context.PaymentTypes.AnyAsync(x => x.paymenttype_name == paymentType.paymenttype_name);
                if (exists) return Json(new { success = false, message = "Bu ödeme türü zaten kayıtlı!" });

                // A. HESABI OLUŞTUR
                _context.PaymentTypes.Add(paymentType);
                await _context.SaveChangesAsync();

                // B. AÇILIŞ BAKİYESİ VARSA HAREKET OLUŞTUR
                if (paymentType.paymenttype_balance != 0)
                {
                    var defaultPersonId = await _context.Persons.Where(x => x.is_bank == true).Select(x => x.person_id).FirstOrDefaultAsync();
                    var defaultIncomeTypeId = await _context.IncomeTypes.Where(x => x.is_bank == true).Select(x => x.incometype_id).FirstOrDefaultAsync();
                    var defaultExpenseTypeId = await _context.ExpenseTypes.Where(x => x.is_bank == true).Select(x => x.expensetype_id).FirstOrDefaultAsync();

                    if (defaultPersonId != 0)
                    {
                        if (paymentType.paymenttype_balance > 0)
                        {
                            var income = new Income
                            {
                                paymenttype_id = paymentType.paymenttype_id,
                                income_amount = paymentType.paymenttype_balance,
                                income_date = DateTime.Now,
                                income_description = "Hesap Açılış Bakiyesi",
                                is_bankmovement = true,
                                person_id = defaultPersonId,
                                incometype_id = defaultIncomeTypeId != 0 ? defaultIncomeTypeId : 1
                            };
                            _context.Incomes.Add(income);
                        }
                        else
                        {
                            var expense = new Expense
                            {
                                paymenttype_id = paymentType.paymenttype_id,
                                expense_amount = Math.Abs(paymentType.paymenttype_balance),
                                expense_date = DateTime.Now,
                                expense_description = "Hesap Açılış Bakiyesi (Borç/Devir)",
                                is_bankmovement = true,
                                person_id = defaultPersonId,
                                expensetype_id = defaultExpenseTypeId != 0 ? defaultExpenseTypeId : 1
                            };
                            _context.Expenses.Add(expense);
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                // Eklenen veriyi geri dön
                return Json(new { success = true, message = "Hesap ve açılış fişi oluşturuldu!", data = paymentType });
            }
            return Json(new { success = false, message = "Form verileri geçersiz." });
        }

        // 3. GÜNCELLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PaymentType paymentType)
        {
            if (ModelState.IsValid)
            {
                // 1. İsim Çakışma Kontrolü
                bool exists = await _context.PaymentTypes.AnyAsync(x => x.paymenttype_name == paymentType.paymenttype_name && x.paymenttype_id != paymentType.paymenttype_id);
                if (exists)
                {
                    return Json(new { success = false, message = "Bu isimde başka bir kayıt zaten mevcut!" });
                }

                // 2. Mevcut Kaydı Çek
                var existingRecord = await _context.PaymentTypes.FindAsync(paymentType.paymenttype_id);
                if (existingRecord == null) return Json(new { success = false, message = "Kayıt bulunamadı." });

                // --- BAKİYE DÜZELTME MANTIĞI ---
                var totalIncome = await _context.Incomes.Where(x => x.paymenttype_id == paymentType.paymenttype_id).SumAsync(x => x.income_amount);
                var totalExpense = await _context.Expenses.Where(x => x.paymenttype_id == paymentType.paymenttype_id).SumAsync(x => x.expense_amount);
                decimal currentRealBalance = totalIncome - totalExpense;
                decimal targetBalance = paymentType.paymenttype_balance;
                decimal difference = targetBalance - currentRealBalance;

                if (difference != 0)
                {
                    var defaultPersonId = await _context.Persons.Where(x => x.is_bank == true).Select(x => x.person_id).FirstOrDefaultAsync();
                    var defaultIncomeTypeId = await _context.IncomeTypes.Where(x => x.is_bank == true).Select(x => x.incometype_id).FirstOrDefaultAsync();
                    var defaultExpenseTypeId = await _context.ExpenseTypes.Where(x => x.is_bank == true).Select(x => x.expensetype_id).FirstOrDefaultAsync();

                    if (defaultPersonId != 0)
                    {
                        if (difference > 0)
                        {
                            var income = new Income
                            {
                                paymenttype_id = paymentType.paymenttype_id,
                                income_amount = difference,
                                income_date = DateTime.Now,
                                income_description = "Bakiye Düzeltme Fişi (Manuel)",
                                is_bankmovement = true,
                                person_id = defaultPersonId,
                                incometype_id = defaultIncomeTypeId != 0 ? defaultIncomeTypeId : 1
                            };
                            _context.Incomes.Add(income);
                        }
                        else
                        {
                            var expense = new Expense
                            {
                                paymenttype_id = paymentType.paymenttype_id,
                                expense_amount = Math.Abs(difference),
                                expense_date = DateTime.Now,
                                expense_description = "Bakiye Düzeltme Fişi (Manuel)",
                                is_bankmovement = true,
                                person_id = defaultPersonId,
                                expensetype_id = defaultExpenseTypeId != 0 ? defaultExpenseTypeId : 1
                            };
                            _context.Expenses.Add(expense);
                        }
                    }
                }
                // --- MANTIK BİTİŞ ---

                // 3. Güncelle
                existingRecord.paymenttype_name = paymentType.paymenttype_name;
                existingRecord.paymenttype_order = paymentType.paymenttype_order;
                existingRecord.is_creditcard = paymentType.is_creditcard;
                existingRecord.paymenttype_balance = targetBalance;

                _context.PaymentTypes.Update(existingRecord);
                await _context.SaveChangesAsync();

                // Güncellenen veriyi geri dön
                return Json(new { success = true, message = "Güncelleme ve bakiye eşitleme başarılı!", data = existingRecord });
            }
            return Json(new { success = false, message = "Güncelleme başarısız." });
        }

        // 4. SİLME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.PaymentTypes.FindAsync(id);
            if (item != null)
            {
                _context.PaymentTypes.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Kayıt silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }
    }
}