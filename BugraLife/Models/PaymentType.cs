using System.ComponentModel.DataAnnotations;

namespace BugraLife.Models
{
    public class PaymentType
    {
        [Key]
        public int paymenttype_id { get; set; }
        public string? paymenttype_name { get; set; }
        public int paymenttype_order { get; set; }

        public decimal paymenttype_balance { get; set; }
        public bool is_bank { get; set; }
        public bool is_creditcard { get; set; } = false;


    }
}
