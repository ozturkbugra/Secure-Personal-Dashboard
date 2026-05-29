namespace BugraLife.Models
{

    public class CategorySummary
    {
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public double Percentage { get; set; } // Yüzdelik dilim (Bar için)
    }



    // Hareket Detayı İçin Yardımcı Sınıf
    public class ReportMovementItem
    {
        public DateTime Date { get; set; }
        public string CategoryName { get; set; } // Gelir/Gider Türü
        public string AccountName { get; set; }  // Hangi Hesap
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public bool IsExpense { get; set; } // Renklendirme için (True=Gider, False=Gelir)
        public string PersonName { get; set; } // İşlemi Yapan
    }



    public class IncomeExpenseReportViewModel
    {
        // ... (Eski özellikler aynen kalıyor) ...
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<int> SelectedPersonIds { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetResult { get; set; }
        public List<CategorySummary> IncomeCategories { get; set; }
        public List<CategorySummary> ExpenseCategories { get; set; }
        public List<ReportMovementItem> Details { get; set; }

        // --- YENİ: TARİH BAZLI GRAFİK VERİSİ ---
        public List<TimelineSummary> Timeline { get; set; }
    }

    public class TimelineSummary
    {
        public string DateLabel { get; set; } // Örn: "01.01.2024"
        public decimal DailyIncome { get; set; }
        public decimal DailyExpense { get; set; }
        public decimal DailyNet { get; set; } // O günkü kar/zarar
    }
}

