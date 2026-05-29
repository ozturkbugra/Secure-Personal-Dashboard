namespace BugraLife.Models
{
    public class DebtReceivableViewModel
    {
        // Filtreler
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<int> SelectedDebtorIds { get; set; }
        public List<int> SelectedIngredientIds { get; set; }

        // Özet Kartlar İçin Genel Toplamlar
        public decimal TotalReceivable { get; set; } // Toplam Alacak (+)
        public decimal TotalDebt { get; set; }       // Toplam Borç (-)
        public decimal NetBalance { get; set; }      // Net Durum

        // GRUPLANMIŞ LİSTE (İstediğin Özellik: Ingredient'a göre toplam)
        public List<DebtGroupedItem> GroupedItems { get; set; }

        // Detaylı Hareket Listesi (İstersen aşağıda döküm olarak göstermek için)
        public List<Movement> Details { get; set; }
    }

    public class DebtGroupedItem
    {
        public string IngredientName { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } // "Alacak" veya "Borç"
    }
}