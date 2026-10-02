using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Linq;

namespace WebEcommerce.Controllers
{
    public class HomeController : Controller
    {
        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            using (var context = new WebEcommerce.Models.ApplicationDbContext())
            {
                try 
                {
                    var now = DateTime.UtcNow;
                    var activeFlashSale = await context.FlashSales
                        .Include("FlashSaleItems.Product.ProductImages")
                        .Where(f => f.IsActive && f.StartTime <= now && f.EndTime >= now)
                        .OrderBy(f => f.EndTime)
                        .FirstOrDefaultAsync();

                    ViewBag.ActiveFlashSale = activeFlashSale;
                }
                catch (Exception ex)
                {
                    // Ignore error if FlashSale table does not exist yet
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    ViewBag.ActiveFlashSale = null;
                }

                try
                {
                    var featuredProducts = await context.Products
                        .Include("Category")
                        .Include("ProductImages")
                        .Where(p => p.Status == 1)
                        .OrderByDescending(p => p.ViewCount)
                        .Take(8)
                        .ToListAsync();
                    
                    ViewBag.FeaturedProducts = featuredProducts;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    ViewBag.FeaturedProducts = new System.Collections.Generic.List<WebEcommerce.Models.Product>();
                }
            }
            return View();
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
    }
}