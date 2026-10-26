using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Tổng tiền chỉ tính các sản phẩm còn hàng
        /// </summary>
        public decimal AvailableCartTotal => Items != null ? Items.Where(i => i.IsAvailable).Sum(i => i.ItemTotal) : 0m;

        /// <summary>
        /// Số lượng các sản phẩm còn hàng
        /// </summary>
        public int AvailableItemCount => Items != null ? Items.Where(i => i.IsAvailable).Sum(i => i.Quantity) : 0;

        /// <summary>
        /// Kiểm tra xem trong giỏ có sản phẩm nào hết hàng không
        /// </summary>
        public bool HasOutOfStockItems => Items != null && Items.Any(i => i.IsOutOfStock);

        /// <summary>
        /// Số lượng sản phẩm bị hết hàng
        /// </summary>
        public int OutOfStockCount => Items != null ? Items.Count(i => i.IsOutOfStock) : 0;

        /// <summary>
        /// Có ít nhất một sản phẩm khả dụng để tiến hành đặt hàng
        /// </summary>
        public bool CanCheckout => Items != null && Items.Any(i => i.IsAvailable);
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

        /// <summary>
        /// Trạng thái sản phẩm: 1 = Active/Đang bán, 2 = Hết hàng, 3 = Ngừng bán
        /// </summary>
        public int Status { get; set; } = 1;

        /// <summary>
        /// True nếu sản phẩm hết hàng hoặc ngừng kinh doanh
        /// </summary>
        public bool IsOutOfStock => StockQuantity <= 0 || Status != 1;

        /// <summary>
        /// True nếu sản phẩm còn hàng và có thể đặt
        /// </summary>
        public bool IsAvailable => !IsOutOfStock;
    }
}

