using Microsoft.AspNetCore.Mvc;
using BugraLife.DBContext;
using BugraLife.Models;
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

        // ---------------------------------------------------------
        // 1. HESAP HAREKETLERİ (GÜNCEL)
        // ---------------------------------------------------------
        public async Task<IActionResult> AccountMovements(DateTime? startDate, DateTime? endDate, List<int> accountIds, List<int> personIds)
        {
            var model = new AccountMovementViewModel
            {
                Movements = new List<MovementItem>(),
                StartDate = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                EndDate = endDate ?? DateTime.Now,
                SelectedAccountIds = accountIds ?? new List<int>(),
                SelectedPersonIds = personIds ?? new List<int>()
            };

            ViewBag.Accounts = await _context.PaymentTypes.Where(x=> x.is_bank == false).OrderBy(x => x.paymenttype_order).ToListAsync();
            ViewBag.Persons = await _context.Persons.Where(x => x.is_bank == false).OrderBy(x => x.person_id).ToListAsync();

            // Eğer hiç seçim yoksa işlem yapma (Sayfa ilk açıldığında boş gelsin)
            // NOT: Kullanıcı "Tümü" seçerse listede -1 olacak, o yüzden count > 0 olacak.
            if ((accountIds == null || !accountIds.Any()) && (personIds == null || !personIds.Any()))
            {
                return View(model);
            }

            // --- FİLTRE MANTIĞI ---
            // Eğer listede -1 varsa "Tümü" seçilmiş demektir, filtreleme YAPMA (false).
            // Yoksa ve liste doluysa filtrele (true).
            bool filterByAccount = accountIds != null && accountIds.Any() && !accountIds.Contains(-1);
            bool filterByPerson = personIds != null && personIds.Any() && !personIds.Contains(-1);

            // 1. GELİRLER
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

            // 2. GİDERLER
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

            return View(model);
        }

        // ---------------------------------------------------------
        // 2. GİDER TÜRÜ HAREKETLERİ (GÜNCEL)
        // ---------------------------------------------------------
        public async Task<IActionResult> ExpenseTypeMovements(DateTime? startDate, DateTime? endDate, List<int> typeIds, List<int> personIds)
        {
            var model = new ExpenseTypeReportViewModel
            {
                Items = new List<ExpenseReportItem>(),
                StartDate = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                EndDate = endDate ?? DateTime.Now,
                SelectedTypeIds = typeIds ?? new List<int>(),
                SelectedPersonIds = personIds ?? new List<int>()
            };

            ViewBag.ExpenseTypes = await _context.ExpenseTypes.Where(x => x.is_bank == false).OrderBy(x => x.expensetype_order).ToListAsync();
            ViewBag.Persons = await _context.Persons.Where(x => x.is_bank == false).OrderBy(x => x.person_id).ToListAsync();

            if ((typeIds == null || !typeIds.Any()) && (personIds == null || !personIds.Any())) return View(model);

            // FİLTRE MANTIĞI (-1 kontrolü)
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

            return View(model);
        }

        // ---------------------------------------------------------
        // 3. GELİR TÜRÜ HAREKETLERİ (GÜNCEL)
        // ---------------------------------------------------------
        public async Task<IActionResult> IncomeTypeMovements(DateTime? startDate, DateTime? endDate, List<int> typeIds, List<int> personIds)
        {
            var model = new IncomeTypeReportViewModel
            {
                Items = new List<IncomeReportItem>(),
                StartDate = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                EndDate = endDate ?? DateTime.Now,
                SelectedTypeIds = typeIds ?? new List<int>(),
                SelectedPersonIds = personIds ?? new List<int>()
            };

            ViewBag.IncomeTypes = await _context.IncomeTypes.Where(x => x.is_bank == false).OrderBy(x => x.incometype_order).ToListAsync();
            ViewBag.Persons = await _context.Persons.Where(x => x.is_bank == false).OrderBy(x => x.person_id).ToListAsync();

            if ((typeIds == null || !typeIds.Any()) && (personIds == null || !personIds.Any())) return View(model);

            // FİLTRE MANTIĞI (-1 kontrolü)
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

            return View(model);
        }

        // ---------------------------------------------------------
        // 4. BORÇ / ALACAK RAPORU (Ingredient Gruplu)
        // ---------------------------------------------------------
        public async Task<IActionResult> DebtReceivableReport(DateTime? startDate, DateTime? endDate, List<int> debtorIds, List<int> ingredientIds)
        {
            var model = new DebtReceivableViewModel
            {
                GroupedItems = new List<DebtGroupedItem>(),
                Details = new List<Movement>(),
                StartDate = startDate ?? new DateTime(DateTime.Now.Year, 1, 1), // Yıl başından
                EndDate = endDate ?? DateTime.Now,
                SelectedDebtorIds = debtorIds ?? new List<int>(),
                SelectedIngredientIds = ingredientIds ?? new List<int>()
            };

            // Dropdown Verilerini Çek
            ViewBag.Debtors = await _context.Debtors.OrderBy(x => x.debtor_name).ToListAsync();
            ViewBag.Ingredients = await _context.Ingredients.OrderBy(x => x.ingredient_name).ToListAsync();

            // Filtre seçimi yoksa boş dön (Tümü seçeneği -1 dahil)
            if ((debtorIds == null || !debtorIds.Any()) && (ingredientIds == null || !ingredientIds.Any()))
            {
                return View(model);
            }

            // --- FİLTRE MANTIĞI (-1 Tümü Kontrolü) ---
            bool filterByDebtor = debtorIds != null && debtorIds.Any() && !debtorIds.Contains(-1);
            bool filterByIngredient = ingredientIds != null && ingredientIds.Any() && !ingredientIds.Contains(-1);

            // 1. SORGUYU HAZIRLA
            var query = _context.Movements
                .Include(x => x.Debtor)
                .Include(x => x.Ingredient)
                .Include(x => x.Person)
                .Where(x => x.movement_date >= model.StartDate && x.movement_date <= model.EndDate);

            if (filterByDebtor) query = query.Where(x => debtorIds.Contains(x.debtor_id));
            if (filterByIngredient) query = query.Where(x => ingredientIds.Contains(x.ingredient_id));

            // Verileri Çek
            var movements = await query.OrderByDescending(x => x.movement_date).ToListAsync();
            model.Details = movements;

            // 2. INGREDIENT'A GÖRE GRUPLA VE TOPLA
            model.GroupedItems = movements
                .GroupBy(x => x.Ingredient.ingredient_name)
                .Select(g => new DebtGroupedItem
                {
                    IngredientName = g.Key,
                    TotalAmount = g.Sum(x => x.movement_amount),
                    Status = g.Sum(x => x.movement_amount) >= 0 ? "Alacak" : "Borç"
                })
                .ToList();

            // 3. GENEL TOPLAMLARI HESAPLA (Sadece Para Birimi TL olanları toplamak mantıklı olabilir ama burada genel matematiksel toplam alıyoruz)
            // Pozitifler Alacak, Negatifler Borç
            model.TotalReceivable = movements.Where(x => x.movement_amount > 0).Sum(x => x.movement_amount);
            model.TotalDebt = movements.Where(x => x.movement_amount < 0).Sum(x => x.movement_amount);
            model.NetBalance = model.TotalReceivable + model.TotalDebt; // Borç eksi olduğu için topluyoruz

            return View(model);
        }

        // ---------------------------------------------------------
        // 5. PORTFÖY / VARLIK RAPORU
        // ---------------------------------------------------------
        public async Task<IActionResult> PortfolioReport(DateTime? startDate, DateTime? endDate, List<int> ingredientIds, List<int> personIds)
        {
            var model = new PortfolioReportViewModel
            {
                GroupedItems = new List<PortfolioGroupedItem>(),
                Details = new List<Asset>(),
                StartDate = startDate ?? new DateTime(DateTime.Now.Year, 1, 1), // Yıl başı varsayılan
                EndDate = endDate ?? DateTime.Now,
                SelectedIngredientIds = ingredientIds ?? new List<int>(),
                SelectedPersonIds = personIds ?? new List<int>()
            };

            // Dropdown Verileri
            ViewBag.Ingredients = await _context.Ingredients.OrderBy(x => x.ingredient_name).ToListAsync();
            ViewBag.Persons = await _context.Persons.Where(x=> x.is_bank == false).OrderBy(x => x.person_id).ToListAsync();

            // Seçim yoksa boş dön (-1 Tümü seçeneği dahil)
            if ((ingredientIds == null || !ingredientIds.Any()) && (personIds == null || !personIds.Any()))
            {
                return View(model);
            }

            // --- FİLTRE MANTIĞI (-1 Kontrolü) ---
            bool filterByIngredient = ingredientIds != null && ingredientIds.Any() && !ingredientIds.Contains(-1);
            bool filterByPerson = personIds != null && personIds.Any() && !personIds.Contains(-1);

            // 1. SORGU
            var query = _context.Assets
                .Include(x => x.Ingredient)
                .Include(x => x.Person)
                .Where(x => x.asset_date >= model.StartDate && x.asset_date <= model.EndDate);

            if (filterByIngredient) query = query.Where(x => ingredientIds.Contains(x.ingredient_id));
            if (filterByPerson) query = query.Where(x => personIds.Contains(x.person_id));

            // Verileri Çek
            var assets = await query.OrderByDescending(x => x.asset_date).ToListAsync();
            model.Details = assets;

            // 2. GRUPLAMA (Ingredient'a göre topla)
            model.GroupedItems = assets
                .GroupBy(x => x.Ingredient.ingredient_name)
                .Select(g => new PortfolioGroupedItem
                {
                    IngredientName = g.Key,
                    TotalAmount = g.Sum(x => x.asset_amount)
                })
                .ToList();

            return View(model);
        }

        // ---------------------------------------------------------
        // 6. HESAP BAKİYELERİ RAPORU (Tarihli Bakiye Durumu)
        // ---------------------------------------------------------
        public async Task<IActionResult> AccountBalances(DateTime? filterDate)
        {
            // Tarih seçilmediyse bugünü, seçildiyse o günün son saniyesini al (23:59:59)
            var selectedDate = filterDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Now;

            var model = new AccountBalanceReportViewModel
            {
                FilterDate = selectedDate,
                Accounts = new List<AccountBalanceItem>()
            };

            // 1. Tüm Hesapları Çek
            var accounts = await _context.PaymentTypes.Where(x=> x.is_bank == false).ToListAsync();

            // 2. O Tarihe Kadar Olan Tüm Gelir ve Giderleri Çek (Performans için tek sorguda çekip RAM'de işliyoruz)
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

            // 3. Hesaplama Döngüsü
            foreach (var acc in accounts)
            {
                // Gelir Toplamı (Varsa al, yoksa 0)
                decimal totalInc = allIncomes.FirstOrDefault(x => x.AccId == acc.paymenttype_id)?.Total ?? 0;

                // Gider Toplamı (Varsa al, yoksa 0)
                decimal totalExp = allExpenses.FirstOrDefault(x => x.AccId == acc.paymenttype_id)?.Total ?? 0;

                // Bakiye = Gelir - Gider
                decimal calculatedBalance = totalInc - totalExp;

                model.Accounts.Add(new AccountBalanceItem
                {
                    AccountName = acc.paymenttype_name,
                    IsBank = acc.is_bank,
                    IsCreditCard = acc.is_creditcard,
                    Balance = calculatedBalance
                });
            }

            // 4. Genel Toplamlar
            // Varlıklar: Kredi kartı olmayan ve bakiyesi artı olanlar
            model.TotalAssets = model.Accounts.Where(x => !x.IsCreditCard).Sum(x => x.Balance);

            // Borçlar: Kredi kartları (Genelde eksi olur) veya eksiye düşmüş hesaplar
            model.TotalLiabilities = model.Accounts.Where(x => x.IsCreditCard).Sum(x => x.Balance);

            model.NetWorth = model.TotalAssets + model.TotalLiabilities; // Borç eksi olduğu için topluyoruz

            return View(model);
        }


        // ---------------------------------------------------------
        // 7. GENEL GELİR / GİDER RAPORU (ÖZET VE DAĞILIM)
        // ---------------------------------------------------------
        // ---------------------------------------------------------
        // 7. GENEL GELİR / GİDER RAPORU (ÖZET, DAĞILIM VE DETAY)
        // ---------------------------------------------------------
        // ---------------------------------------------------------
        // 7. GENEL GELİR / GİDER RAPORU (TAM VERSİYON)
        // ---------------------------------------------------------
        public async Task<IActionResult> IncomeExpenseReport(DateTime? startDate, DateTime? endDate, List<int> personIds)
        {
            // 1. Model Başlangıç Ayarları
            var model = new IncomeExpenseReportViewModel
            {
                StartDate = startDate ?? new DateTime(DateTime.Now.Year, 1, 1), // Yıl başından
                EndDate = endDate ?? DateTime.Now,
                SelectedPersonIds = personIds ?? new List<int>(),

                IncomeCategories = new List<CategorySummary>(),
                ExpenseCategories = new List<CategorySummary>(),
                Details = new List<ReportMovementItem>(),
                Timeline = new List<TimelineSummary>()
            };

            // Dropdown İçin Kişileri Doldur
            ViewBag.Persons = await _context.Persons.Where(x=> x.is_bank == false).OrderBy(x => x.person_id).ToListAsync();

            // 2. Filtreleme Mantığı ("Tümü" yani -1 seçildiyse filtreleme yapma)
            bool filterByPerson = personIds != null && personIds.Any() && !personIds.Contains(-1);

            // ========================================================================
            // BÖLÜM A: GELİRLERİN ÇEKİLMESİ VE İŞLENMESİ
            // ========================================================================
            var incomeQuery = _context.Incomes
                .Include(x => x.IncomeType)
                .Include(x => x.PaymentType)
                .Include(x => x.Person)
                .Where(x => x.income_date >= model.StartDate && x.income_date <= model.EndDate && x.is_bankmovement == false);

            if (filterByPerson)
            {
                incomeQuery = incomeQuery.Where(x => personIds.Contains(x.person_id));
            }

            var incomes = await incomeQuery.ToListAsync();
            model.TotalIncome = incomes.Sum(x => x.income_amount);

            // A-1. Kategori Bazlı Gruplama (Progress Bar İçin)
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

            // A-2. Detay Listesi Mapping
            var incomeDetails = incomes.Select(x => new ReportMovementItem
            {
                Date = x.income_date,
                CategoryName = x.IncomeType?.incometype_name ?? "Diğer",
                AccountName = x.PaymentType?.paymenttype_name ?? "-",
                PersonName = x.Person?.person_name ?? "-",
                Description = x.income_description,
                Amount = x.income_amount,
                IsExpense = false // Gelir (Yeşil)
            });

            // ========================================================================
            // BÖLÜM B: GİDERLERİN ÇEKİLMESİ VE İŞLENMESİ
            // ========================================================================
            var expenseQuery = _context.Expenses
                .Include(x => x.ExpenseType)
                .Include(x => x.PaymentType)
                .Include(x => x.Person)
                .Where(x => x.expense_date >= model.StartDate && x.expense_date <= model.EndDate && x.is_bankmovement == false);

            if (filterByPerson)
            {
                expenseQuery = expenseQuery.Where(x => personIds.Contains(x.person_id));
            }

            var expenses = await expenseQuery.ToListAsync();
            model.TotalExpense = expenses.Sum(x => x.expense_amount);

            // B-1. Kategori Bazlı Gruplama (Progress Bar İçin)
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

            // B-2. Detay Listesi Mapping
            var expenseDetails = expenses.Select(x => new ReportMovementItem
            {
                Date = x.expense_date,
                CategoryName = x.ExpenseType?.expensetype_name ?? "Diğer",
                AccountName = x.PaymentType?.paymenttype_name ?? "-",
                PersonName = x.Person?.person_name ?? "-",
                Description = x.expense_description,
                Amount = x.expense_amount,
                IsExpense = true // Gider (Kırmızı)
            });

            // ========================================================================
            // BÖLÜM C: BİRLEŞTİRME VE SONUÇ
            // ========================================================================

            // Listeleri Birleştir ve Sırala
            model.Details.AddRange(incomeDetails);
            model.Details.AddRange(expenseDetails);
            model.Details = model.Details.OrderByDescending(x => x.Date).ToList();

            // Net Kar/Zarar
            model.NetResult = model.TotalIncome - model.TotalExpense;

            // ========================================================================
            // BÖLÜM D: TARİH BAZLI GRAFİK VERİSİ (TREND ANALİZİ)
            // ========================================================================

            // Günlük Gruplamalar
            var incomeByDate = incomes
                .GroupBy(x => x.income_date.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.income_amount) })
                .ToList();

            var expenseByDate = expenses
                .GroupBy(x => x.expense_date.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.expense_amount) })
                .ToList();

            // Tüm tarihleri birleştir (Union) ve sırala
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

            return View(model);
        }
    }
}