using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BugraLife.Models
{
    // Listedeki tek bir ürün. Fiyat opsiyonel; alındığında fiyat geçmişine yazılır.
    public class ShoppingItem
    {
        [Key]
        public int shoppingitem_id { get; set; }

        [ForeignKey("List")]
        public int shoppinglist_id { get; set; }
        public virtual ShoppingList? List { get; set; }

        [Required]
        public string shoppingitem_name { get; set; }

        public string? shoppingitem_quantity { get; set; } // ESKİ/serbest metin (legacy) — artık amount+unit kullanılıyor

        [Column(TypeName = "decimal(18,3)")]
        public decimal? shoppingitem_amount { get; set; } // miktar (ör. 2) — birim fiyat için

        public string? shoppingitem_unit { get; set; } // birim (ör. "lt", "kg", "adet")

        public string? shoppingitem_description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? shoppingitem_price { get; set; } // toplam fiyat, opsiyonel

        public bool shoppingitem_isbought { get; set; }

        public bool shoppingitem_historylogged { get; set; } // fiyat geçmişine yazıldı mı (tekrar yazmamak için)

        public int shoppingitem_order { get; set; }

        public DateTime shoppingitem_created { get; set; } = DateTime.Now;
    }
}
