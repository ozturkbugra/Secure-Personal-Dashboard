namespace BugraLife.Models
{
    public class ExpenseTypeReportViewModel
    {
        // Filtreler
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<int> SelectedTypeIds { get; set; } // Çoklu seçim ID'leri

        public List<int> SelectedPersonIds { get; set; } // Kişi Filtresi
        // Sonuçlar
        public decimal TotalAmount { get; set; } // Toplam Harcama
        public List<ExpenseReportItem> Items { get; set; } // Satırlar
    }

    public class ExpenseReportItem
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string CategoryName { get; set; } // Gider Türü Adı
        public string AccountName { get; set; }  // Hangi Hesaptan Ödendi?
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}