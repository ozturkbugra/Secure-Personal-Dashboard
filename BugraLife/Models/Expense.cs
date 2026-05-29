using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BugraLife.Models
{
    public class Expense
    {
        [Key]
        public int expense_id { get; set; }
        public int expensetype_id { get; set; }
        
        [ForeignKey("expensetype_id")]
        public virtual ExpenseType? ExpenseType { get; set; }

        public int paymenttype_id { get; set; }

        [ForeignKey("paymenttype_id")]
        public virtual PaymentType? PaymentType { get; set; }

        public int person_id { get; set; }
        [ForeignKey("person_id")]
        public virtual Person? Person { get; set; }

        public decimal expense_amount { get; set; }
        public DateTime expense_date { get; set; }
        public string expense_description { get; set; }
        public bool is_bankmovement { get; set; }

        

    }
}
