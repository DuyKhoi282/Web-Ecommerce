using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: /Wishlist
        [Authorize]
        public async Task<ActionResult> Index()
        {
            try
            {
                var userId = User.Identity.GetUserId();

                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Wishlists.Any(w => w.UserID == userId))
                    .OrderByDescending(p => p.Wishlists
                        .Where(w => w.UserID == userId)
                        .Max(w => w.AddedAt))
                    .ToListAsync();

                return View(products);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"[Wishlist/Index] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách yêu thích.";
                return View(new List<Product>());
            }
        }

        // ==============================================
        // POST: /Wishlist/Toggle (AJAX)
        // Không dùng [Authorize]: request Ajax sẽ bị redirect 302 sang trang Login
        // ==============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Toggle(int productId)
        {
            var userId = User.Identity.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    loginUrl = Url.Action("Login", "Account"),
                    message = "Vui lòng đăng nhập để lưu sản phẩm yêu thích."
                });
            }

            if (productId <= 0)
            {
                return Json(new { success = false, message = "Sản phẩm không hợp lệ." });
            }

            try
            {
                var existing = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserID == userId && w.ProductID == productId);

                if (existing != null)
                {
                    _context.Wishlists.Remove(existing);
                    await _context.SaveChangesAsync();
                    return Json(new
                    {
                        success = true,
                        isLiked = false,
                        count = await _context.Wishlists.CountAsync(w => w.UserID == userId),
                        message = "Đã bỏ sản phẩm khỏi danh sách yêu thích."
                    });
                }

                if (!await _context.Products.AnyAsync(p => p.ProductID == productId))
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                _context.Wishlists.Add(new Wishlist { UserID = userId, ProductID = productId });
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    isLiked = true,
                    count = await _context.Wishlists.CountAsync(w => w.UserID == userId),
                    message = "Đã lưu sản phẩm vào danh sách yêu thích."
                });
            }
            catch (DbEntityValidationException ex)
            {
                Trace.TraceError($"[Wishlist/Toggle] Validation: {ex}");
                return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng thử lại." });
            }
            catch (Exception ex)
            {
                // Gồm cả vi phạm UNIQUE (UserID, ProductID) khi bấm liên tiếp 2 lần
                Trace.TraceError($"[Wishlist/Toggle] {ex}");
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }

        // ==============================================
        // GET: /Wishlist/MyProductIds (AJAX) - tô tim đã thích khi tải trang
        // ==============================================
        [HttpGet]
        public async Task<JsonResult> MyProductIds()
        {
            try
            {
                var userId = User.Identity.GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new { productIds = new int[0] }, JsonRequestBehavior.AllowGet);
                }

                var ids = await _context.Wishlists
                    .Where(w => w.UserID == userId)
                    .Select(w => w.ProductID)
                    .ToListAsync();

                return Json(new { productIds = ids }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"[Wishlist/MyProductIds] {ex}");
                return Json(new { productIds = new int[0] }, JsonRequestBehavior.AllowGet);
            }
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
