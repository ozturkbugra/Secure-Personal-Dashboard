namespace BugraLife.Models
{
    public class IncomeTypeReportViewModel
    {
        // Filtreler
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<int> SelectedTypeIds { get; set; } // Çoklu seçim ID'leri

        public List<int> SelectedPersonIds { get; set; } // Kişi Filtresi

        // Sonuçlar
        public decimal TotalAmount { get; set; } // Toplam Gelir
        public List<IncomeReportItem> Items { get; set; } // Satırlar
    }

    public class IncomeReportItem
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string CategoryName { get; set; } // Gelir Türü Adı
        public string AccountName { get; set; }  // Hangi Hesaba Yattı?
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}