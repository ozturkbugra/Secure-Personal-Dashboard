namespace BugraLife.Models
{
    public class PortfolioReportViewModel
    {
        // Filtreler
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<int> SelectedIngredientIds { get; set; }
        public List<int> SelectedPersonIds { get; set; }

        // �ZET L�STES� (Varl�k T�r�ne G�re Toplamlar)
        // �rn: { Alt�n, 50 Gram }, { Dolar, 1000 $ }
        public List<PortfolioGroupedItem> GroupedItems { get; set; }

        // DETAY L�STES� (Tek tek i�lemler)
        public List<Asset> Details { get; set; }
    }

    public class PortfolioGroupedItem
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; }
        public decimal TotalAmount { get; set; }
    }
}