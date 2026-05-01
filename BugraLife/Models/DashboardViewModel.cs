namespace BugraLife.Models
{
    public class DashboardViewModel
    {
        // Diğer dashboard verileri (Toplam bakiye vs.) buraya eklenebilir.
        // Biz şimdilik sabit gider listesine odaklanıyoruz.
        public List<FixedExpenseStatus> FixedExpenseStatuses { get; set; }

        public List<AccountStatus> Accounts { get; set; }

        public List<PlannedToDo> PendingToDos { get; set; }

        public List<DebtorBalanceViewModel> DebtorBalances { get; set; }
    }

    public class FixedExpenseStatus
    {
        public string ExpenseName { get; set; } // Örn: Kira
        public int PaymentDay { get; set; }     // Örn: 1
        public decimal EstimatedAmount { get; set; } // Tutar varsa
        public bool IsPaid { get; set; }        // Ödendi mi?
        public int DaysDiff { get; set; }       // Pozitif: Kaldı, Negatif: Geçti
        public DateTime DueDate { get; set; }   // Son Ödeme Tarihi
    }

    public class AccountStatus
    {
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public string Type { get; set; } // "Kasa", "Banka", "Kredi Kartı"
        public bool IsCreditCard { get; set; }
    }

    public class DebtorBalanceViewModel
    {
        public string Name { get; set; }           // Kişi Adı
        public string IngredientName { get; set; } // TL, Çeyrek Altın, Dolar vb.
        public decimal Balance { get; set; }       // Net miktar
    }
}