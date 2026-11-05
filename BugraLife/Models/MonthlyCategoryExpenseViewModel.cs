namespace BugraLife.Models
{
    public class MonthlyCategoryExpenseViewModel
    {
        // Filtreler
        public int? SelectedYear { get; set; } // null veya 0 => "Tüm Yıllar"
        public List<int> AvailableYears { get; set; } = new();
        public List<int> SelectedPersonIds { get; set; } = new();
        public List<int> SelectedTypeIds { get; set; } = new();
        public int ActiveMonth { get; set; } = DateTime.Now.Month; // 1-12

        // Genel İstatistikler (KPI)
        public decimal TotalExpense { get; set; }
        public decimal MonthlyAverage { get; set; }
        public string TopCategoryName { get; set; } = "-";
        public decimal TopCategoryAmount { get; set; }
        public string TopMonthName { get; set; } = "-";
        public decimal TopMonthAmount { get; set; }

        // Matris Tablosu
        public List<MonthlyCategoryRow> CategoryRows { get; set; } = new();
        public decimal[] MonthlyTotals { get; set; } = new decimal[12];
        public int[] MonthlyCounts { get; set; } = new int[12];

        // Seçilen Ayın Kırılımı (Drill-down)
        public string ActiveMonthName { get; set; } = "";
        public decimal ActiveMonthTotal { get; set; }
        public List<CategoryBreakdownItem> ActiveMonthCategories { get; set; } = new();
        public List<ExpenseDetailItem> ActiveMonthExpenses { get; set; } = new();

        // Tüm ayların harcama verisi (İstemcide anlık geçiş için)
        public List<ExpenseDetailItem> AllExpenses { get; set; } = new();
    }

    public class MonthlyCategoryRow
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public decimal[] MonthlyAmounts { get; set; } = new decimal[12]; // 0: Ocak ... 11: Aralık
        public decimal TotalAmount { get; set; }
        public double Percentage { get; set; }
    }

    public class CategoryBreakdownItem
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
        public int TransactionCount { get; set; }
    }

    public class ExpenseDetailItem
    {
        public int ExpenseId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public int Month { get; set; } // 1-12
        public int Year { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string PersonName { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
    }
}
