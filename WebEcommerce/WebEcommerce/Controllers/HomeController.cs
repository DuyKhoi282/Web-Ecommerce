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
        private readonly ApplicationDbContext _context;

        public HomeController()
        {
            _context = new ApplicationDbContext();
        }

        public async Task<ActionResult> Index()
        {
            try
            {
                // Lấy sản phẩm còn kinh doanh (Status=1), sắp xếp theo ViewCount giảm dần
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status == 1)
                    .OrderByDescending(p => p.ViewCount)
                    .Take(8)
                    .ToListAsync();

                return View(products);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Home/Index] {ex}");
                return View(new List<Product>());
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

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