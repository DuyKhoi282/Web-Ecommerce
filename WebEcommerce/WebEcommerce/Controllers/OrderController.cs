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

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

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

                    // 2. Tính tổng tiền & Kiểm tra Promo Code
                    decimal totalAmount = cart.CartItems.Sum(ci => ci.UnitPriceAtAddition * ci.Quantity);
                    decimal discountAmount = 0;
                    decimal finalAmount = totalAmount;
                    int? promoCodeId = null;

                    if (!string.IsNullOrWhiteSpace(model.PromoCode))
                    {
                        string codeUpper = model.PromoCode.ToUpper();
                        var promo = await _context.PromotionalCodes.FirstOrDefaultAsync(p => p.Code == codeUpper && p.IsActive);
                        
                        if (promo != null && promo.StartDate <= DateTime.UtcNow && promo.EndDate >= DateTime.UtcNow && promo.CurrentUsage < promo.MaxUsage && totalAmount >= promo.MinOrderAmount)
                        {
                            promoCodeId = promo.PromoCodeID;
                            if (promo.DiscountType == "Percentage")
                            {
                                discountAmount = totalAmount * (promo.DiscountValue / 100);
                            }
                            else
                            {
                                discountAmount = promo.DiscountValue;
                            }
                            
                            if (discountAmount > totalAmount) discountAmount = totalAmount;
                            finalAmount = totalAmount - discountAmount;
                            
                            // Tăng số lượt sử dụng
                            promo.CurrentUsage++;
                            _context.Entry(promo).State = EntityState.Modified;
                        }
                    }

                    // 3. Tạo Order
                    var order = new Order
                    {
                        UserID = userId,
                        PromoCodeID = promoCodeId,
                        OrderDate = DateTime.UtcNow,
                        TotalAmount = totalAmount,
                        DiscountAmount = discountAmount,
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

                        // Cập nhật SoldQuantity cho FlashSale nếu mua với giá FlashSale
                        var now = DateTime.UtcNow;
                        var activeFlashSale = _context.FlashSaleItems
                            .Include(f => f.FlashSale)
                            .Where(f => f.ProductID == item.ProductID && 
                                        f.FlashSale.IsActive && 
                                        f.FlashSale.StartTime <= now && 
                                        f.FlashSale.EndTime >= now)
                            .OrderByDescending(f => f.FlashSale.EndTime)
                            .FirstOrDefault();

                        if (activeFlashSale != null && activeFlashSale.FlashSalePrice == item.UnitPriceAtAddition)
                        {
                            activeFlashSale.SoldQuantity += item.Quantity;
                            _context.Entry(activeFlashSale).State = EntityState.Modified;
                        }
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

        // POST: /Order/ApplyPromoCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ApplyPromoCode(string code)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var cart = await _context.Carts
                    .Include(c => c.CartItems.Select(ci => ci.Product))
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống." });
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
                }

                var promo = await _context.PromotionalCodes
                    .FirstOrDefaultAsync(p => p.Code == code.ToUpper() && p.IsActive);

                if (promo == null)
                {
                    return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc không tồn tại." });
                }

                if (promo.StartDate > DateTime.UtcNow || promo.EndDate < DateTime.UtcNow)
                {
                    return Json(new { success = false, message = "Mã giảm giá không nằm trong thời gian áp dụng." });
                }

                if (promo.CurrentUsage >= promo.MaxUsage)
                {
                    return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng." });
                }

                decimal totalAmount = cart.CartItems.Sum(ci => ci.UnitPriceAtAddition * ci.Quantity);

                if (totalAmount < promo.MinOrderAmount)
                {
                    return Json(new { success = false, message = $"Đơn hàng phải từ {promo.MinOrderAmount:N0}đ để áp dụng mã này." });
                }

                decimal discountAmount = 0;
                if (promo.DiscountType == "Percentage")
                {
                    discountAmount = totalAmount * (promo.DiscountValue / 100);
                }
                else
                {
                    discountAmount = promo.DiscountValue;
                }

                if (discountAmount > totalAmount) discountAmount = totalAmount;
                decimal finalAmount = totalAmount - discountAmount;

                return Json(new {
                    success = true,
                    message = "Áp dụng mã thành công!",
                    discountAmount = discountAmount,
                    finalAmount = finalAmount,
                    code = promo.Code
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Order/ApplyPromoCode] {ex}");
                return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống." });
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

        // GET: /Order/History
        public async Task<ActionResult> History()
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var orders = await _context.Orders
                    .Include(o => o.OrderDetails.Select(od => od.Product.ProductImages))
                    .Where(o => o.UserID == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Order/History] {ex}");
                TempData["ErrorMessage"] = "Không thể tải danh sách đơn hàng.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Order/Detail/5
        public async Task<ActionResult> Detail(int id)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var order = await _context.Orders
                    .Include(o => o.OrderDetails.Select(od => od.Product.ProductImages))
                    .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("History");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Order/Detail] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải chi tiết đơn hàng.";
                return RedirectToAction("History");
            }
        }

        // POST: /Order/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Cancel(int id)
        {
            var userId = User.Identity.GetUserId();
            var order = await _context.Orders
                .Include(o => o.OrderDetails.Select(od => od.Product))
                .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("History");
            }

            // Chỉ cho phép hủy nếu đơn hàng đang ở trạng thái Pending
            if (order.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng này do trạng thái không hợp lệ.";
                return RedirectToAction("Detail", new { id = order.OrderID });
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    order.Status = "Cancelled";
                    
                    // Phục hồi lại số lượng tồn kho (Restore Stock)
                    foreach (var detail in order.OrderDetails)
                    {
                        detail.Product.StockQuantity += detail.Quantity;
                        _context.Entry(detail.Product).State = EntityState.Modified;
                    }

                    _context.Entry(order).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    
                    transaction.Commit();
                    TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công và hoàn trả số lượng kho.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"[Order/Cancel] {ex}");
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi hủy đơn hàng. Vui lòng thử lại.";
                }
            }

            return RedirectToAction("Detail", new { id = order.OrderID });
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
