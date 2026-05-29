namespace BugraLife.Models
{
    public class PortfolioReportViewModel
    {
        // Filtreler
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<int> SelectedIngredientIds { get; set; }
        public List<int> SelectedPersonIds { get; set; }

        // ÖZET LİSTESİ (Varlık Türüne Göre Toplamlar)
        // Örn: { Altın, 50 Gram }, { Dolar, 1000 $ }
        public List<PortfolioGroupedItem> GroupedItems { get; set; }

        // DETAY LİSTESİ (Tek tek işlemler)
        public List<Asset> Details { get; set; }
    }

    public class PortfolioGroupedItem
    {
        public string IngredientName { get; set; }
        public decimal TotalAmount { get; set; }
    }
}