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
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: /Order/Checkout
        public async Task<ActionResult> Checkout()
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var cart = await _context.Carts
                    .Include(c => c.CartItems.Select(ci => ci.Product))
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction("Index", "Cart");
                }

                // Kiểm tra lại stock trước khi cho vào trang checkout
                foreach (var item in cart.CartItems)
                {
                    if (item.Product.Status != 1 || item.Quantity > item.Product.StockQuantity)
                    {
                        TempData["ErrorMessage"] = $"Sản phẩm {item.Product.Name} không đủ số lượng hoặc ngừng kinh doanh.";
                        return RedirectToAction("Index", "Cart");
                    }
                }

                var user = await _context.Users.FindAsync(userId);

                var model = new CheckoutViewModel
                {
                    Cart = cart,
                    ShippingAddress = user?.Address,
                    ReceiverPhone = user?.PhoneNumber // Hoặc thuộc tính số điện thoại trong ApplicationUser
                };

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Order/Checkout GET] {ex}");
                TempData["ErrorMessage"] = "Lỗi khi tải trang thanh toán.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // POST: /Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = User.Identity.GetUserId();
            var cart = await _context.Carts
                .Include(c => c.CartItems.Select(ci => ci.Product))
                .FirstOrDefaultAsync(c => c.UserID == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            model.Cart = cart; // Re-assign for the View in case of error

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Kiểm tra lại stock lần cuối
                    foreach (var item in cart.CartItems)
                    {
                        if (item.Product.Status != 1)
                        {
                            ModelState.AddModelError("", $"Sản phẩm {item.Product.Name} không thể mua lúc này.");
                            return View(model);
                        }

                        if (item.Quantity > item.Product.StockQuantity)
                        {
                            ModelState.AddModelError("", $"Sản phẩm {item.Product.Name} chỉ còn {item.Product.StockQuantity} trong kho.");
                            return View(model);
                        }
                    }

                    // 2. Tính tổng tiền
                    decimal totalAmount = cart.CartItems.Sum(ci => ci.UnitPriceAtAddition * ci.Quantity);
                    decimal finalAmount = totalAmount; // Chưa tính promotion

                    // 3. Tạo Order
                    var order = new Order
                    {
                        UserID = userId,
                        OrderDate = DateTime.UtcNow,
                        TotalAmount = totalAmount,
                        DiscountAmount = 0,
                        FinalAmount = finalAmount,
                        Status = "Pending",
                        ShippingAddress = model.ShippingAddress,
                        ReceiverPhone = model.ReceiverPhone,
                        PaymentMethod = model.PaymentMethod,
                        PaymentStatus = "Unpaid",
                        Notes = model.Notes
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); // Lưu để lấy OrderID

                    // 4. Tạo OrderDetail và trừ Stock
                    foreach (var item in cart.CartItems)
                    {
                        var orderDetail = new OrderDetail
                        {
                            OrderID = order.OrderID,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPriceAtAddition,
                            Subtotal = item.UnitPriceAtAddition * item.Quantity
                        };
                        _context.OrderDetails.Add(orderDetail);

                        // Trừ Stock
                        item.Product.StockQuantity -= item.Quantity;
                        _context.Entry(item.Product).State = EntityState.Modified;
                    }

                    // 5. Xóa Cart và CartItems
                    _context.CartItems.RemoveRange(cart.CartItems);
                    _context.Carts.Remove(cart);

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Đặt hàng thành công!";
                    return RedirectToAction("OrderSuccess", new { id = order.OrderID });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"[Order/Checkout POST] {ex}");
                    ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình xử lý đơn hàng. Vui lòng thử lại.");
                    return View(model);
                }
            }
        }

        // GET: /Order/OrderSuccess
        public async Task<ActionResult> OrderSuccess(int id)
        {
            var userId = User.Identity.GetUserId();
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
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
