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

                var cart = await _context.Carts
                    .Include("CartItems.Product")
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
                    $"[Cart/Index Error] {ex.Message}"
                );

                TempData["ErrorMessage"] =
                    "Đã xảy ra lỗi khi tải giỏ hàng.";

                return View((object)null);
            }
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                if (productId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm không hợp lệ."
                    });
                }

                if (quantity <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Số lượng sản phẩm không hợp lệ."
                    });
                }

                var userId = User.Identity.GetUserId();

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

                if (product.Status == 3)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm đã ngừng kinh doanh."
                    });
                }

                if (product.StockQuantity <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm hiện đã hết hàng."
                    });
                }

                var cart = await _context.Carts
                    .Include("CartItems")
                    .FirstOrDefaultAsync(c => c.UserID == userId);

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

                var cartItem = cart.CartItems
                    .FirstOrDefault(ci => ci.ProductID == productId);

                if (cartItem == null)
                {
                    if (quantity > product.StockQuantity)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Chỉ còn {product.StockQuantity} sản phẩm trong kho."
                        });
                    }

                    cartItem = new CartItem
                    {
                        CartID = cart.CartID,
                        ProductID = product.ProductID,
                        Quantity = quantity,
                        UnitPriceAtAddition =
                            product.DiscountPrice ?? product.Price
                    };

                    _context.CartItems.Add(cartItem);
                }
                else
                {
                    var newQuantity = cartItem.Quantity + quantity;

                    if (newQuantity > product.StockQuantity)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Chỉ còn {product.StockQuantity} sản phẩm trong kho."
                        });
                    }

                    cartItem.Quantity = newQuantity;

                    // Cập nhật giá hiện tại của sản phẩm
                    cartItem.UnitPriceAtAddition =
                        product.DiscountPrice ?? product.Price;
                }

                cart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var cartItemCount = cart.CartItems.Sum(ci => ci.Quantity);

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
                    $"[Cart/AddToCart Error] {ex.Message}"
                );

                return Json(new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi thêm sản phẩm vào giỏ hàng."
                });
            }
        }
    }
}