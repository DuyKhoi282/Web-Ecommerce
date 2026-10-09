using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
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

        public async Task<ActionResult> Index()
        {
            try
            {
                var categories = await _db.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                var featuredProducts = await _db.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status != 3)
                    .OrderByDescending(p => p.ProductID)
                    .Take(10)
                    .ToListAsync();

                var allProducts = await _db.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status != 3)
                    .OrderByDescending(p => p.ProductID)
                    .ToListAsync();

                var products = await _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status == 1)
                    .OrderByDescending(p => p.ViewCount)
                    .Take(8)
                    .ToListAsync();

                ViewBag.Categories = categories;
                ViewBag.FeaturedProducts = featuredProducts;
                ViewBag.AllProducts = allProducts;

                return View(products);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Home/Index] {ex}");
                ViewBag.Categories = new List<Category>();
                ViewBag.FeaturedProducts = new List<Product>();
                ViewBag.AllProducts = new List<Product>();
                return View(new List<Product>());
            }
        }
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

                var cart = await _db.Carts
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
                _db.Dispose();
            }

            base.Dispose(disposing);
        }
            base.Dispose(disposing);
        }
    }
}