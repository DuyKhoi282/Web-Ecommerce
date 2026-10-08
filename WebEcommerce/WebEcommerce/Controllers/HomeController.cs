using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        public ActionResult Index()
        {
            try
            {
                // Skill [ef6-npgsql-optimization]: AsNoTracking cho read-only & Include eager loading
                var categories = _db.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .Take(5)
                    .ToList();

                var featuredProducts = _db.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status != 3)
                    .OrderByDescending(p => p.ProductID)
                    .Take(8)
                    .ToList();

                ViewBag.Categories = categories;
                ViewBag.FeaturedProducts = featuredProducts;
            }
            catch (Exception)
            {
                // Skill [security-practices]: Boundary defense try-catch
                ViewBag.Categories = new List<Category>();
                ViewBag.FeaturedProducts = new List<Product>();
            }

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Giới thiệu hệ thống TheChillShop.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Liên hệ với chúng tôi.";
            return View();
        }


        // GET: /Home/GetCartCount (AJAX)
        [HttpGet]
        public async Task<JsonResult> GetCartCount()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
                }

                var userId = User.Identity.GetUserId();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (cart == null || cart.CartItems == null)
                {
                    return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
                }

                var totalCount = cart.CartItems.Sum(ci => ci.Quantity);

                return Json(new { count = totalCount }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Home/GetCartCount] {ex}");
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
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