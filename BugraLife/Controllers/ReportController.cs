using Microsoft.AspNetCore.Mvc;
using BugraLife.DBContext;
using BugraLife.Models;
using BugraLife.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public ReportController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. ANA SAYFA (Butonların olduğu yer)
        public IActionResult Index()
        {
            return View();
        }

        private static string Range(DateTime s, DateTime e) =>
            $"{ReportExport.D(s)} - {ReportExport.D(e)}";

        // ---------------------------------------------------------
        // 1. HESAP HAREKETLERİ (GÜNCEL)
        // ---------------------------------------------------------
        private async Task<AccountMovementViewModel> BuildAccountMovements(DateTime? startDate, DateTime? endDate, List<int> accountIds, List<int> personIds, bool fillViewBag)
        {
            var model = new AccountMovementViewModel
            {
                Movements = new List<MovementItem>(),
                StartDate = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                EndDate = endDate ?? DateTime.Now,
                SelectedAccountIds = accountIds ?? new List<int>(),
                SelectedPersonIds = personIds ?? new List<int>()
            };

            if (fillViewBag)
            {
                ViewBag.Accounts = await _context.PaymentTypes.Where(x => x.is_bank == false).OrderBy(x => x.paymenttype_order).ToListAsync();
                ViewBag.Persons = await _context.Persons.Where(x => x.is_bank == false).OrderBy(x => x.person_id).ToListAsync();
            }

            if ((accountIds == null || !accountIds.Any()) && (personIds == null || !personIds.Any()))
                return model;

            bool filterByAccount = accountIds != null && accountIds.Any() && !accountIds.Contains(-1);
            bool filterByPerson = personIds != null && personIds.Any() && !personIds.Contains(-1);

            var incomesQuery = _context.Incomes
                .Include(x => x.PaymentType)
                .Include(x => x.Person)
                .Where(x => x.income_date >= model.StartDate && x.income_date <= model.EndDate);

            if (filterByAccount) incomesQuery = incomesQuery.Where(x => accountIds.Contains(x.paymenttype_id));
            if (filterByPerson) incomesQuery = incomesQuery.Where(x => personIds.Contains(x.person_id));

            var incomes = await incomesQuery.Select(x => new MovementItem
            {
                Id = x.income_id,
                Date = x.income_date,
                AccountName = x.PaymentType.paymenttype_name,
                Description = x.income_description,
                Amount = x.income_amount,
                Type = "Gelir",
                IsExpense = false
            }).ToListAsync();

            var expensesQuery = _context.Expenses
                .Include(x => x.PaymentType)
                .Include(x => x.Person)
                .Where(x => x.expense_date >= model.StartDate && x.expense_date <= model.EndDate);

            if (filterByAccount) expensesQuery = expensesQuery.Where(x => accountIds.Contains(x.paymenttype_id));
            if (filterByPerson) expensesQuery = expensesQuery.Where(x => personIds.Contains(x.person_id));

            var expenses = await expensesQuery.Select(x => new MovementItem
            {
                Id = x.expense_id,
                Date = x.expense_date,
                AccountName = x.PaymentType.paymenttype_name,
                Description = x.expense_description,
                Amount = x.expense_amount,
                Type = "Gider",
                IsExpense = true
            }).ToListAsync();

            model.Movements.AddRange(incomes);
            model.Movements.AddRange(expenses);
            model.Movements = model.Movements.OrderByDescending(x => x.Date).ToList();

            model.TotalIncome = incomes.Sum(x => x.Amount);
            model.TotalExpense = expenses.Sum(x => x.Amount);
            model.NetBalance = model.TotalIncome - model.TotalExpense;

            return model;
        }

        public async Task<IActionResult> AccountMovements(DateTime? startDate, DateTime? endDate, List<int> accountIds, List<int> personIds)
            => View(await BuildAccountMovements(startDate, endDate, accountIds, personIds, true));

        public async Task<IActionResult> AccountMovementsExport(string format, DateTime? startDate, DateTime? endDate, List<int> accountIds, List<int> personIds)
        {
            var m = await BuildAccountMovements(startDate, endDate, accountIds, personIds, false);
            var headers = new[] { "Tarih", "Hesap", "Tür", "Açıklama", "Tutar" };
            var rows = m.Movements.Select(x => new[]
            {
                ReportExport.D(x.Date), x.AccountName ?? "", x.Type ?? "",
                x.Description ?? "", (x.IsExpense ? "-" : "+") + ReportExport.Money(x.Amount)
            }).ToList();
            var summary = new List<(string, string)>
            {
                ("Toplam Gelir", ReportExport.Money(m.TotalIncome)),
                ("Toplam Gider", ReportExport.Money(m.TotalExpense)),
                ("Net", ReportExport.Money(m.NetBalance))
            };
            return Export(format, "Hesap Hareketleri", Range(m.StartDate, m.EndDate), headers, rows, summary);
        }

        // ---------------------------------------------------------
        // 2. GİDER TÜRÜ HAREKETLERİ (GÜNCEL)
        // ---------------------------------------------------------
        private async Task<ExpenseTypeReportViewModel> BuildExpenseTypeMovements(DateTime? startDate, DateTime? endDate, List<int> typeIds, List<int> personIds, bool fillViewBag)
        {
            var model = new ExpenseTypeReportViewModel
            {
                Items = new List<ExpenseReportItem>(),
                StartDate = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                EndDate = endDate ?? DateTime.Now,
                SelectedTypeIds = typeIds ?? new List<int>(),
                SelectedPersonIds = personIds ?? new List<int>()
            };

            if (fillViewBag)
            {
                ViewBag.ExpenseTypes = await _context.ExpenseTypes.Where(x => x.is_bank == false).OrderBy(x => x.expensetype_order).ToListAsync();
                ViewBag.Persons = await _context.Persons.Where(x => x.is_bank == false).OrderBy(x => x.person_id).ToListAsync();
            }

            if ((typeIds == null || !typeIds.Any()) && (personIds == null || !personIds.Any())) return model;

            bool filterByType = typeIds != null && typeIds.Any() && !typeIds.Contains(-1);
            bool filterByPerson = personIds != null && personIds.Any() && !personIds.Contains(-1);

            var query = _context.Expenses
                .Include(x => x.ExpenseType)
                .Include(x => x.PaymentType)
                .Where(x => x.expense_date >= model.StartDate && x.expense_date <= model.EndDate);

            if (filterByType) query = query.Where(x => typeIds.Contains(x.expensetype_id));
            if (filterByPerson) query = query.Where(x => personIds.Contains(x.person_id));

            var expenses = await query.OrderByDescending(x => x.expense_date)
                .Select(x => new ExpenseReportItem
                {
                    Id = x.expense_id,
                    Date = x.expense_date,
                    CategoryName = x.ExpenseType.expensetype_name,
                    AccountName = x.PaymentType.paymenttype_name,
                    Description = x.expense_description,
                    Amount = x.expense_amount
                }).ToListAsync();

            model.Items = expenses;
            model.TotalAmount = expenses.Sum(x => x.Amount);
            return model;
        }

        public async Task<IActionResult> ExpenseTypeMovements(DateTime? startDate, DateTime? endDate, List<int> typeIds, List<int> personIds)
            => View(await BuildExpenseTypeMovements(startDate, endDate, typeIds, personIds, true));

        public async Task<IActionResult> ExpenseTypeMovementsExport(string format, DateTime? startDate, DateTime? endDate, List<int> typeIds, List<int> personIds)
        {
            var m = await BuildExpenseTypeMovements(startDate, endDate, typeIds, personIds, false);
            var headers = new[] { "Tarih", "Kategori", "Hesap", "Açıklama", "Tutar" };
            var rows = m.Items.Select(x => new[]
            {
                ReportExport.D(x.Date), x.CategoryName ?? "", x.AccountName ?? "",
                x.Description ?? "", ReportExport.Money(x.Amount)
            }).ToList();
            var summary = new List<(string, string)> { ("Toplam Harcama", ReportExport.Money(m.TotalAmount)) };
            return Export(format, "Gider Türü Hareketleri", Range(m.StartDate, m.EndDate), headers, rows, summary);
        }

        // ---------------------------------------------------------
        // 3. GELİR TÜRÜ HAREKETLERİ (GÜNCEL)
        // ---------------------------------------------------------
        private async Task<IncomeTypeReportViewModel> BuildIncomeTypeMovements(DateTime? startDate, DateTime? endDate, List<int> typeIds, List<int> personIds, bool fillViewBag)
        {
            var model = new IncomeTypeReportViewModel
            {
                Items = new List<IncomeReportItem>(),
                StartDate = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                EndDate = endDate ?? DateTime.Now,
                SelectedTypeIds = typeIds ?? new List<int>(),
                SelectedPersonIds = personIds ?? new List<int>()
            };

            if (fillViewBag)
            {
                ViewBag.IncomeTypes = await _context.IncomeTypes.Where(x => x.is_bank == false).OrderBy(x => x.incometype_order).ToListAsync();
                ViewBag.Persons = await _context.Persons.Where(x => x.is_bank == false).OrderBy(x => x.person_id).ToListAsync();
            }

            if ((typeIds == null || !typeIds.Any()) && (personIds == null || !personIds.Any())) return model;

            bool filterByType = typeIds != null && typeIds.Any() && !typeIds.Contains(-1);
            bool filterByPerson = personIds != null && personIds.Any() && !personIds.Contains(-1);

            var query = _context.Incomes
                .Include(x => x.IncomeType)
                .Include(x => x.PaymentType)
                .Where(x => x.income_date >= model.StartDate && x.income_date <= model.EndDate);

            if (filterByType) query = query.Where(x => typeIds.Contains(x.incometype_id));
            if (filterByPerson) query = query.Where(x => personIds.Contains(x.person_id));

            var incomes = await query.OrderByDescending(x => x.income_date)
                .Select(x => new IncomeReportItem
                {
                    Id = x.income_id,
                    Date = x.income_date,
                    CategoryName = x.IncomeType.incometype_name,
                    AccountName = x.PaymentType.paymenttype_name,
                    Description = x.income_description,
                    Amount = x.income_amount
                }).ToListAsync();

            model.Items = incomes;
            model.TotalAmount = incomes.Sum(x => x.Amount);
            return model;
        }

        public async Task<IActionResult> IncomeTypeMovements(DateTime? startDate, DateTime? endDate, List<int> typeIds, List<int> personIds)
            => View(await BuildIncomeTypeMovements(startDate, endDate, typeIds, personIds, true));

        public async Task<IActionResult> IncomeTypeMovementsExport(string format, DateTime? startDate, DateTime? endDate, List<int> typeIds, List<int> personIds)
        {
            var m = await BuildIncomeTypeMovements(startDate, endDate, typeIds, personIds, false);
            var headers = new[] { "Tarih", "Kategori", "Hesap", "Açıklama", "Tutar" };
            var rows = m.Items.Select(x => new[]
            {
                ReportExport.D(x.Date), x.CategoryName ?? "", x.AccountName ?? "",
                x.Description ?? "", ReportExport.Money(x.Amount)
            }).ToList();
            var summary = new List<(string, string)> { ("Toplam Gelir", ReportExport.Money(m.TotalAmount)) };
            return Export(format, "Gelir Türü Hareketleri", Range(m.StartDate, m.EndDate), headers, rows, summary);
        }

        // ---------------------------------------------------------
        // 4. BORÇ / ALACAK RAPORU (Ingredient Gruplu)
        // ---------------------------------------------------------
        private async Task<DebtReceivableViewModel> BuildDebtReceivable(DateTime? startDate, DateTime? endDate, List<int> debtorIds, List<int> ingredientIds, bool fillViewBag)
        {
            var model = new DebtReceivableViewModel
            {
                GroupedItems = new List<DebtGroupedItem>(),
                Details = new List<Movement>(),
                StartDate = startDate ?? new DateTime(DateTime.Now.Year, 1, 1),
                EndDate = endDate ?? DateTime.Now,
                SelectedDebtorIds = debtorIds ?? new List<int>(),
                SelectedIngredientIds = ingredientIds ?? new List<int>()
            };

            if (fillViewBag)
            {
                ViewBag.Debtors = await _context.Debtors.OrderBy(x => x.debtor_name).ToListAsync();
                ViewBag.Ingredients = await _context.Ingredients.OrderBy(x => x.ingredient_name).ToListAsync();
            }

            if ((debtorIds == null || !debtorIds.Any()) && (ingredientIds == null || !ingredientIds.Any()))
                return model;

            bool filterByDebtor = debtorIds != null && debtorIds.Any() && !debtorIds.Contains(-1);
            bool filterByIngredient = ingredientIds != null && ingredientIds.Any() && !ingredientIds.Contains(-1);

            var query = _context.Movements
                .Include(x => x.Debtor)
                .Include(x => x.Ingredient)
                .Include(x => x.Person)
                .Where(x => x.movement_date <= model.EndDate);

            if (filterByDebtor) query = query.Where(x => debtorIds.Contains(x.debtor_id));
            if (filterByIngredient) query = query.Where(x => ingredientIds.Contains(x.ingredient_id));

            var movements = await query.OrderByDescending(x => x.movement_date).ToListAsync();
            model.Details = movements;

            model.GroupedItems = movements
                .GroupBy(x => x.Ingredient.ingredient_name)
                .Select(g => new DebtGroupedItem
                {
                    IngredientName = g.Key,
                    TotalAmount = g.Sum(x => x.movement_amount),
                    Status = g.Sum(x => x.movement_amount) >= 0 ? "Alacak" : "Borç"
                })
                .ToList();

            model.TotalReceivable = movements.Where(x => x.movement_amount > 0).Sum(x => x.movement_amount);
            model.TotalDebt = movements.Where(x => x.movement_amount < 0).Sum(x => x.movement_amount);
            model.NetBalance = model.TotalReceivable + model.TotalDebt;
            return model;
        }

        public async Task<IActionResult> DebtReceivableReport(DateTime? startDate, DateTime? endDate, List<int> debtorIds, List<int> ingredientIds)
            => View(await BuildDebtReceivable(startDate, endDate, debtorIds, ingredientIds, true));

        public async Task<IActionResult> DebtReceivableReportExport(string format, DateTime? startDate, DateTime? endDate, List<int> debtorIds, List<int> ingredientIds)
        {
            var m = await BuildDebtReceivable(startDate, endDate, debtorIds, ingredientIds, false);
            var headers = new[] { "Tarih", "Borçlu/Alacaklı", "Varlık", "Kişi", "Açıklama", "Tutar" };
            var rows = m.Details.Select(x => new[]
            {
                ReportExport.D(x.movement_date), x.Debtor?.debtor_name ?? "", x.Ingredient?.ingredient_name ?? "",
                x.Person?.person_name ?? "", x.movement_description ?? "", ReportExport.Money(x.movement_amount)
            }).ToList();
            var summary = new List<(string, string)>
            {
                ("Toplam Alacak", ReportExport.Money(m.TotalReceivable)),
                ("Toplam Borç", ReportExport.Money(m.TotalDebt)),
                ("Net", ReportExport.Money(m.NetBalance))
            };
            return Export(format, "Borç / Alacak Raporu", $"Bitiş: {ReportExport.D(m.EndDate)}", headers, rows, summary);
        }

        // ---------------------------------------------------------
        // 5. PORTFÖY / VARLIK RAPORU
        // ---------------------------------------------------------
        private async Task<PortfolioReportViewModel> BuildPortfolio(DateTime? startDate, DateTime? endDate, List<int> ingredientIds, List<int> personIds, bool fillViewBag)
        {
            var model = new PortfolioReportViewModel
            {
                GroupedItems = new List<PortfolioGroupedItem>(),
                Details = new List<Asset>(),
                StartDate = startDate ?? new DateTime(DateTime.Now.Year, 1, 1),
                EndDate = endDate ?? DateTime.Now,
                SelectedIngredientIds = ingredientIds ?? new List<int>(),
                SelectedPersonIds = personIds ?? new List<int>()
            };

            if (fillViewBag)
            {
                ViewBag.Ingredients = await _context.Ingredients.OrderBy(x => x.ingredient_name).ToListAsync();
                ViewBag.Persons = await _context.Persons.Where(x => x.is_bank == false).OrderBy(x => x.person_id).ToListAsync();
            }

            if ((ingredientIds == null || !ingredientIds.Any()) && (personIds == null || !personIds.Any()))
                return model;

            bool filterByIngredient = ingredientIds != null && ingredientIds.Any() && !ingredientIds.Contains(-1);
            bool filterByPerson = personIds != null && personIds.Any() && !personIds.Contains(-1);

            var baseQuery = _context.Assets
                .Include(x => x.Ingredient)
                .Include(x => x.Person)
                .Where(x => x.asset_date <= model.EndDate);

            if (filterByIngredient) baseQuery = baseQuery.Where(x => ingredientIds.Contains(x.ingredient_id));
            if (filterByPerson) baseQuery = baseQuery.Where(x => personIds.Contains(x.person_id));

            var allAssetsUpToEndDate = await baseQuery.ToListAsync();

            model.Details = allAssetsUpToEndDate
                .Where(x => x.asset_date >= model.StartDate)
                .OrderByDescending(x => x.asset_date)
                .ToList();

            model.GroupedItems = allAssetsUpToEndDate
                .GroupBy(x => x.Ingredient.ingredient_name)
                .Select(g => new PortfolioGroupedItem
                {
                    IngredientName = g.Key,
                    TotalAmount = g.Sum(x => x.asset_amount)
                })
                .ToList();

            return model;
        }

        public async Task<IActionResult> PortfolioReport(DateTime? startDate, DateTime? endDate, List<int> ingredientIds, List<int> personIds)
            => View(await BuildPortfolio(startDate, endDate, ingredientIds, personIds, true));

        public async Task<IActionResult> PortfolioReportExport(string format, DateTime? startDate, DateTime? endDate, List<int> ingredientIds, List<int> personIds)
        {
            var m = await BuildPortfolio(startDate, endDate, ingredientIds, personIds, false);
            var headers = new[] { "Tarih", "Varlık", "Kişi", "Açıklama", "Miktar" };
            var rows = m.Details.Select(x => new[]
            {
                ReportExport.D(x.asset_date), x.Ingredient?.ingredient_name ?? "", x.Person?.person_name ?? "",
                x.asset_description ?? "", ReportExport.Qty(x.asset_amount)
            }).ToList();
            var summary = m.GroupedItems
                .Select(g => (g.IngredientName ?? "", ReportExport.Qty(g.TotalAmount)))
                .ToList();
            return Export(format, "Portföy Raporu", Range(m.StartDate, m.EndDate), headers, rows, summary);
        }

        // ---------------------------------------------------------
        // 6. HESAP BAKİYELERİ RAPORU (Tarihli Bakiye Durumu)
        // ---------------------------------------------------------
        private async Task<AccountBalanceReportViewModel> BuildAccountBalances(DateTime? filterDate)
        {
            var selectedDate = filterDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Now;

            var model = new AccountBalanceReportViewModel
            {
                FilterDate = selectedDate,
                Accounts = new List<AccountBalanceItem>()
            };

            var accounts = await _context.PaymentTypes.Where(x => x.is_bank == false).ToListAsync();

            var allIncomes = await _context.Incomes
                .Where(x => x.income_date <= selectedDate)
                .GroupBy(x => x.paymenttype_id)
                .Select(g => new { AccId = g.Key, Total = g.Sum(x => x.income_amount) })
                .ToListAsync();

            var allExpenses = await _context.Expenses
                .Where(x => x.expense_date <= selectedDate)
                .GroupBy(x => x.paymenttype_id)
                .Select(g => new { AccId = g.Key, Total = g.Sum(x => x.expense_amount) })
                .ToListAsync();

            foreach (var acc in accounts)
            {
                decimal totalInc = allIncomes.FirstOrDefault(x => x.AccId == acc.paymenttype_id)?.Total ?? 0;
                decimal totalExp = allExpenses.FirstOrDefault(x => x.AccId == acc.paymenttype_id)?.Total ?? 0;
                decimal calculatedBalance = totalInc - totalExp;

                model.Accounts.Add(new AccountBalanceItem
                {
                    AccountName = acc.paymenttype_name,
                    IsBank = acc.is_bank,
                    IsCreditCard = acc.is_creditcard,
                    Balance = calculatedBalance
                });
            }

            model.TotalAssets = model.Accounts.Where(x => !x.IsCreditCard).Sum(x => x.Balance);
            model.TotalLiabilities = model.Accounts.Where(x => x.IsCreditCard).Sum(x => x.Balance);
            model.NetWorth = model.TotalAssets + model.TotalLiabilities;
            return model;
        }

        public async Task<IActionResult> AccountBalances(DateTime? filterDate)
            => View(await BuildAccountBalances(filterDate));

        public async Task<IActionResult> AccountBalancesExport(string format, DateTime? filterDate)
        {
            var m = await BuildAccountBalances(filterDate);
            var headers = new[] { "Hesap", "Tür", "Bakiye" };
            var rows = m.Accounts.Select(x => new[]
            {
                x.AccountName ?? "",
                x.IsCreditCard ? "Kredi Kartı" : (x.IsBank ? "Banka" : "Hesap"),
                ReportExport.Money(x.Balance)
            }).ToList();
            var summary = new List<(string, string)>
            {
                ("Toplam Varlık", ReportExport.Money(m.TotalAssets)),
                ("Toplam Borç", ReportExport.Money(m.TotalLiabilities)),
                ("Net Durum", ReportExport.Money(m.NetWorth))
            };
            return Export(format, "Hesap Bakiyeleri", $"Tarih: {ReportExport.D(m.FilterDate)}", headers, rows, summary);
        }

        // ---------------------------------------------------------
        // 7. GENEL GELİR / GİDER RAPORU (TAM VERSİYON)
        // ---------------------------------------------------------
        private async Task<IncomeExpenseReportViewModel> BuildIncomeExpense(DateTime? startDate, DateTime? endDate, List<int> personIds, bool fillViewBag)
        {
            var model = new IncomeExpenseReportViewModel
            {
                StartDate = startDate ?? new DateTime(DateTime.Now.Year, 1, 1),
                EndDate = endDate ?? DateTime.Now,
                SelectedPersonIds = personIds ?? new List<int>(),
                IncomeCategories = new List<CategorySummary>(),
                ExpenseCategories = new List<CategorySummary>(),
                Details = new List<ReportMovementItem>(),
                Timeline = new List<TimelineSummary>()
            };

            if (fillViewBag)
                ViewBag.Persons = await _context.Persons.Where(x => x.is_bank == false).OrderBy(x => x.person_id).ToListAsync();

            bool filterByPerson = personIds != null && personIds.Any() && !personIds.Contains(-1);

            var incomeQuery = _context.Incomes
                .Include(x => x.IncomeType)
                .Include(x => x.PaymentType)
                .Include(x => x.Person)
                .Where(x => x.income_date >= model.StartDate && x.income_date <= model.EndDate && x.is_bankmovement == false);

            if (filterByPerson) incomeQuery = incomeQuery.Where(x => personIds.Contains(x.person_id));

            var incomes = await incomeQuery.ToListAsync();
            model.TotalIncome = incomes.Sum(x => x.income_amount);

            if (model.TotalIncome > 0)
            {
                model.IncomeCategories = incomes
                    .GroupBy(x => x.IncomeType.incometype_name)
                    .Select(g => new CategorySummary
                    {
                        Name = g.Key,
                        Amount = g.Sum(x => x.income_amount),
                        Percentage = (double)(g.Sum(x => x.income_amount) / model.TotalIncome) * 100
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToList();
            }

            var incomeDetails = incomes.Select(x => new ReportMovementItem
            {
                Date = x.income_date,
                CategoryName = x.IncomeType?.incometype_name ?? "Diğer",
                AccountName = x.PaymentType?.paymenttype_name ?? "-",
                PersonName = x.Person?.person_name ?? "-",
                Description = x.income_description,
                Amount = x.income_amount,
                IsExpense = false
            });

            var expenseQuery = _context.Expenses
                .Include(x => x.ExpenseType)
                .Include(x => x.PaymentType)
                .Include(x => x.Person)
                .Where(x => x.expense_date >= model.StartDate && x.expense_date <= model.EndDate && x.is_bankmovement == false);

            if (filterByPerson) expenseQuery = expenseQuery.Where(x => personIds.Contains(x.person_id));

            var expenses = await expenseQuery.ToListAsync();
            model.TotalExpense = expenses.Sum(x => x.expense_amount);

            if (model.TotalExpense > 0)
            {
                model.ExpenseCategories = expenses
                    .GroupBy(x => x.ExpenseType.expensetype_name)
                    .Select(g => new CategorySummary
                    {
                        Name = g.Key,
                        Amount = g.Sum(x => x.expense_amount),
                        Percentage = (double)(g.Sum(x => x.expense_amount) / model.TotalExpense) * 100
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToList();
            }

            var expenseDetails = expenses.Select(x => new ReportMovementItem
            {
                Date = x.expense_date,
                CategoryName = x.ExpenseType?.expensetype_name ?? "Diğer",
                AccountName = x.PaymentType?.paymenttype_name ?? "-",
                PersonName = x.Person?.person_name ?? "-",
                Description = x.expense_description,
                Amount = x.expense_amount,
                IsExpense = true
            });

            model.Details.AddRange(incomeDetails);
            model.Details.AddRange(expenseDetails);
            model.Details = model.Details.OrderByDescending(x => x.Date).ToList();

            model.NetResult = model.TotalIncome - model.TotalExpense;

            var incomeByDate = incomes
                .GroupBy(x => x.income_date.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.income_amount) })
                .ToList();

            var expenseByDate = expenses
                .GroupBy(x => x.expense_date.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.expense_amount) })
                .ToList();

            var allDates = incomeByDate.Select(x => x.Date)
                           .Union(expenseByDate.Select(x => x.Date))
                           .OrderBy(x => x)
                           .ToList();

            foreach (var date in allDates)
            {
                var inc = incomeByDate.FirstOrDefault(x => x.Date == date)?.Total ?? 0;
                var exp = expenseByDate.FirstOrDefault(x => x.Date == date)?.Total ?? 0;

                model.Timeline.Add(new TimelineSummary
                {
                    DateLabel = date.ToString("dd.MM.yyyy"),
                    DailyIncome = inc,
                    DailyExpense = exp,
                    DailyNet = inc - exp
                });
            }

            return model;
        }

        public async Task<IActionResult> IncomeExpenseReport(DateTime? startDate, DateTime? endDate, List<int> personIds)
            => View(await BuildIncomeExpense(startDate, endDate, personIds, true));

        public async Task<IActionResult> IncomeExpenseReportExport(string format, DateTime? startDate, DateTime? endDate, List<int> personIds)
        {
            var m = await BuildIncomeExpense(startDate, endDate, personIds, false);
            var headers = new[] { "Tarih", "Kategori", "Hesap", "Kişi", "Açıklama", "Yön", "Tutar" };
            var rows = m.Details.Select(x => new[]
            {
                ReportExport.D(x.Date), x.CategoryName ?? "", x.AccountName ?? "", x.PersonName ?? "",
                x.Description ?? "", x.IsExpense ? "Gider" : "Gelir", ReportExport.Money(x.Amount)
            }).ToList();
            var summary = new List<(string, string)>
            {
                ("Toplam Gelir", ReportExport.Money(m.TotalIncome)),
                ("Toplam Gider", ReportExport.Money(m.TotalExpense)),
                ("Net Sonuç", ReportExport.Money(m.NetResult))
            };
            return Export(format, "Genel Gelir / Gider Raporu", Range(m.StartDate, m.EndDate), headers, rows, summary);
        }

        // ---------------------------------------------------------
        // ORTAK EXPORT YARDIMCISI
        // ---------------------------------------------------------
        private IActionResult Export(string format, string title, string subtitle, string[] headers,
            List<string[]> rows, List<(string label, string value)> summary)
        {
            string file = ReportExport.SafeName(title);
            if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = ReportExport.Pdf(title, subtitle, headers, rows, summary);
                return File(bytes, "application/pdf", $"{file}.pdf");
            }

            var xlsx = ReportExport.Excel(title, subtitle, headers, rows, summary);
            return File(xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{file}.xlsx");
        }
    }
}
