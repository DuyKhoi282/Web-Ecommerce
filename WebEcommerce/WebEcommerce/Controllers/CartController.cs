using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: /Cart
        public async Task<ActionResult> Index()
        {
            try
            {
                var userId = User.Identity.GetUserId();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems.Select(ci => ci.Product))
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (cart == null)
                {
                    return View((object)null);
                }

                return View(cart);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Cart/Index] {ex}"
                );

                TempData["ErrorMessage"] =
                    "Đã xảy ra lỗi khi tải giỏ hàng.";

                return View((object)null);
            }
        }

        [ChildActionOnly]
        public ActionResult CartCount()
        {
            if (!Request.IsAuthenticated) return Content("0");
            
            var userId = User.Identity.GetUserId();
            var count = _context.Carts
                .Where(c => c.UserID == userId)
                .SelectMany(c => c.CartItems)
                .Sum(ci => (int?)ci.Quantity) ?? 0;
                
            return Content(count.ToString());
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AddToCart(
            int productId,
            int quantity = 1)
        {
            try
            {
                // ==========================================
                // 1. Validate User
                // ==========================================
                var userId = User.Identity.GetUserId();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại."
                    });
                }

                // ==========================================
                // 2. Validate Product ID
                // ==========================================
                if (productId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm không hợp lệ."
                    });
                }

                // ==========================================
                // 3. Validate Quantity
                // ==========================================
                if (quantity <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Số lượng sản phẩm phải lớn hơn 0."
                    });
                }

                // ==========================================
                // 4. Find Product
                // ==========================================
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductID == productId);

                if (product == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy sản phẩm."
                    });
                }

                // ==========================================
                // 5. Validate Product Status
                // ==========================================
                if (product.Status != 1)
                {
                    string message;

                    if (product.Status == 2)
                    {
                        message = "Sản phẩm hiện đã hết hàng.";
                    }
                    else if (product.Status == 3)
                    {
                        message = "Sản phẩm đã ngừng kinh doanh.";
                    }
                    else
                    {
                        message = "Sản phẩm hiện không thể mua.";
                    }

                    return Json(new
                    {
                        success = false,
                        message = message
                    });
                }

                // ==========================================
                // 6. Validate Stock
                // ==========================================
                if (product.StockQuantity <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm hiện đã hết hàng."
                    });
                }

                if (quantity > product.StockQuantity)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            $"Chỉ còn {product.StockQuantity} sản phẩm trong kho."
                    });
                }

                // ==========================================
                // 7. Find Cart of Current User
                // ==========================================
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                // ==========================================
                // 8. Create Cart if not exists
                // ==========================================
                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserID = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Carts.Add(cart);

                    await _context.SaveChangesAsync();
                }

                // ==========================================
                // 9. Find Existing CartItem
                // ==========================================
                var cartItem = cart.CartItems
                    .FirstOrDefault(ci => ci.ProductID == productId);

                // ==========================================
                // 10. Add New CartItem
                // ==========================================
                if (cartItem == null)
                {
                    cartItem = new CartItem
                    {
                        CartID = cart.CartID,
                        ProductID = product.ProductID,
                        Quantity = quantity,
                        UnitPriceAtAddition =
                            GetCurrentProductPrice(product)
                    };

                    _context.CartItems.Add(cartItem);
                }
                else
                {
                    // ======================================
                    // 11. Calculate New Quantity
                    // ======================================
                    var newQuantity = cartItem.Quantity + quantity;

                    // ======================================
                    // 12. Re-check Stock
                    // ======================================
                    if (newQuantity > product.StockQuantity)
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                $"Trong giỏ đã có {cartItem.Quantity} sản phẩm. " +
                                $"Kho chỉ còn {product.StockQuantity} sản phẩm."
                        });
                    }

                    cartItem.Quantity = newQuantity;

                    // Keep the current product price
                    cartItem.UnitPriceAtAddition =
                        GetCurrentProductPrice(product);
                }

                // ==========================================
                // 13. Update Cart Timestamp
                // ==========================================
                cart.UpdatedAt = DateTime.UtcNow;

                // ==========================================
                // 14. Save Database
                // ==========================================
                await _context.SaveChangesAsync();

                // ==========================================
                // 15. Calculate Cart Item Count
                // ==========================================
                var cartItemCount = await _context.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .SumAsync(ci => (int?)ci.Quantity) ?? 0;

                return Json(new
                {
                    success = true,
                    message = "Đã thêm sản phẩm vào giỏ hàng.",
                    cartItemCount = cartItemCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Cart/AddToCart] {ex}"
                );

                return Json(new
                {
                    success = false,
                    message =
                        "Đã xảy ra lỗi khi thêm sản phẩm vào giỏ hàng. Vui lòng thử lại."
                });
            }
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UpdateQuantity(int cartItemId, int newQuantity)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Json(new { success = false, message = "Vui lòng đăng nhập." });

                if (newQuantity <= 0)
                    return Json(new { success = false, message = "Số lượng không hợp lệ." });

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Include(ci => ci.Cart)
                    .FirstOrDefaultAsync(ci => ci.CartItemID == cartItemId && ci.Cart.UserID == userId);

                if (cartItem == null)
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ." });

                if (newQuantity > cartItem.Product.StockQuantity)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Chỉ còn {cartItem.Product.StockQuantity} sản phẩm trong kho.",
                        currentQuantity = cartItem.Quantity 
                    });
                }

                cartItem.Quantity = newQuantity;
                cartItem.Cart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var cartItems = await _context.CartItems.Where(ci => ci.CartID == cartItem.CartID).ToListAsync();
                var cartTotal = cartItems.Sum(ci => ci.UnitPriceAtAddition * ci.Quantity);
                var itemTotal = cartItem.UnitPriceAtAddition * cartItem.Quantity;
                var cartItemCount = cartItems.Sum(ci => ci.Quantity);

                return Json(new {
                    success = true,
                    itemTotal = itemTotal,
                    cartTotal = cartTotal,
                    cartItemCount = cartItemCount,
                    quantity = newQuantity
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Cart/UpdateQuantity] {ex}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi cập nhật số lượng." });
            }
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RemoveItem(int cartItemId)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Json(new { success = false, message = "Vui lòng đăng nhập." });

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .FirstOrDefaultAsync(ci => ci.CartItemID == cartItemId && ci.Cart.UserID == userId);

                if (cartItem == null)
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

                var cartId = cartItem.CartID;
                if (cartItem.Cart != null) 
                {
                    cartItem.Cart.UpdatedAt = DateTime.UtcNow;
                }
                _context.CartItems.Remove(cartItem);

                await _context.SaveChangesAsync();

                var cartItems = await _context.CartItems.Where(ci => ci.CartID == cartId).ToListAsync();
                var cartTotal = cartItems.Sum(ci => ci.UnitPriceAtAddition * ci.Quantity);
                var cartItemCount = cartItems.Sum(ci => ci.Quantity);

                return Json(new {
                    success = true,
                    cartTotal = cartTotal,
                    cartItemCount = cartItemCount,
                    isEmpty = !cartItems.Any()
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Cart/RemoveItem] {ex}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa sản phẩm." });
            }
        }

        // ==============================================
        // Get current selling price
        // ==============================================
        private decimal GetCurrentProductPrice(Product product)
        {
            var now = DateTime.UtcNow;
            FlashSaleItem activeFlashSale = null;

            try
            {
                activeFlashSale = _context.FlashSaleItems
                    .Include(f => f.FlashSale)
                    .Where(f => f.ProductID == product.ProductID && 
                                f.FlashSale.IsActive && 
                                f.FlashSale.StartTime <= now && 
                                f.FlashSale.EndTime >= now &&
                                f.SoldQuantity < f.StockQuantity)
                    .OrderByDescending(f => f.FlashSale.EndTime)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCurrentProductPrice] {ex}");
            }

            if (activeFlashSale != null)
            {
                return activeFlashSale.FlashSalePrice;
            }

            if (product.DiscountPrice.HasValue &&
                product.DiscountPrice.Value > 0)
            {
                return product.DiscountPrice.Value;
            }

            return product.Price;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}