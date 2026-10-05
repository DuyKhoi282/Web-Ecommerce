using Microsoft.AspNet.Identity;
using System;
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

                foreach (var ci in cookieItems)
                {
                    var product = products.FirstOrDefault(p => p.ProductID == ci.ProductID);

                    if (product == null)
                    {
                        // Product was deleted from DB → remove from cookie
                        continue;
                    }

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

                // Clean up cookie: remove items whose products no longer exist
                if (vm.Items.Count < cookieItems.Count)
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
                System.Diagnostics.Debug.WriteLine(
                    $"[Cart/Index] {ex}"
                );

                TempData["ErrorMessage"] =
                    "Đã xảy ra lỗi khi tải giỏ hàng.";

                return View(new CartViewModel());
            }
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

                if (product == null || product.Status != 1)
                {
                    return Json(new { success = false, message = "Sản phẩm không còn kinh doanh." });
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

                // Calculate totals
                var unitPrice = GetCurrentProductPrice(product);
                var itemTotal = unitPrice * newQuantity;

                // Recalculate cart total
                var allItems = CookieCartHelper.GetCartItems(Request);
                // Update the in-memory list since Request cookie is stale
                var itemInList = allItems.FirstOrDefault(i => i.ProductID == productId);
                if (itemInList != null) itemInList.Quantity = newQuantity;

                // We need product prices for all items to calculate cart total
                var otherProductIds = allItems
                    .Where(i => i.ProductID != productId)
                    .Select(i => i.ProductID)
                    .ToList();

                decimal cartTotal = itemTotal;
                if (otherProductIds.Any())
                {
                    var otherProducts = await _context.Products
                        .Where(p => otherProductIds.Contains(p.ProductID))
                        .ToListAsync();

                    foreach (var ci in allItems.Where(i => i.ProductID != productId))
                    {
                        var op = otherProducts.FirstOrDefault(p => p.ProductID == ci.ProductID);
                        if (op != null)
                        {
                            cartTotal += GetCurrentProductPrice(op) * ci.Quantity;
                        }
                    }
                }

                var cartItemCount = allItems.Sum(i =>
                    i.ProductID == productId ? newQuantity : i.Quantity);

                return Json(new
                {
                    success = true,
                    message = "Đã tăng số lượng.",
                    quantity = newQuantity,
                    itemTotal = itemTotal,
                    cartTotal = cartTotal,
                    cartItemCount = cartItemCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Cart/IncreaseQuantity] {ex}");
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

                    // Calculate remaining totals
                    var remainingItems = cookieItems
                        .Where(i => i.ProductID != productId)
                        .ToList();

                    decimal remainTotal = 0;
                    int remainCount = 0;

                    if (remainingItems.Any())
                    {
                        var remainIds = remainingItems.Select(i => i.ProductID).ToList();
                        var remainProducts = await _context.Products
                            .Where(p => remainIds.Contains(p.ProductID))
                            .ToListAsync();

                        foreach (var ri in remainingItems)
                        {
                            var rp = remainProducts.FirstOrDefault(p => p.ProductID == ri.ProductID);
                            if (rp != null)
                            {
                                remainTotal += GetCurrentProductPrice(rp) * ri.Quantity;
                            }
                            remainCount += ri.Quantity;
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        removed = true,
                        message = "Đã xóa sản phẩm khỏi giỏ hàng.",
                        cartTotal = remainTotal,
                        cartItemCount = remainCount
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
                    unitPrice = GetCurrentProductPrice(product);
                }

                var itemTotal = unitPrice * newQuantity;

                // Calculate cart total
                var allItems = cookieItems.ToList();
                var itemInList = allItems.FirstOrDefault(i => i.ProductID == productId);
                if (itemInList != null) itemInList.Quantity = newQuantity;

                var otherProductIds = allItems
                    .Where(i => i.ProductID != productId)
                    .Select(i => i.ProductID)
                    .ToList();

                decimal cartTotal2 = itemTotal;
                if (otherProductIds.Any())
                {
                    var otherProducts = await _context.Products
                        .Where(p => otherProductIds.Contains(p.ProductID))
                        .ToListAsync();

                    foreach (var ci in allItems.Where(i => i.ProductID != productId))
                    {
                        var op = otherProducts.FirstOrDefault(p => p.ProductID == ci.ProductID);
                        if (op != null)
                        {
                            cartTotal2 += GetCurrentProductPrice(op) * ci.Quantity;
                        }
                    }
                }

                var cartItemCount = allItems.Sum(i => i.Quantity);

                return Json(new
                {
                    success = true,
                    removed = false,
                    message = "Đã giảm số lượng.",
                    quantity = newQuantity,
                    itemTotal = itemTotal,
                    cartTotal = cartTotal2,
                    cartItemCount = cartItemCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Cart/DecreaseQuantity] {ex}");
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

                // Calculate remaining totals
                var remainingItems = cookieItems
                    .Where(i => i.ProductID != productId)
                    .ToList();

                decimal cartTotal = 0;
                int cartItemCount = 0;

                if (remainingItems.Any())
                {
                    var remainIds = remainingItems.Select(i => i.ProductID).ToList();
                    var remainProducts = await _context.Products
                        .Where(p => remainIds.Contains(p.ProductID))
                        .ToListAsync();

                    foreach (var ri in remainingItems)
                    {
                        var rp = remainProducts.FirstOrDefault(p => p.ProductID == ri.ProductID);
                        if (rp != null)
                        {
                            cartTotal += GetCurrentProductPrice(rp) * ri.Quantity;
                        }
                        cartItemCount += ri.Quantity;
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "Đã xóa sản phẩm khỏi giỏ hàng.",
                    cartTotal = cartTotal,
                    cartItemCount = cartItemCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Cart/RemoveItem] {ex}");
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
                System.Diagnostics.Debug.WriteLine($"[Cart/GetCartCount] {ex}");
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==============================================
        // Get current selling price
        // ==============================================
        private decimal GetCurrentProductPrice(Product product)
        {
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