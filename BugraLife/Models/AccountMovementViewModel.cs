namespace BugraLife.Models
{
    public class AccountMovementViewModel
    {
        // Filtreleme için kullanılacaklar
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<int> SelectedAccountIds { get; set; } // Çoklu seçim ID'leri

        // Rapor Sonuçları
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance { get; set; } // Gelir - Gider
        public List<int> SelectedPersonIds { get; set; } // Kişi Filtresi
        // Hareket Listesi
        public List<MovementItem> Movements { get; set; }
    }

    public class MovementItem
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string AccountName { get; set; } // Hangi hesap?
        public string Description { get; set; } // Açıklama
        public decimal Amount { get; set; }
        public string Type { get; set; } // "Gelir" veya "Gider"
        public bool IsExpense { get; set; } // Renklendirme için (True ise Kırmızı, False ise Yeşil)
    }
}
