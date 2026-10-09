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

        // ==============================================
        // GET: /Order/Checkout
        // ==============================================
        [HttpGet]
        public async Task<ActionResult> Checkout()
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var cart = await _context.Carts
                    .Include(c => c.CartItems.Select(ci => ci.Product.ProductImages))
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                    return RedirectToAction("Index", "Cart");
                }

                // Lấy thông tin user để pre-fill
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                var vm = new CheckoutViewModel
                {
                    ShippingAddress = user?.Address ?? "",
                    ReceiverPhone = user?.PhoneNumber ?? ""
                };

                ViewBag.Cart = cart;
                ViewBag.CartTotal = cart.CartItems.Sum(ci => ci.UnitPriceAtAddition * ci.Quantity);
                return View(vm);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Order/Checkout GET] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // ==============================================
        // POST: /Order/Checkout
        // ==============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = User.Identity.GetUserId();

            try
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems.Select(ci => ci.Product))
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống.";
                    return RedirectToAction("Index", "Cart");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Cart = cart;
                    ViewBag.CartTotal = cart.CartItems.Sum(ci => ci.UnitPriceAtAddition * ci.Quantity);
                    return View(model);
                }

                // Validate payment method
                if (model.PaymentMethod != "COD" && model.PaymentMethod != "BankTransfer")
                {
                    ModelState.AddModelError("PaymentMethod", "Phương thức thanh toán không hợp lệ.");
                    ViewBag.Cart = cart;
                    ViewBag.CartTotal = cart.CartItems.Sum(ci => ci.UnitPriceAtAddition * ci.Quantity);
                    return View(model);
                }

                // ========================================
                // RE-CHECK STOCK tại thời điểm checkout
                // ========================================
                foreach (var item in cart.CartItems)
                {
                    var product = item.Product;
                    if (product == null || product.Status != 1)
                    {
                        TempData["ErrorMessage"] = $"Sản phẩm \"{product?.Name ?? "không xác định"}\" không còn kinh doanh.";
                        return RedirectToAction("Index", "Cart");
                    }
                    if (item.Quantity > product.StockQuantity)
                    {
                        TempData["ErrorMessage"] = $"Sản phẩm \"{product.Name}\" chỉ còn {product.StockQuantity} trong kho, nhưng giỏ hàng có {item.Quantity}.";
                        return RedirectToAction("Index", "Cart");
                    }
                }

                // ========================================
                // TÍNH TOÁN SERVER-SIDE (không tin client)
                // ========================================
                decimal totalAmount = 0;
                foreach (var item in cart.CartItems)
                {
                    var currentPrice = GetCurrentProductPrice(item.Product);
                    item.UnitPriceAtAddition = currentPrice;
                    totalAmount += currentPrice * item.Quantity;
                }

                decimal discountAmount = 0;
                int? promoCodeId = null;

                // ========================================
                // APPLY PROMO CODE (nếu có)
                // ========================================
                if (!string.IsNullOrWhiteSpace(model.PromoCode))
                {
                    var promo = await _context.PromotionalCodes
                        .FirstOrDefaultAsync(p => p.Code == model.PromoCode.Trim());

                    if (promo == null)
                    {
                        TempData["ErrorMessage"] = "Mã giảm giá không tồn tại.";
                        ViewBag.Cart = cart;
                        ViewBag.CartTotal = totalAmount;
                        return View(model);
                    }

                    var now = DateTime.UtcNow;
                    if (!promo.IsActive || promo.StartDate > now || promo.EndDate < now)
                    {
                        TempData["ErrorMessage"] = "Mã giảm giá đã hết hạn hoặc chưa có hiệu lực.";
                        ViewBag.Cart = cart;
                        ViewBag.CartTotal = totalAmount;
                        return View(model);
                    }

                    if (promo.CurrentUsage >= promo.MaxUsage)
                    {
                        TempData["ErrorMessage"] = "Mã giảm giá đã hết lượt sử dụng.";
                        ViewBag.Cart = cart;
                        ViewBag.CartTotal = totalAmount;
                        return View(model);
                    }

                    if (totalAmount < promo.MinOrderAmount)
                    {
                        TempData["ErrorMessage"] = $"Đơn hàng tối thiểu {promo.MinOrderAmount:N0}đ để áp dụng mã này.";
                        ViewBag.Cart = cart;
                        ViewBag.CartTotal = totalAmount;
                        return View(model);
                    }

                    // Tính discount
                    if (promo.DiscountType == "Percentage")
                    {
                        discountAmount = totalAmount * promo.DiscountValue / 100;
                    }
                    else // Fixed
                    {
                        discountAmount = promo.DiscountValue;
                    }

                    // Không cho discount > totalAmount
                    if (discountAmount > totalAmount)
                    {
                        discountAmount = totalAmount;
                    }

                    promoCodeId = promo.PromoCodeID;
                }

                var finalAmount = totalAmount - discountAmount;
                if (finalAmount < 0) finalAmount = 0;

                // ========================================
                // TRANSACTION: Create Order
                // ========================================
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Create Order
                        var order = new Order
                        {
                            UserID = userId,
                            PromoCodeID = promoCodeId,
                            OrderDate = DateTime.UtcNow,
                            TotalAmount = totalAmount,
                            DiscountAmount = discountAmount,
                            FinalAmount = finalAmount,
                            Status = "Pending",
                            ShippingAddress = model.ShippingAddress.Trim(),
                            ReceiverPhone = model.ReceiverPhone.Trim(),
                            PaymentMethod = model.PaymentMethod,
                            PaymentStatus = "Unpaid",
                            Notes = model.Notes?.Trim()
                        };
                        _context.Orders.Add(order);
                        await _context.SaveChangesAsync();

                        // 2. Create OrderDetails + Decrement Stock
                        foreach (var item in cart.CartItems)
                        {
                            var currentPrice = GetCurrentProductPrice(item.Product);
                            var orderDetail = new OrderDetail
                            {
                                OrderID = order.OrderID,
                                ProductID = item.ProductID,
                                Quantity = item.Quantity,
                                UnitPrice = currentPrice,
                                Subtotal = currentPrice * item.Quantity
                            };
                            _context.OrderDetails.Add(orderDetail);

                            // Decrement stock
                            item.Product.StockQuantity -= item.Quantity;
                            if (item.Product.StockQuantity <= 0)
                            {
                                item.Product.StockQuantity = 0;
                                item.Product.Status = 2; // Out of stock
                            }
                        }

                        // 3. Update promo usage
                        if (promoCodeId.HasValue)
                        {
                            var promo = await _context.PromotionalCodes.FindAsync(promoCodeId.Value);
                            if (promo != null)
                            {
                                promo.CurrentUsage += 1;
                            }
                        }

                        // 4. Clear cart
                        _context.CartItems.RemoveRange(cart.CartItems);
                        cart.UpdatedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        TempData["SuccessMessage"] = "Đặt hàng thành công! Mã đơn hàng: #" + order.OrderID;
                        return RedirectToAction("Detail", new { id = order.OrderID });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"[Order/Checkout POST Transaction] {ex}");
                        TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tạo đơn hàng. Vui lòng thử lại.";
                        ViewBag.Cart = cart;
                        ViewBag.CartTotal = totalAmount;
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Order/Checkout POST] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // ==============================================
        // GET: /Order (Order History)
        // ==============================================
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var orders = await _context.Orders
                    .Include(o => o.OrderDetails.Select(od => od.Product))
                    .Where(o => o.UserID == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Order/Index] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải đơn hàng.";
                return View(new System.Collections.Generic.List<Order>());
            }
        }

        // ==============================================
        // GET: /Order/Detail/5
        // ==============================================
        [HttpGet]
        public async Task<ActionResult> Detail(int id)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var order = await _context.Orders
                    .Include(o => o.OrderDetails.Select(od => od.Product.ProductImages))
                    .Include(o => o.PromotionalCode)
                    .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Order/Detail] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi.";
                return RedirectToAction("Index");
            }
        }

        // ==============================================
        // POST: /Order/Cancel/5
        // ==============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Cancel(int id)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var order = await _context.Orders
                    .Include(o => o.OrderDetails.Select(od => od.Product))
                    .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index");
                }

                // Chỉ cancel khi Pending hoặc Confirmed
                if (order.Status != "Pending" && order.Status != "Confirmed")
                {
                    TempData["ErrorMessage"] = $"Không thể hủy đơn hàng ở trạng thái \"{order.Status}\".";
                    return RedirectToAction("Detail", new { id = id });
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Restore stock
                        foreach (var detail in order.OrderDetails)
                        {
                            if (detail.Product != null)
                            {
                                detail.Product.StockQuantity += detail.Quantity;
                                if (detail.Product.Status == 2 && detail.Product.StockQuantity > 0)
                                {
                                    detail.Product.Status = 1; // Back in stock
                                }
                            }
                        }

                        order.Status = "Cancelled";
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công.";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"[Order/Cancel Transaction] {ex}");
                        TempData["ErrorMessage"] = "Đã xảy ra lỗi khi hủy đơn hàng.";
                    }
                }

                return RedirectToAction("Detail", new { id = id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Order/Cancel] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi.";
                return RedirectToAction("Index");
            }
        }

        // ==============================================
        // POST: /Order/ApplyPromoCode (AJAX)
        // ==============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ApplyPromoCode(string code, decimal cartTotal)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
                }

                var promo = await _context.PromotionalCodes
                    .FirstOrDefaultAsync(p => p.Code == code.Trim());

                if (promo == null)
                    return Json(new { success = false, message = "Mã giảm giá không tồn tại." });

                var now = DateTime.UtcNow;
                if (!promo.IsActive || promo.StartDate > now || promo.EndDate < now)
                    return Json(new { success = false, message = "Mã giảm giá đã hết hạn hoặc chưa có hiệu lực." });

                if (promo.CurrentUsage >= promo.MaxUsage)
                    return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng." });

                // Server recalculate cart total
                var userId = User.Identity.GetUserId();
                var cart = await _context.Carts
                    .Include(c => c.CartItems.Select(ci => ci.Product))
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (cart == null || !cart.CartItems.Any())
                    return Json(new { success = false, message = "Giỏ hàng trống." });

                var serverTotal = cart.CartItems.Sum(ci => GetCurrentProductPrice(ci.Product) * ci.Quantity);

                if (serverTotal < promo.MinOrderAmount)
                    return Json(new { success = false, message = $"Đơn hàng tối thiểu {promo.MinOrderAmount:N0}đ." });

                decimal discount = 0;
                if (promo.DiscountType == "Percentage")
                    discount = serverTotal * promo.DiscountValue / 100;
                else
                    discount = promo.DiscountValue;

                if (discount > serverTotal) discount = serverTotal;

                return Json(new
                {
                    success = true,
                    message = $"Áp dụng mã \"{promo.Code}\" thành công!",
                    discount = discount,
                    finalAmount = serverTotal - discount,
                    description = promo.Description
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Order/ApplyPromoCode] {ex}");
                return Json(new { success = false, message = "Đã xảy ra lỗi." });
            }
        }

        private decimal GetCurrentProductPrice(Product product)
        {
            if (product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0)
                return product.DiscountPrice.Value;
            return product.Price;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
