using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Models;
using WebEcommerce.Filters;

namespace WebEcommerce.Controllers
{
    [CustomAuthorize(Roles = "Administrator,StoreManager", Users = "admin@thechillshop.vn")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController()
        {
            _context = new ApplicationDbContext();
        }

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminDashboard
        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "Bảng điều khiển Tổng quan";

            var model = new AdminDashboardViewModel();

            try
            {
                // KPI 1: Total Revenue
                model.TotalRevenue = await _context.Orders
                    .Where(o => o.Status != "Cancelled")
                    .SumAsync(o => (decimal?)o.FinalAmount) ?? 0;

                // KPI 2: Total Orders
                model.TotalOrders = await _context.Orders.CountAsync();

                // KPI 3: Total Customers
                model.TotalCustomers = await _context.Users.CountAsync();

                // KPI 4: Total Products
                model.TotalProducts = await _context.Products.CountAsync();

                // Recent Orders (Top 5)
                var recentOrdersData = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

                model.RecentOrders = recentOrdersData.Select(o => new RecentOrderItemViewModel
                {
                    OrderID = o.OrderID,
                    CustomerName = o.User?.FullName ?? o.ShippingAddress ?? "Khách vãng lai",
                    OrderDate = o.OrderDate,
                    FinalAmount = o.FinalAmount,
                    OrderStatus = o.Status ?? "Pending"
                }).ToList();

                // Top Selling Products (Top 5)
                var topProductsData = await _context.Products
                    .Include(p => p.ProductImages)
                    .OrderByDescending(p => p.StockQuantity)
                    .Take(5)
                    .ToListAsync();

                model.TopProducts = topProductsData.Select(p => new TopProductItemViewModel
                {
                    ProductID = p.ProductID,
                    ProductName = p.Name,
                    Price = p.Price,
                    SoldQuantity = 0,
                    ImageUrl = p.ProductImages.FirstOrDefault(i => i.IsMain)?.ImageURL ?? p.ProductImages.FirstOrDefault()?.ImageURL
                }).ToList();

                // Chart: Last 6 months labels & estimated distribution
                var currentMonth = DateTime.UtcNow;
                for (int i = 5; i >= 0; i--)
                {
                    var m = currentMonth.AddMonths(-i);
                    model.ChartLabels.Add($"T{m.Month}/{m.Year}");

                    var monthStart = new DateTime(m.Year, m.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    var monthEnd = monthStart.AddMonths(1);

                    var rev = await _context.Orders
                        .Where(o => o.OrderDate >= monthStart && o.OrderDate < monthEnd && o.Status != "Cancelled")
                        .SumAsync(o => (decimal?)o.FinalAmount) ?? 0;

                    model.ChartRevenueData.Add(rev);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi tải số liệu Dashboard: " + ex.Message;
            }

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
