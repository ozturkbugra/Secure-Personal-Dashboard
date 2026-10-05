using System.Globalization;

namespace BugraLife.Views
{
    // _CartRow partial'ı için küçük görünüm modeli: ürün + son fiyat + kültür.
    public class CartRowVm
    {
        public ShoppingItemRef Item { get; }
        public decimal? LastPrice { get; }
        public CultureInfo Tr { get; }

        public CartRowVm(BugraLife.Models.ShoppingItem item, decimal? lastPrice, CultureInfo tr)
        {
            Item = new ShoppingItemRef(item);
            LastPrice = lastPrice;
            Tr = tr;
        }
    }

    // ShoppingItem'a doğrudan bağımlılığı sadeleştiren ince sarmalayıcı.
    public class ShoppingItemRef
    {
        public int Id { get; }
        public string Name { get; }
        public decimal? Amount { get; }
        public string? Unit { get; }
        public string? Description { get; }
        public decimal? Price { get; }
        public bool IsBought { get; }
        public decimal? UnitPrice { get; }

        public ShoppingItemRef(BugraLife.Models.ShoppingItem i)
        {
            Id = i.shoppingitem_id;
            Name = i.shoppingitem_name;
            Amount = i.shoppingitem_amount;
            Unit = i.shoppingitem_unit;
            Description = i.shoppingitem_description;
            Price = i.shoppingitem_price;
            IsBought = i.shoppingitem_isbought;
            UnitPrice = BugraLife.Models.ShoppingCalc.UnitPrice(i.shoppingitem_price, i.shoppingitem_amount);
        }
    }
}
