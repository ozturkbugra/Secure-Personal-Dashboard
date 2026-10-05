using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BugraLife.Models
{
    // Ürün adı bazında fiyat geçmişi. Liste/ürün silinse de kalır → "geçen sefer X ₺" karşılaştırması.
    public class ShoppingPriceHistory
    {
        [Key]
        public int shoppingpricehistory_id { get; set; }

        [Required]
        public string product_name { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal price { get; set; } // ödenen toplam fiyat

        public string? unit { get; set; } // birim (ör. "lt") — karşılaştırma aynı birimde yapılır

        [Column(TypeName = "decimal(18,2)")]
        public decimal? unit_price { get; set; } // birim fiyat (toplam ÷ miktar); miktar yoksa toplam

        public DateTime date { get; set; } = DateTime.Now;
    }
}
