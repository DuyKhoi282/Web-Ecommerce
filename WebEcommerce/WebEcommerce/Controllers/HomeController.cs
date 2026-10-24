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

                // Query danh sách các đợt Flash Sale đang và sắp diễn ra trong CSDL
                var flashSales = new List<FlashSale>();
                FlashSale activeFlashSale = null;
                try
                {
                    var now = DateTime.UtcNow;
                    flashSales = await _db.FlashSales
                        .AsNoTracking()
                        .Include(fs => fs.FlashSaleItems)
                        .Include(fs => fs.FlashSaleItems.Select(fsi => fsi.Product))
                        .Include(fs => fs.FlashSaleItems.Select(fsi => fsi.Product.ProductImages))
                        .Include(fs => fs.FlashSaleItems.Select(fsi => fsi.Product.Category))
                        .Where(fs => fs.IsActive && fs.EndTime > now)
                        .OrderBy(fs => fs.StartTime)
                        .ToListAsync();

                    foreach (var fs in flashSales)
                    {
                        if (fs.FlashSaleItems != null)
                        {
                            fs.FlashSaleItems = fs.FlashSaleItems
                                .Where(fsi => fsi.Product != null && fsi.Product.Status != 3)
                                .ToList();
                        }
                    }

                    // Ưu tiên đợt đang diễn ra (StartTime <= now < EndTime), nếu không có thì lấy đợt sắp diễn ra sớm nhất
                    activeFlashSale = flashSales.FirstOrDefault(fs => fs.StartTime <= now && fs.EndTime > now)
                                      ?? flashSales.FirstOrDefault();
                }
                catch (Exception fsEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Home/FlashSales] {fsEx.Message}");
                }

                ViewBag.Categories = categories;
                ViewBag.FeaturedProducts = featuredProducts;
                ViewBag.AllProducts = allProducts;
                ViewBag.FlashSales = flashSales;
                ViewBag.ActiveFlashSale = activeFlashSale;

                return View(products);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Home/Index] {ex}");
                ViewBag.Categories = new List<Category>();
                ViewBag.FeaturedProducts = new List<Product>();
                ViewBag.AllProducts = new List<Product>();
                ViewBag.FlashSales = new List<FlashSale>();
                ViewBag.ActiveFlashSale = null;
                return View(new List<Product>());
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
    }
}