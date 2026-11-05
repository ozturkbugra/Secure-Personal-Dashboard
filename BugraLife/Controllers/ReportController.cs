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
        // 8. TAKSİTLİ İŞLEMLER RAPORU (KALANLAR & GEÇMİŞ)
        // ---------------------------------------------------------
        private async Task<InstallmentReportViewModel> BuildInstallments(bool isHistory)
        {
            var today = DateTime.Today;
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);

            var query = _context.Expenses
                .Include(x => x.PaymentType)
                .Include(x => x.ExpenseType)
                .Include(x => x.Person)
                .Where(x => x.is_bankmovement == false &&
                            x.PaymentType != null &&
                            x.PaymentType.is_creditcard &&
                            x.expense_description != null &&
                            x.expense_description.Contains("Taksit"));

            if (isHistory)
            {
                // Geçmiş taksitler: Bu aydan önceki aylar
                query = query.Where(x => x.expense_date < currentMonthStart);
            }
            else
            {
                // Kalan taksitler: Bu aydan ve ileriye dönük
                query = query.Where(x => x.expense_date >= currentMonthStart);
            }

            var expenses = await query
                .OrderBy(x => x.expense_date)
                .ThenBy(x => x.expense_id)
                .AsNoTracking()
                .ToListAsync();

            if (isHistory)
            {
                // Geçmişte en yeni aylar en üstte görünsün
                expenses = expenses.OrderByDescending(x => x.expense_date).ThenByDescending(x => x.expense_id).ToList();
            }

            var trCulture = new System.Globalization.CultureInfo("tr-TR");
            var model = new InstallmentReportViewModel
            {
                IsHistory = isHistory,
                TotalAmount = expenses.Sum(x => x.expense_amount),
                TotalCount = expenses.Count
            };

            // Ay ay grupla (Year, Month)
            var monthGroups = expenses
                .GroupBy(x => new { x.expense_date.Year, x.expense_date.Month })
                .Select(mg =>
                {
                    var firstDate = new DateTime(mg.Key.Year, mg.Key.Month, 1);
                    var monthName = firstDate.ToString("MMMM yyyy", trCulture);
                    // Baş harfi büyük
                    monthName = char.ToUpper(monthName[0]) + monthName.Substring(1);

                    var mGroup = new InstallmentMonthGroup
                    {
                        Year = mg.Key.Year,
                        Month = mg.Key.Month,
                        MonthName = monthName,
                        MonthTotal = mg.Sum(x => x.expense_amount),
                        InstallmentCount = mg.Count()
                    };

                    // Her ayın içini ödeme hesabına (Kredi Kartı / Hesap) göre grupla
                    var accountGroups = mg
                        .GroupBy(x => new
                        {
                            Id = x.paymenttype_id,
                            Name = x.PaymentType != null ? x.PaymentType.paymenttype_name : "Bilinmeyen Hesap",
                            IsCard = x.PaymentType != null && x.PaymentType.is_creditcard,
                            Order = x.PaymentType != null ? x.PaymentType.paymenttype_order : 999
                        })
                        .OrderByDescending(ag => ag.Key.IsCard) // Kredi kartları üstte
                        .ThenBy(ag => ag.Key.Order)
                        .Select(ag =>
                        {
                            var aGroup = new InstallmentAccountGroup
                            {
                                PaymentTypeId = ag.Key.Id,
                                AccountName = ag.Key.Name,
                                IsCreditCard = ag.Key.IsCard,
                                AccountTotal = ag.Sum(x => x.expense_amount),
                                InstallmentCount = ag.Count()
                            };

                            // Hesap içindeki taksit kalemleri
                            aGroup.Items = ag.Select(item =>
                            {
                                string desc = item.expense_description ?? "";
                                string instInfo = "";
                                int idx = desc.IndexOf("Taksit", StringComparison.OrdinalIgnoreCase);
                                if (idx > 0)
                                {
                                    instInfo = desc.Substring(0, idx + 6).Trim();
                                }

                                return new InstallmentItemDetail
                                {
                                    ExpenseId = item.expense_id,
                                    ExpenseDate = item.expense_date,
                                    Amount = item.expense_amount,
                                    Description = desc,
                                    ExpenseTypeName = item.ExpenseType?.expensetype_name ?? "-",
                                    PersonName = item.Person?.person_name ?? "-",
                                    InstallmentInfo = instInfo
                                };
                            }).ToList();

                            return aGroup;
                        }).ToList();

                    mGroup.Accounts = accountGroups;
                    return mGroup;
                }).ToList();

            model.Months = monthGroups;
            model.DistinctMonthCount = monthGroups.Count;

            return model;
        }

        // Kalan Taksitler Sayfası
        public async Task<IActionResult> Installments()
        {
            var model = await BuildInstallments(isHistory: false);
            return View(model);
        }

        // Geçmiş Taksitler Sayfası
        public async Task<IActionResult> InstallmentsHistory()
        {
            var model = await BuildInstallments(isHistory: true);
            return View("Installments", model);
        }

        // Taksitler Excel / PDF Export
        public async Task<IActionResult> InstallmentsExport(string format, bool isHistory = false)
        {
            var model = await BuildInstallments(isHistory);
            var title = isHistory ? "Geçmiş Taksitler Raporu" : "Kalan Taksitler Raporu";
            var subtitle = isHistory
                ? $"Geçmiş Dönem Taksitli Harcamalar - Toplam: {ReportExport.Money(model.TotalAmount)}"
                : $"Gelecek Dönem Kalan Taksit Ödemeleri - Toplam: {ReportExport.Money(model.TotalAmount)}";

            var headers = new[] { "Dönem", "Tarih", "Ödeme Hesabı / Kart", "Gider Türü", "Kişi", "Açıklama", "Tutar" };
            var rows = new List<string[]>();

            foreach (var m in model.Months)
            {
                foreach (var a in m.Accounts)
                {
                    foreach (var it in a.Items)
                    {
                        rows.Add(new[]
                        {
                            m.MonthName,
                            ReportExport.D(it.ExpenseDate),
                            a.AccountName,
                            it.ExpenseTypeName,
                            it.PersonName,
                            it.Description,
                            ReportExport.Money(it.Amount)
                        });
                    }
                }
            }

            var summary = new List<(string label, string value)>
            {
                ("Toplam Taksit Tutarı", ReportExport.Money(model.TotalAmount)),
                ("Toplam Taksit Adedi", model.TotalCount.ToString()),
                ("Ay Sayısı", model.DistinctMonthCount.ToString())
            };

            return Export(format, title, subtitle, headers, rows, summary);
        }


        // ---------------------------------------------------------
        // 9. AYLIK KATEGORİ BAZLI GİDER RAPORU
        // ---------------------------------------------------------
        private static readonly string[] TurkishMonths = new[]
        {
            "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
            "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
        };

        private async Task<MonthlyCategoryExpenseViewModel> BuildMonthlyCategoryExpenses(
            int? year, int? month, List<int> typeIds, List<int> personIds, bool fillViewBag)
        {
            var availableYears = await _context.Expenses
                .Where(x => !x.is_bankmovement)
                .Select(x => x.expense_date.Year)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();

            if (!availableYears.Any())
            {
                availableYears = new List<int> { DateTime.Now.Year };
            }

            int? selectedYear = year ?? (availableYears.Contains(DateTime.Now.Year) ? DateTime.Now.Year : availableYears.First());

            int activeMonth = month.HasValue && month.Value >= 1 && month.Value <= 12
                ? month.Value
                : DateTime.Now.Month;

            var allExpenseTypes = await _context.ExpenseTypes
                .AsNoTracking()
                .Where(x => !x.is_bank)
                .OrderBy(x => x.expensetype_order)
                .ToListAsync();

            var allPersons = await _context.Persons
                .AsNoTracking()
                .Where(x => !x.is_bank)
                .OrderBy(x => x.person_id)
                .ToListAsync();

            if (fillViewBag)
            {
                ViewBag.ExpenseTypes = allExpenseTypes;
                ViewBag.Persons = allPersons;
            }

            List<int> validSelectedTypeIds;
            if (typeIds == null || !typeIds.Any())
            {
                validSelectedTypeIds = allExpenseTypes.Select(x => x.expensetype_id).ToList();
            }
            else
            {
                validSelectedTypeIds = typeIds.Where(x => x > 0).ToList();
            }

            List<int> validSelectedPersonIds;
            if (personIds == null || !personIds.Any())
            {
                validSelectedPersonIds = allPersons.Select(x => x.person_id).ToList();
            }
            else
            {
                validSelectedPersonIds = personIds.Where(x => x > 0).ToList();
            }

            var model = new MonthlyCategoryExpenseViewModel
            {
                SelectedYear = selectedYear,
                AvailableYears = availableYears,
                SelectedPersonIds = validSelectedPersonIds,
                SelectedTypeIds = validSelectedTypeIds,
                ActiveMonth = activeMonth,
                ActiveMonthName = TurkishMonths[activeMonth - 1]
            };

            var query = _context.Expenses
                .AsNoTracking()
                .Include(x => x.ExpenseType)
                .Include(x => x.PaymentType)
                .Include(x => x.Person)
                .Where(x => !x.is_bankmovement);

            if (selectedYear.HasValue && selectedYear.Value > 0)
            {
                query = query.Where(x => x.expense_date.Year == selectedYear.Value);
            }

            if (validSelectedTypeIds.Count < allExpenseTypes.Count)
            {
                query = query.Where(x => validSelectedTypeIds.Contains(x.expensetype_id));
            }

            if (validSelectedPersonIds.Count < allPersons.Count)
            {
                query = query.Where(x => validSelectedPersonIds.Contains(x.person_id));
            }

            var rawExpenses = await query.OrderByDescending(x => x.expense_date).ToListAsync();

            model.TotalExpense = rawExpenses.Sum(x => x.expense_amount);

            model.AllExpenses = rawExpenses.Select(x => new ExpenseDetailItem
            {
                ExpenseId = x.expense_id,
                ExpenseDate = x.expense_date,
                Month = x.expense_date.Month,
                Year = x.expense_date.Year,
                CategoryId = x.expensetype_id,
                CategoryName = x.ExpenseType?.expensetype_name ?? "Diğer",
                AccountName = x.PaymentType?.paymenttype_name ?? "-",
                PersonName = x.Person?.person_name ?? "-",
                Description = x.expense_description ?? "",
                Amount = x.expense_amount
            }).ToList();

            foreach (var exp in rawExpenses)
            {
                int mIdx = exp.expense_date.Month - 1;
                if (mIdx >= 0 && mIdx < 12)
                {
                    model.MonthlyTotals[mIdx] += exp.expense_amount;
                    model.MonthlyCounts[mIdx]++;
                }
            }

            var categoryGroups = rawExpenses
                .GroupBy(x => new { x.expensetype_id, Name = x.ExpenseType?.expensetype_name ?? "Diğer" });

            foreach (var grp in categoryGroups)
            {
                var row = new MonthlyCategoryRow
                {
                    CategoryId = grp.Key.expensetype_id,
                    CategoryName = grp.Key.Name,
                    TotalAmount = 0
                };

                foreach (var item in grp)
                {
                    int mIdx = item.expense_date.Month - 1;
                    if (mIdx >= 0 && mIdx < 12)
                    {
                        row.MonthlyAmounts[mIdx] += item.expense_amount;
                    }
                    row.TotalAmount += item.expense_amount;
                }

                row.Percentage = model.TotalExpense > 0
                    ? (double)(row.TotalAmount / model.TotalExpense * 100)
                    : 0;

                model.CategoryRows.Add(row);
            }

            model.CategoryRows = model.CategoryRows.OrderByDescending(x => x.TotalAmount).ToList();

            if (model.CategoryRows.Any())
            {
                var topCat = model.CategoryRows.First();
                model.TopCategoryName = topCat.CategoryName;
                model.TopCategoryAmount = topCat.TotalAmount;
            }

            decimal maxMonthAmount = -1;
            int topMonthIdx = -1;
            for (int i = 0; i < 12; i++)
            {
                if (model.MonthlyTotals[i] > maxMonthAmount)
                {
                    maxMonthAmount = model.MonthlyTotals[i];
                    topMonthIdx = i;
                }
            }

            if (topMonthIdx >= 0 && maxMonthAmount > 0)
            {
                model.TopMonthName = TurkishMonths[topMonthIdx];
                model.TopMonthAmount = maxMonthAmount;
            }

            int monthsWithExpense = model.MonthlyTotals.Count(x => x > 0);
            model.MonthlyAverage = monthsWithExpense > 0
                ? model.TotalExpense / monthsWithExpense
                : 0;

            var activeExpenses = model.AllExpenses.Where(x => x.Month == activeMonth).ToList();
            model.ActiveMonthExpenses = activeExpenses;
            model.ActiveMonthTotal = activeExpenses.Sum(x => x.Amount);

            if (model.ActiveMonthTotal > 0)
            {
                model.ActiveMonthCategories = activeExpenses
                    .GroupBy(x => new { x.CategoryId, x.CategoryName })
                    .Select(g => new CategoryBreakdownItem
                    {
                        CategoryId = g.Key.CategoryId,
                        CategoryName = g.Key.CategoryName,
                        Amount = g.Sum(x => x.Amount),
                        TransactionCount = g.Count(),
                        Percentage = (double)(g.Sum(x => x.Amount) / model.ActiveMonthTotal * 100)
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToList();
            }

            return model;
        }

        public async Task<IActionResult> MonthlyCategoryExpenses(int? year, int? month, List<int> typeIds, List<int> personIds)
            => View(await BuildMonthlyCategoryExpenses(year, month, typeIds, personIds, true));

        public async Task<IActionResult> MonthlyCategoryExpensesExport(string format, int? year, int? month, List<int> typeIds, List<int> personIds, string exportType = "matrix")
        {
            var model = await BuildMonthlyCategoryExpenses(year, month, typeIds, personIds, false);
            string yearTitle = model.SelectedYear.HasValue && model.SelectedYear.Value > 0
                ? $"{model.SelectedYear} Yılı"
                : "Tüm Yıllar";

            if (exportType == "detail")
            {
                string title = $"Aylık Gider Detayı - {model.ActiveMonthName} ({yearTitle})";
                string subtitle = $"Toplam Harcama: {ReportExport.Money(model.ActiveMonthTotal)} ({model.ActiveMonthExpenses.Count} adet işlem)";
                var headers = new[] { "Tarih", "Kategori", "Ödenen Hesap", "Kişi", "Açıklama", "Tutar" };
                var rows = model.ActiveMonthExpenses.Select(x => new[]
                {
                    ReportExport.D(x.ExpenseDate),
                    x.CategoryName,
                    x.AccountName,
                    x.PersonName,
                    x.Description,
                    ReportExport.Money(x.Amount)
                }).ToList();

                var summary = new List<(string label, string value)>
                {
                    ("Dönem", $"{model.ActiveMonthName} {yearTitle}"),
                    ("İşlem Adedi", model.ActiveMonthExpenses.Count.ToString()),
                    ("Toplam Tutar", ReportExport.Money(model.ActiveMonthTotal))
                };

                return Export(format, title, subtitle, headers, rows, summary);
            }
            else
            {
                string title = $"Gider Türlerine Göre Aylık Harcama Raporu ({yearTitle})";
                string subtitle = $"Toplam Harcama: {ReportExport.Money(model.TotalExpense)} - Aylık Ortalama: {ReportExport.Money(model.MonthlyAverage)}";

                var headers = new[]
                {
                    "Gider Türü", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
                    "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık", "Toplam", "Oran"
                };

                var rows = new List<string[]>();
                foreach (var row in model.CategoryRows)
                {
                    var r = new List<string> { row.CategoryName };
                    for (int i = 0; i < 12; i++)
                    {
                        r.Add(row.MonthlyAmounts[i] > 0 ? ReportExport.Money(row.MonthlyAmounts[i]) : "-");
                    }
                    r.Add(ReportExport.Money(row.TotalAmount));
                    r.Add($"%{row.Percentage:0.0}");
                    rows.Add(r.ToArray());
                }

                var totalRow = new List<string> { "TOPLAM" };
                for (int i = 0; i < 12; i++)
                {
                    totalRow.Add(model.MonthlyTotals[i] > 0 ? ReportExport.Money(model.MonthlyTotals[i]) : "-");
                }
                totalRow.Add(ReportExport.Money(model.TotalExpense));
                totalRow.Add("%100.0");
                rows.Add(totalRow.ToArray());

                var summary = new List<(string label, string value)>
                {
                    ("Yıl", yearTitle),
                    ("Genel Toplam Gider", ReportExport.Money(model.TotalExpense)),
                    ("Aylık Ortalama", ReportExport.Money(model.MonthlyAverage)),
                    ("En Çok Harcanan Kategori", $"{model.TopCategoryName} ({ReportExport.Money(model.TopCategoryAmount)})"),
                    ("En Çok Harcama Yapılan Ay", $"{model.TopMonthName} ({ReportExport.Money(model.TopMonthAmount)})")
                };

                return Export(format, title, subtitle, headers, rows, summary);
            }
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
