using System.Collections.Generic;

namespace WebEcommerce.Models
{
    /// <summary>
    /// ViewModel for displaying the cart page.
    /// Built from cookie data + product info from the database.
    /// </summary>
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal CartTotal { get; set; }
    }

    /// <summary>
    /// ViewModel for a single item in the cart.
    /// Contains product details fetched from DB + quantity from cookie.
    /// </summary>
    public class CartItemViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ImageURL { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal ItemTotal { get; set; }
        public int StockQuantity { get; set; }
    }
}
