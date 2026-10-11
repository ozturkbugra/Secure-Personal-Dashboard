namespace BugraLife.Models
{
    public class InstallmentReportViewModel
    {
        public bool IsHistory { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalCount { get; set; }
        public int DistinctMonthCount { get; set; }
        public List<InstallmentMonthGroup> Months { get; set; } = new();
    }

    public class InstallmentMonthGroup
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty; // Örn: "Kasım 2026"
        public decimal MonthTotal { get; set; }
        public int InstallmentCount { get; set; }
        public List<InstallmentAccountGroup> Accounts { get; set; } = new();
    }

    public class InstallmentAccountGroup
    {
        public int PaymentTypeId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public bool IsCreditCard { get; set; }
        public decimal AccountTotal { get; set; }
        public int InstallmentCount { get; set; }
        public List<InstallmentItemDetail> Items { get; set; } = new();
    }

    public class InstallmentItemDetail
    {
        public int ExpenseId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ExpenseTypeName { get; set; } = string.Empty;
        public string PersonName { get; set; } = string.Empty;
        public string InstallmentInfo { get; set; } = string.Empty; // Örn: "3/6 Taksit"
    }
}
