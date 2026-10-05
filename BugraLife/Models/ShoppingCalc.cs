using System.Globalization;

namespace BugraLife.Models
{
    // Birim fiyat hesabı — controller, VM ve view aynı mantığı kullansın diye tek yerde.
    public static class ShoppingCalc
    {
        public static readonly CultureInfo Tr = new("tr-TR");

        public static string NormUnit(string? unit) => (unit ?? "").Trim().ToLower(Tr);

        // Birim fiyat (toplam ÷ miktar). Miktar yoksa toplam fiyatın kendisi (adet başı gibi).
        public static decimal? UnitPrice(decimal? price, decimal? amount)
        {
            if (!price.HasValue) return null;
            if (amount.HasValue && amount.Value > 0) return Math.Round(price.Value / amount.Value, 2);
            return price.Value;
        }

        // Geçmiş eşleştirme anahtarı: ürün adı + birim (aynı birimde karşılaştırma).
        public static string Key(string name, string? unit) => name.Trim().ToLower(Tr) + "|" + NormUnit(unit);

        // "25,00 ₺/lt" veya miktar yoksa "25,00 ₺"
        public static string UnitPriceText(decimal unitPrice, string? unit)
        {
            var u = NormUnit(unit);
            return unitPrice.ToString("N2", Tr) + " ₺" + (string.IsNullOrEmpty(u) ? "" : "/" + u);
        }
    }
}
