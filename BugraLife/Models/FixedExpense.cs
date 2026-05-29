using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BugraLife.Models
{
    public class FixedExpense
    {
        [Key]
        public int fixedexpense_id { get; set; }

        // Ad alanını sildik, ExpenseType üzerinden alacağız.

        public int expensetype_id { get; set; }

        [ForeignKey("expensetype_id")]
        public virtual ExpenseType? ExpenseType { get; set; }

        // Ödeme Günü (Ayın kaçı? 1-31 arası)
        public int payment_day { get; set; }

        // Yılda kaç kez ödeniyor? (12 = Aylık)
        public int frequency_count { get; set; }

        // Tahmini tutar silindi.

        public bool is_active { get; set; } = true;
    }
}