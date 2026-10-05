using System.ComponentModel.DataAnnotations;

namespace BugraLife.Models
{
    // Alışveriş listesi / sepet (ör. "Haftalık Market", "BİM"). Fitness günleri gibi pill sekme.
    public class ShoppingList
    {
        [Key]
        public int shoppinglist_id { get; set; }

        [Required]
        public string shoppinglist_name { get; set; }

        public int shoppinglist_order { get; set; }

        public DateTime? shoppinglist_date { get; set; } // planlanan alışveriş tarihi (opsiyonel)

        public bool shoppinglist_active { get; set; } = true;

        public DateTime shoppinglist_created { get; set; } = DateTime.Now;

        public virtual ICollection<ShoppingItem> Items { get; set; } = new List<ShoppingItem>();
    }
}
