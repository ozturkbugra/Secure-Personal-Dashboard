namespace BugraLife.Models
{
    public class AccountBalanceReportViewModel
    {
        public DateTime FilterDate { get; set; } // Hangi tarihteki bakiye?

        // Sonuçlar
        public decimal TotalAssets { get; set; }      // Toplam Varlık (Nakit + Banka)
        public decimal TotalLiabilities { get; set; } // Toplam Borç (Kredi Kartları)
        public decimal NetWorth { get; set; }         // Net Durum

        public List<AccountBalanceItem> Accounts { get; set; }
    }

    public class AccountBalanceItem
    {
        public string AccountName { get; set; }
        public bool IsCreditCard { get; set; }
        public bool IsBank { get; set; }
        public decimal Balance { get; set; } // O tarihteki hesaplanmış bakiye
    }
}