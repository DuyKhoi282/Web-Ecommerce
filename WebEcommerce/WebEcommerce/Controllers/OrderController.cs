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

                // Build cart view model for the checkout page - Chỉ lấy các sản phẩm còn hàng
                var cartVm = new CartViewModel();
                int outOfStockCount = 0;

                foreach (var ci in cookieItems)
                {
                    var product = products.FirstOrDefault(p => p.ProductID == ci.ProductID);
                    if (product == null) continue;

                    // Nếu sản phẩm hết hàng hoặc ngừng kinh doanh -> loại bỏ khỏi danh sách thanh toán
                    if (product.Status != 1 || product.StockQuantity <= 0)
                    {
                        outOfStockCount++;
                        continue;
                    }

                    var checkoutQty = Math.Min(ci.Quantity, product.StockQuantity);
                    if (checkoutQty <= 0)
                    {
                        outOfStockCount++;
                        continue;
                    }

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
                        Quantity = checkoutQty,
                        ItemTotal = unitPrice * checkoutQty,
                        StockQuantity = product.StockQuantity,
                        Status = product.Status
                    });
                }
                cartVm.CartTotal = cartVm.Items.Sum(i => i.ItemTotal);

                if (!cartVm.Items.Any())
                {
                    TempData["ErrorMessage"] = "Tất cả sản phẩm trong giỏ hàng hiện đã hết hàng hoặc không khả dụng để thanh toán.";
                    return RedirectToAction("Index", "Cart");
                }

                if (outOfStockCount > 0)
                {
                    TempData["InfoMessage"] = $"Đã tự động loại bỏ {outOfStockCount} sản phẩm tạm hết hàng khỏi đơn thanh toán.";
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
                // LỌC SẢN PHẨM KHẢ DỤNG TẠI THỜI ĐIỂM CHECKOUT
                // ========================================
                var orderItems = new List<(Product Product, int Quantity, decimal CurrentPrice)>();
                decimal totalAmount = 0;

                foreach (var ci in cookieItems)
                {
                    var product = products.FirstOrDefault(p => p.ProductID == ci.ProductID);
                    // Bỏ qua sản phẩm không tồn tại, hết hàng hoặc ngừng kinh doanh
                    if (product == null || product.Status != 1 || product.StockQuantity <= 0)
                    {
                        continue;
                    }

                    var purchaseQty = Math.Min(ci.Quantity, product.StockQuantity);
                    if (purchaseQty <= 0) continue;

                    var currentPrice = GetCurrentProductPrice(product);
                    totalAmount += currentPrice * purchaseQty;
                    orderItems.Add((product, purchaseQty, currentPrice));
                }

                if (!orderItems.Any())
                {
                    TempData["ErrorMessage"] = "Các sản phẩm trong giỏ hàng hiện đã hết hàng hoặc không khả dụng. Vui lòng kiểm tra lại giỏ hàng.";
                    return RedirectToAction("Index", "Cart");
                }

                decimal discountAmount = 0;
                int? promoCodeId = null;

                // ========================================
                // APPLY PROMO CODE (nếu có)
                // ========================================
                if (!string.IsNullOrWhiteSpace(model.PromoCode))
                {
                    var cleanPromo = model.PromoCode.Trim().ToUpper();
                    var promo = await _context.PromotionalCodes
                        .FirstOrDefaultAsync(p => p.Code.ToUpper() == cleanPromo || p.Code.Trim().ToUpper() == cleanPromo);

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

                            // Cập nhật số lượng đã bán của Flash Sale nếu có
                            var now = DateTime.UtcNow;
                            var activeFsItem = await _context.FlashSaleItems
                                .Include(f => f.FlashSale)
                                .FirstOrDefaultAsync(f => f.ProductID == item.Product.ProductID
                                                       && f.FlashSale != null
                                                       && f.FlashSale.IsActive
                                                       && f.FlashSale.StartTime <= now
                                                       && f.FlashSale.EndTime > now);
                            if (activeFsItem != null)
                            {
                                activeFsItem.SoldQuantity += item.Quantity;
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

                        // 4. Chỉ xóa khỏi cookie những sản phẩm đã được đặt mua thành công
                        // Các sản phẩm hết hàng còn lại trong giỏ sẽ giữ nguyên để người dùng theo dõi hoặc xóa thủ công
                        var remainingCookieItems = cookieItems
                            .Where(ci => !orderItems.Any(oi => oi.Product.ProductID == ci.ProductID))
                            .ToList();
                        CookieCartHelper.SaveCartItems(Response, remainingCookieItems);

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

                ViewBag.HasOrderReview = await _context.OrderReviews
           .AnyAsync(r => r.OrderID == id && r.UserID == userId);


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
public async Task<ActionResult> Cancel(int id, string cancellationReason)
        {
            try
            {
                // 1. Validate lý do hủy ở phía server
                cancellationReason = (cancellationReason ?? "").Trim();

                if (string.IsNullOrWhiteSpace(cancellationReason))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy đơn hàng.";
                    return RedirectToAction("Detail", new { id = id });
                }

                if (cancellationReason.Length > 1000)
                {
                    TempData["ErrorMessage"] = "Lý do hủy không được vượt quá 1.000 ký tự.";
                    return RedirectToAction("Detail", new { id = id });
                }

                var userId = User.Identity.GetUserId();

                // 2. Chỉ tải đơn hàng thuộc về khách hàng đang đăng nhập
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails.Select(od => od.Product))
                    .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index");
                }

                // 3. Khách hàng CHỈ được tự hủy đơn khi đơn ở trạng thái Pending (Chờ xử lý)
                if (order.Status != "Pending")
                {
                    TempData["ErrorMessage"] = order.Status == "Confirmed"
                        ? "Đơn hàng đã được xác nhận và đang được chuẩn bị. Quý khách không thể tự hủy trực tiếp, vui lòng liên hệ tổng đài CSKH 1900 8888 để được hỗ trợ."
                        : $"Không thể hủy đơn hàng ở trạng thái \"{order.Status}\".";

                    return RedirectToAction("Detail", new { id = id });
                }

                // 4. Cập nhật đơn hàng và hoàn trả tồn kho, flash sale, voucher trong transaction
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var now = DateTime.UtcNow;

                        foreach (var detail in order.OrderDetails)
                        {
                            if (detail.Product != null)
                            {
                                // Hoàn lại tồn kho
                                detail.Product.StockQuantity += detail.Quantity;

                                if (detail.Product.Status == 2 &&
                                    detail.Product.StockQuantity > 0)
                                {
                                    detail.Product.Status = 1;
                                }

                                // Hoàn lại số lượng đã bán của Flash Sale nếu có
                                var activeFsItem = await _context.FlashSaleItems
                                    .Include(f => f.FlashSale)
                                    .FirstOrDefaultAsync(f => f.ProductID == detail.Product.ProductID
                                                           && f.FlashSale != null
                                                           && f.FlashSale.IsActive
                                                           && f.FlashSale.StartTime <= now
                                                           && f.FlashSale.EndTime > now);
                                if (activeFsItem != null && activeFsItem.SoldQuantity > 0)
                                {
                                    activeFsItem.SoldQuantity = Math.Max(0, activeFsItem.SoldQuantity - detail.Quantity);
                                }
                            }
                        }

                        // Hoàn lại lượt sử dụng mã giảm giá (voucher) nếu có
                        if (order.PromoCodeID.HasValue)
                        {
                            var promo = await _context.PromotionalCodes.FindAsync(order.PromoCodeID.Value);
                            if (promo != null && promo.CurrentUsage > 0)
                            {
                                promo.CurrentUsage -= 1;
                            }
                        }

                        order.CancellationReason = cancellationReason;
                        order.CancelledBy = "Customer";
                        order.CancelledAt = DateTime.UtcNow;
                        order.Status = "Cancelled";

                        await _context.SaveChangesAsync();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();

                        System.Diagnostics.Debug.WriteLine(
                            $"[Order/Cancel Transaction] {ex}");

                        TempData["ErrorMessage"] =
                            "Đã xảy ra lỗi khi hủy đơn hàng. Vui lòng thử lại.";

                        return RedirectToAction("Detail", new { id = id });
                    }
                }

                // 5. Đơn hàng đã được hủy thành công.
                // Gửi email SAU KHI transaction đã commit.
                bool emailSent = false;

                try
                {
                    if (order.User != null &&
                        !string.IsNullOrWhiteSpace(order.User.Email))
                    {
                        var emailService = new EmailService();

                        string customerName = System.Web.HttpUtility.HtmlEncode(
                            order.User.FullName ?? "Quý khách");

                        string safeReason = System.Web.HttpUtility.HtmlEncode(
                            order.CancellationReason);

                        string orderDate = order.OrderDate
                            .ToLocalTime()
                            .ToString("dd/MM/yyyy HH:mm");

                        string htmlBody = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
</head>
<body style=""margin:0;padding:20px;background:#f4f5f7;
             font-family:Arial,sans-serif;color:#333;"">
    <div style=""max-width:600px;margin:0 auto;background:#fff;
                border-radius:12px;overflow:hidden;"">

        <div style=""background:#d70018;color:#fff;padding:24px;
                    text-align:center;"">
            <h1 style=""margin:0;font-size:24px;"">
                THE CHILL SHOP
            </h1>
            <p style=""margin:8px 0 0;"">
                Xác nhận hủy đơn hàng
            </p>
        </div>

        <div style=""padding:28px;"">
            <p>Xin chào <strong>{customerName}</strong>,</p>

            <p>
                Chúng tôi xác nhận rằng bạn đã hủy thành công
                đơn hàng dưới đây.
            </p>

            <div style=""background:#f8fafc;padding:18px;
                        border-radius:8px;margin:20px 0;"">
                <p>
                    <strong>Mã đơn hàng:</strong>
                    #{order.OrderID}
                </p>
                <p>
                    <strong>Ngày đặt hàng:</strong>
                    {orderDate}
                </p>
                <p>
                    <strong>Trạng thái:</strong>
                    Đã hủy
                </p>
                <p style=""margin-bottom:0;"">
                    <strong>Lý do hủy:</strong><br>
                    {safeReason}
                </p>
            </div>

            <p>
                Nếu bạn thay đổi quyết định, bạn có thể truy cập
                The Chill Shop để tham khảo và đặt hàng lại.
            </p>

            <p style=""margin-top:28px;"">
                Cảm ơn bạn đã quan tâm đến The Chill Shop.
            </p>

            <p>
                Trân trọng,<br>
                <strong>Đội ngũ The Chill Shop</strong>
            </p>
        </div>

        <div style=""background:#f4f5f7;padding:16px;
                    text-align:center;font-size:12px;color:#777;"">
            Email tự động từ The Chill Shop.
        </div>
    </div>
</body>
</html>";

                        await emailService.SendAsync(new IdentityMessage
                        {
                            Destination = order.User.Email,
                            Subject = $"Xác nhận hủy đơn hàng #{order.OrderID} - The Chill Shop",
                            Body = htmlBody
                        });

                        emailSent = true;
                    }
                }
                catch (Exception emailEx)
                {
                    // Không rollback đơn hàng nếu gửi email thất bại
                    System.Diagnostics.Debug.WriteLine(
                        $"[Customer Cancellation Email Error] Order #{id}: {emailEx}");
                }

                if (emailSent)
                {
                    TempData["SuccessMessage"] =
                        "Đã hủy đơn hàng thành công. Email xác nhận đã được gửi đến bạn.";
                }
                else
                {
                    TempData["SuccessMessage"] =
                        "Đã hủy đơn hàng thành công, nhưng email xác nhận chưa gửi được.";
                }

                return RedirectToAction("Detail", new { id = id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Order/Cancel] {ex}");

                TempData["ErrorMessage"] = "Đã xảy ra lỗi. Vui lòng thử lại.";

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

                var cleanCode = code.Trim().ToUpper();
                var promo = await _context.PromotionalCodes
                    .FirstOrDefaultAsync(p => p.Code.ToUpper() == cleanCode || p.Code.Trim().ToUpper() == cleanCode);

                if (promo == null)
                    return Json(new { success = false, message = "Mã giảm giá không tồn tại." });

                var now = DateTime.UtcNow;
                if (!promo.IsActive || promo.StartDate > now.AddMinutes(1) || promo.EndDate < now)
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
                    if (product != null && product.Status == 1 && product.StockQuantity > 0)
                    {
                        var qty = Math.Min(ci.Quantity, product.StockQuantity);
                        serverTotal += GetCurrentProductPrice(product) * qty;
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

                // Chỉ đưa vào danh sách thanh toán các sản phẩm còn hàng
                if (product.Status != 1 || product.StockQuantity <= 0)
                {
                    continue;
                }

                var qty = Math.Min(ci.Quantity, product.StockQuantity);
                if (qty <= 0) continue;

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
                    Quantity = qty,
                    ItemTotal = unitPrice * qty,
                    StockQuantity = product.StockQuantity,
                    Status = product.Status
                });
            }
            vm.CartTotal = vm.Items.Sum(i => i.ItemTotal);
            return vm;
        }

        private decimal GetCurrentProductPrice(Product product)
        {
            // 1. Kiểm tra nếu sản phẩm đang thuộc Flash Sale đang diễn ra và còn suất
            try
            {
                var now = DateTime.UtcNow;
                var flashItem = _context.FlashSaleItems
                    .AsNoTracking()
                    .Include(f => f.FlashSale)
                    .FirstOrDefault(f => f.ProductID == product.ProductID
                                      && f.FlashSale != null
                                      && f.FlashSale.IsActive
                                      && f.FlashSale.StartTime <= now
                                      && f.FlashSale.EndTime > now
                                      && f.SoldQuantity < f.MaxQuantity);
                if (flashItem != null)
                {
                    return flashItem.FlashSalePrice;
                }
            }
            catch {}

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
