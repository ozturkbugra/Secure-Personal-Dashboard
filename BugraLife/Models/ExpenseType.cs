using System.ComponentModel.DataAnnotations;

namespace BugraLife.Models
{
    public class ExpenseType
    {
        [Key]
        public int expensetype_id { get; set; }
        public string expensetype_name { get; set; }
        public string expensetype_order { get; set; }
        public bool is_bank { get; set; }
        public bool is_home { get; set; }
        public bool is_commission { get; set; } = false;
        public string description { get; set; }

    }
}
