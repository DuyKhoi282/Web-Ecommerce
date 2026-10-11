using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Helpers;
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
                var cookieItems = CookieCartHelper.GetCartItems(Request);

                if (!cookieItems.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                    return RedirectToAction("Index", "Cart");
                }

                // Load products from DB
                var productIds = cookieItems.Select(ci => ci.ProductID).ToList();
                var products = await _context.Products
                    .Include(p => p.ProductImages)
                    .Where(p => productIds.Contains(p.ProductID))
                    .ToListAsync();

                // Build cart view model for the checkout page
                var cartVm = new CartViewModel();
                foreach (var ci in cookieItems)
                {
                    var product = products.FirstOrDefault(p => p.ProductID == ci.ProductID);
                    if (product == null) continue;

                    var unitPrice = GetCurrentProductPrice(product);
                    var image = product.ProductImages?
                        .FirstOrDefault(x => x.IsMain)
                        ?? product.ProductImages?.FirstOrDefault();

                    cartVm.Items.Add(new CartItemViewModel
                    {
                        ProductID = product.ProductID,
                        ProductName = product.Name,
                        ImageURL = image?.ImageURL,
                        UnitPrice = unitPrice,
                        Quantity = ci.Quantity,
                        ItemTotal = unitPrice * ci.Quantity,
                        StockQuantity = product.StockQuantity
                    });
                }
                cartVm.CartTotal = cartVm.Items.Sum(i => i.ItemTotal);

                if (!cartVm.Items.Any())
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

                ViewBag.Cart = cartVm;
                ViewBag.CartTotal = cartVm.CartTotal;
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
                // ========================================
                // Read cart from Cookie
                // ========================================
                var cookieItems = CookieCartHelper.GetCartItems(Request);

                if (!cookieItems.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống.";
                    return RedirectToAction("Index", "Cart");
                }

                // Load products from DB
                var productIds = cookieItems.Select(ci => ci.ProductID).ToList();
                var products = await _context.Products
                    .Include(p => p.ProductImages)
                    .Where(p => productIds.Contains(p.ProductID))
                    .ToListAsync();

                // Build cart view model
                var cartVm = BuildCartViewModel(cookieItems, products);

                if (!ModelState.IsValid)
                {
                    ViewBag.Cart = cartVm;
                    ViewBag.CartTotal = cartVm.CartTotal;
                    return View(model);
                }

                // Validate payment method
                if (model.PaymentMethod != "COD" && model.PaymentMethod != "BankTransfer")
                {
                    ModelState.AddModelError("PaymentMethod", "Phương thức thanh toán không hợp lệ.");
                    ViewBag.Cart = cartVm;
                    ViewBag.CartTotal = cartVm.CartTotal;
                    return View(model);
                }

                // ========================================
                // RE-CHECK STOCK tại thời điểm checkout
                // ========================================
                foreach (var ci in cookieItems)
                {
                    var product = products.FirstOrDefault(p => p.ProductID == ci.ProductID);
                    if (product == null || product.Status != 1)
                    {
                        TempData["ErrorMessage"] = $"Sản phẩm \"{product?.Name ?? "không xác định"}\" không còn kinh doanh.";
                        return RedirectToAction("Index", "Cart");
                    }
                    if (ci.Quantity > product.StockQuantity)
                    {
                        TempData["ErrorMessage"] = $"Sản phẩm \"{product.Name}\" chỉ còn {product.StockQuantity} trong kho, nhưng giỏ hàng có {ci.Quantity}.";
                        return RedirectToAction("Index", "Cart");
                    }
                }

                // ========================================
                // TÍNH TOÁN SERVER-SIDE (không tin client)
                // ========================================
                decimal totalAmount = 0;
                // Build a lookup: ProductID → (Product, Quantity, CurrentPrice)
                var orderItems = new List<(Product Product, int Quantity, decimal CurrentPrice)>();
                foreach (var ci in cookieItems)
                {
                    var product = products.FirstOrDefault(p => p.ProductID == ci.ProductID);
                    if (product == null) continue;
                    var currentPrice = GetCurrentProductPrice(product);
                    totalAmount += currentPrice * ci.Quantity;
                    orderItems.Add((product, ci.Quantity, currentPrice));
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
                        ViewBag.Cart = cartVm;
                        ViewBag.CartTotal = totalAmount;
                        return View(model);
                    }

                    var now = DateTime.UtcNow;
                    if (!promo.IsActive || promo.StartDate > now || promo.EndDate < now)
                    {
                        TempData["ErrorMessage"] = "Mã giảm giá đã hết hạn hoặc chưa có hiệu lực.";
                        ViewBag.Cart = cartVm;
                        ViewBag.CartTotal = totalAmount;
                        return View(model);
                    }

                    if (promo.CurrentUsage >= promo.MaxUsage)
                    {
                        TempData["ErrorMessage"] = "Mã giảm giá đã hết lượt sử dụng.";
                        ViewBag.Cart = cartVm;
                        ViewBag.CartTotal = totalAmount;
                        return View(model);
                    }

                    if (totalAmount < promo.MinOrderAmount)
                    {
                        TempData["ErrorMessage"] = $"Đơn hàng tối thiểu {promo.MinOrderAmount:N0}đ để áp dụng mã này.";
                        ViewBag.Cart = cartVm;
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
                        foreach (var item in orderItems)
                        {
                            var orderDetail = new OrderDetail
                            {
                                OrderID = order.OrderID,
                                ProductID = item.Product.ProductID,
                                Quantity = item.Quantity,
                                UnitPrice = item.CurrentPrice,
                                Subtotal = item.CurrentPrice * item.Quantity
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

                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        // 4. Clear cart cookie after successful order
                        CookieCartHelper.ClearCart(Response);

                        TempData["SuccessMessage"] = "Đặt hàng thành công! Mã đơn hàng: #" + order.OrderID;
                        return RedirectToAction("Detail", new { id = order.OrderID });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"[Order/Checkout POST Transaction] {ex}");
                        TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tạo đơn hàng. Vui lòng thử lại.";
                        ViewBag.Cart = cartVm;
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

                // Server recalculate cart total from cookie
                var cookieItems = CookieCartHelper.GetCartItems(Request);

                if (!cookieItems.Any())
                    return Json(new { success = false, message = "Giỏ hàng trống." });

                var productIds = cookieItems.Select(ci => ci.ProductID).ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.ProductID))
                    .ToListAsync();

                decimal serverTotal = 0;
                foreach (var ci in cookieItems)
                {
                    var product = products.FirstOrDefault(p => p.ProductID == ci.ProductID);
                    if (product != null)
                    {
                        serverTotal += GetCurrentProductPrice(product) * ci.Quantity;
                    }
                }

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

        // ==============================================
        // Helper: Build CartViewModel from cookie + DB products
        // ==============================================
        private CartViewModel BuildCartViewModel(
            List<CookieCartItem> cookieItems,
            List<Product> products)
        {
            var vm = new CartViewModel();
            foreach (var ci in cookieItems)
            {
                var product = products.FirstOrDefault(p => p.ProductID == ci.ProductID);
                if (product == null) continue;

                var unitPrice = GetCurrentProductPrice(product);
                var image = product.ProductImages?
                    .FirstOrDefault(x => x.IsMain)
                    ?? product.ProductImages?.FirstOrDefault();

                vm.Items.Add(new CartItemViewModel
                {
                    ProductID = product.ProductID,
                    ProductName = product.Name,
                    ImageURL = image?.ImageURL,
                    UnitPrice = unitPrice,
                    Quantity = ci.Quantity,
                    ItemTotal = unitPrice * ci.Quantity,
                    StockQuantity = product.StockQuantity
                });
            }
            vm.CartTotal = vm.Items.Sum(i => i.ItemTotal);
            return vm;
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
