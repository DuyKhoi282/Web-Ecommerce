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
                var cookieItems = CookieCartHelper.GetCartItems(Request);

                if (!cookieItems.Any())
                {
                    return View(new CartViewModel());
                }

                // Load product details from DB for each cookie item
                var productIds = cookieItems.Select(ci => ci.ProductID).ToList();
                var products = await _context.Products
                    .Include(p => p.ProductImages)
                    .Where(p => productIds.Contains(p.ProductID))
                    .ToListAsync();

                var vm = new CartViewModel();
                var adjustedNames = new List<string>();

                foreach (var ci in cookieItems)
                {
                    var product = products.FirstOrDefault(p => p.ProductID == ci.ProductID);

                    if (product == null)
                    {
                        // Product was deleted from DB → remove from cookie
                        continue;
                    }

                    // Kho giảm sau khi khách thêm vào giỏ → hạ số lượng về tồn kho (Checkout cũng chỉ đặt tối đa bằng tồn kho)
                    var quantity = ci.Quantity;
                    if (product.StockQuantity > 0 && quantity > product.StockQuantity)
                    {
                        quantity = product.StockQuantity;
                        adjustedNames.Add(product.Name);
                    }

                    var unitPrice = FlashSaleHelper.GetUnitPrice(_context, product, quantity);
                    var image = product.ProductImages?
                        .FirstOrDefault(x => x.IsMain)
                        ?? product.ProductImages?.FirstOrDefault();

                    vm.Items.Add(new CartItemViewModel
                    {
                        ProductID = product.ProductID,
                        ProductName = product.Name,
                        ImageURL = image?.ImageURL,
                        UnitPrice = unitPrice,
                        Quantity = quantity,
                        ItemTotal = unitPrice * quantity,
                        StockQuantity = product.StockQuantity,
                        Status = product.Status
                    });
                }

                // Tổng tiền giỏ hàng chỉ tính các sản phẩm còn hàng
                vm.CartTotal = vm.AvailableCartTotal;

                if (adjustedNames.Any())
                {
                    ViewBag.StockAdjustedMessage =
                        "Kho không còn đủ hàng nên số lượng đã được giảm về mức tồn kho cho: "
                        + string.Join(", ", adjustedNames) + ".";
                }

                // Clean up cookie: remove items whose products no longer exist or exceed stock
                if (vm.Items.Count < cookieItems.Count || adjustedNames.Any())
                {
                    var validItems = vm.Items
                        .Select(i => new CookieCartItem
                        {
                            ProductID = i.ProductID,
                            Quantity = i.Quantity
                        })
                        .ToList();

                    CookieCartHelper.SaveCartItems(Response, validItems);
                }

                return View(vm);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    $"[Cart/Index] {ex}"
                );

                TempData["ErrorMessage"] =
                    "Đã xảy ra lỗi khi tải giỏ hàng.";

                return View(new CartViewModel());
            }
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [AllowAnonymous]
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
                        isAuth = true,
                        message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng."
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
                // 7. Read current cookie cart
                // ==========================================
                var cookieItems = CookieCartHelper.GetCartItems(Request);
                var existing = cookieItems
                    .FirstOrDefault(i => i.ProductID == productId);

                // ==========================================
                // 8. Check total quantity (existing + new)
                // ==========================================
                if (existing != null)
                {
                    var newQuantity = existing.Quantity + quantity;

                    if (newQuantity > product.StockQuantity)
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                $"Trong giỏ đã có {existing.Quantity} sản phẩm. " +
                                $"Kho chỉ còn {product.StockQuantity} sản phẩm."
                        });
                    }
                }

                // ==========================================
                // 9. Save to Cookie
                // ==========================================
                CookieCartHelper.AddItem(Request, Response, productId, quantity);

                // ==========================================
                // 10. Calculate Cart Item Count
                // ==========================================
                var cartItemCount = CookieCartHelper.GetTotalQuantity(Request);

                // After AddItem, re-read to get accurate count
                var updatedItems = CookieCartHelper.GetCartItems(Request);

                // Because AddItem modified the Response cookie but 
                // Request cookie is stale, calculate from what we know
                int totalCount;
                if (existing != null)
                {
                    totalCount = cookieItems.Sum(i => i.Quantity) + quantity;
                }
                else
                {
                    totalCount = cookieItems.Sum(i => i.Quantity) + quantity;
                }

                return Json(new
                {
                    success = true,
                    message = "Đã thêm sản phẩm vào giỏ hàng.",
                    cartItemCount = totalCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
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

        // ==============================================
        // POST: /Cart/IncreaseQuantity
        // ==============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> IncreaseQuantity(int productId)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ." });
                }

                // Find product in DB
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductID == productId);

                if (product == null || product.Status != 1 || product.StockQuantity <= 0)
                {
                    return Json(new { success = false, message = "Sản phẩm hiện đã hết hàng hoặc không còn kinh doanh." });
                }

                // Read cookie
                var cookieItems = CookieCartHelper.GetCartItems(Request);
                var existing = cookieItems
                    .FirstOrDefault(i => i.ProductID == productId);

                if (existing == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
                }

                // Check stock
                var newQuantity = existing.Quantity + 1;
                if (newQuantity > product.StockQuantity)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể tăng thêm. Kho chỉ còn {product.StockQuantity} sản phẩm."
                    });
                }

                // Update cookie
                CookieCartHelper.UpdateQuantity(Request, Response, productId, newQuantity);

                // Giá có thể đổi theo số lượng (hết suất Flash Sale → về giá thường)
                var unitPrice = FlashSaleHelper.GetUnitPrice(_context, product, newQuantity);
                var itemTotal = unitPrice * newQuantity;

                // Update the in-memory list since Request cookie is stale
                existing.Quantity = newQuantity;
                var totals = await CalculateCartTotalsAsync(cookieItems);

                return Json(new
                {
                    success = true,
                    message = "Đã tăng số lượng.",
                    quantity = newQuantity,
                    unitPrice = unitPrice,
                    itemTotal = itemTotal,
                    cartTotal = totals.CartTotal,
                    availableCount = totals.AvailableCount,
                    cartItemCount = totals.ItemCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Cart/IncreaseQuantity] {ex}");
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }

        // ==============================================
        // POST: /Cart/DecreaseQuantity
        // ==============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DecreaseQuantity(int productId)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ." });
                }

                // Read cookie
                var cookieItems = CookieCartHelper.GetCartItems(Request);
                var existing = cookieItems
                    .FirstOrDefault(i => i.ProductID == productId);

                if (existing == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
                }

                // If quantity = 1 → remove item
                if (existing.Quantity <= 1)
                {
                    CookieCartHelper.RemoveItem(Request, Response, productId);

                    var remainTotals = await CalculateCartTotalsAsync(
                        cookieItems.Where(i => i.ProductID != productId).ToList());

                    return Json(new
                    {
                        success = true,
                        removed = true,
                        message = "Đã xóa sản phẩm khỏi giỏ hàng.",
                        cartTotal = remainTotals.CartTotal,
                        availableCount = remainTotals.AvailableCount,
                        cartItemCount = remainTotals.ItemCount
                    });
                }

                // Decrease by 1
                var newQuantity = existing.Quantity - 1;
                CookieCartHelper.UpdateQuantity(Request, Response, productId, newQuantity);

                // Get product for price calculation
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductID == productId);

                decimal unitPrice = 0;
                if (product != null)
                {
                    unitPrice = FlashSaleHelper.GetUnitPrice(_context, product, newQuantity);
                }

                var itemTotal = unitPrice * newQuantity;

                // Update the in-memory list since Request cookie is stale
                existing.Quantity = newQuantity;
                var totals = await CalculateCartTotalsAsync(cookieItems);

                return Json(new
                {
                    success = true,
                    removed = false,
                    message = "Đã giảm số lượng.",
                    quantity = newQuantity,
                    unitPrice = unitPrice,
                    itemTotal = itemTotal,
                    cartTotal = totals.CartTotal,
                    availableCount = totals.AvailableCount,
                    cartItemCount = totals.ItemCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Cart/DecreaseQuantity] {ex}");
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }

        // ==============================================
        // POST: /Cart/RemoveItem
        // ==============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RemoveItem(int productId)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ." });
                }

                var cookieItems = CookieCartHelper.GetCartItems(Request);
                var existing = cookieItems
                    .FirstOrDefault(i => i.ProductID == productId);

                if (existing == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
                }

                CookieCartHelper.RemoveItem(Request, Response, productId);

                var totals = await CalculateCartTotalsAsync(
                    cookieItems.Where(i => i.ProductID != productId).ToList());

                return Json(new
                {
                    success = true,
                    removed = true,
                    message = "Đã xóa sản phẩm khỏi giỏ hàng.",
                    cartTotal = totals.CartTotal,
                    availableCount = totals.AvailableCount,
                    cartItemCount = totals.ItemCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Cart/RemoveItem] {ex}");
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }

        // ==============================================
        // GET: /Cart/GetCartCount (AJAX)
        // ==============================================
        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetCartCount()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
                }

                var totalCount = CookieCartHelper.GetTotalQuantity(Request);
                return Json(new { count = totalCount }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Cart/GetCartCount] {ex}");
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Tổng tiền và số lượng khả dụng chỉ tính sản phẩm còn hàng (khớp CartViewModel.AvailableCartTotal/AvailableItemCount);
        /// ItemCount là tổng số lượng mọi sản phẩm trong giỏ (badge header).
        /// </summary>
        private async Task<(decimal CartTotal, int AvailableCount, int ItemCount)> CalculateCartTotalsAsync(
            List<CookieCartItem> items)
        {
            decimal cartTotal = 0m;
            int availableCount = 0;

            if (items.Any())
            {
                var ids = items.Select(i => i.ProductID).ToList();
                var products = await _context.Products
                    .Where(p => ids.Contains(p.ProductID))
                    .ToListAsync();

                foreach (var ci in items)
                {
                    var p = products.FirstOrDefault(x => x.ProductID == ci.ProductID);
                    if (p != null && p.Status == 1 && p.StockQuantity > 0)
                    {
                        cartTotal += FlashSaleHelper.GetUnitPrice(_context, p, ci.Quantity) * ci.Quantity;
                        availableCount += ci.Quantity;
                    }
                }
            }

            return (cartTotal, availableCount, items.Sum(i => i.Quantity));
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