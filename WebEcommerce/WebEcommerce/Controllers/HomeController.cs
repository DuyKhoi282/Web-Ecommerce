using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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
                    .ToList();

                var featuredProducts = _db.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status != 3)
                    .OrderByDescending(p => p.ProductID)
                    .Take(10)
                    .ToList();

                var allProducts = _db.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status != 3)
                    .OrderByDescending(p => p.ProductID)
                    .ToList();

                ViewBag.Categories = categories;
                ViewBag.FeaturedProducts = featuredProducts;
                ViewBag.AllProducts = allProducts;
            }
            catch (Exception)
            {
                // Skill [security-practices]: Boundary defense try-catch
                ViewBag.Categories = new List<Category>();
                ViewBag.FeaturedProducts = new List<Product>();
                ViewBag.AllProducts = new List<Product>();
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