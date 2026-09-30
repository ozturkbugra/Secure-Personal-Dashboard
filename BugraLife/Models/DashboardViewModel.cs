namespace BugraLife.Models
{
    public class DashboardViewModel
    {
        // Diğer dashboard verileri (Toplam bakiye vs.) buraya eklenebilir.
        // Biz şimdilik sabit gider listesine odaklanıyoruz.
        public List<FixedExpenseStatus> FixedExpenseStatuses { get; set; }

        public List<AccountStatus> Accounts { get; set; }

        public List<PlannedToDo> PendingToDos { get; set; }

        public List<DebtorBalanceViewModel> DebtorBalances { get; set; } // Borç/Alacak
        public List<PortfolioGroupedItem> PortfolioBalances { get; set; }

        // --- Genel bakiye özeti ---
        public decimal TotalCash { get; set; }           // Nakit + banka hesapları toplamı (kredi kartı hariç)
        public decimal TotalCreditCardDebt { get; set; } // Kredi kartı borçları toplamı (pozitif gösterilir)
        public decimal NetBalance { get; set; }          // Net = tüm hesapların (kart dahil) toplamı
    }

    public class FixedExpenseStatus
    {
        public string ExpenseName { get; set; } // Örn: Kira
        public int PaymentDay { get; set; }     // Örn: 1
        public decimal EstimatedAmount { get; set; } // Tutar varsa
        public bool IsPaid { get; set; }        // Ödendi mi?
        public int DaysDiff { get; set; }       // Pozitif: Kaldı, Negatif: Geçti
        public DateTime DueDate { get; set; }   // Son Ödeme Tarihi
        public string? PaymentAccountName { get; set; } // Hangi hesaptan ödeniyor (opsiyonel)
        public bool PaymentIsCreditCard { get; set; }   // Ödeme hesabı kredi kartı mı?
    }

    public class AccountStatus
    {
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public string Type { get; set; } // "Kasa", "Banka", "Kredi Kartı"
        public bool IsCreditCard { get; set; }
        public int? StatementDay { get; set; } // Kredi kartı hesap kesim günü (ayın günü)
    }

    public class DebtorBalanceViewModel
    {
        public string Name { get; set; }           // Cari Adı (Sümeyye)
        public string IngredientName { get; set; } // Varlık Türü (Çeyrek Altın)
        public decimal Balance { get; set; }       // Miktar (-3 veya 1000)
    }


}