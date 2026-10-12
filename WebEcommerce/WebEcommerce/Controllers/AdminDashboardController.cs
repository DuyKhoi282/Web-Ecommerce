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
    [CustomAuthorize(Roles = "Administrator,StoreManager")]
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

                // KPI bổ sung: Đơn chờ duyệt & Sản phẩm sắp hết hàng (< 5)
                model.PendingOrdersCount = await _context.Orders.CountAsync(o => o.Status == "Pending");
                model.LowStockProductsCount = await _context.Products.CountAsync(p => p.StockQuantity < 5 && p.Status == 1);

                // Recent Orders (Top 5)
                var recentOrdersData = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

                model.RecentOrders = recentOrdersData.Select(o => new RecentOrderItemViewModel
                {
                    OrderID = o.OrderID,
                    CustomerName = !string.IsNullOrEmpty(o.User?.FullName) ? o.User.FullName : (!string.IsNullOrEmpty(o.ShippingAddress) ? o.ShippingAddress : "Khách vãng lai"),
                    OrderDate = o.OrderDate,
                    FinalAmount = o.FinalAmount,
                    OrderStatus = o.Status ?? "Pending"
                }).ToList();

                // Top Selling Products (Thống kê thực tế từ OrderDetails)
                var topSoldQuery = await _context.OrderDetails
                    .Where(od => od.Order.Status != "Cancelled")
                    .GroupBy(od => od.ProductID)
                    .Select(g => new { ProductID = g.Key, TotalSold = g.Sum(x => x.Quantity) })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(5)
                    .ToListAsync();

                if (topSoldQuery.Any())
                {
                    var pIds = topSoldQuery.Select(x => x.ProductID).ToList();
                    var products = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.ProductImages)
                        .Where(p => pIds.Contains(p.ProductID))
                        .ToListAsync();

                    model.TopProducts = topSoldQuery.Select(ts =>
                    {
                        var prod = products.FirstOrDefault(p => p.ProductID == ts.ProductID);
                        return new TopProductItemViewModel
                        {
                            ProductID = ts.ProductID,
                            ProductName = prod?.Name ?? $"Sản phẩm #{ts.ProductID}",
                            Price = prod?.Price ?? 0,
                            SoldQuantity = ts.TotalSold,
                            ImageUrl = prod?.ProductImages?.FirstOrDefault(i => i.IsMain)?.ImageURL ?? prod?.ProductImages?.FirstOrDefault()?.ImageURL
                        };
                    }).ToList();
                }
                else
                {
                    // Fallback: Khi chưa có đơn hàng, hiển thị sản phẩm được xem nhiều nhất
                    var fallbackProducts = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.ProductImages)
                        .OrderByDescending(p => p.ViewCount)
                        .Take(5)
                        .ToListAsync();

                    model.TopProducts = fallbackProducts.Select(p => new TopProductItemViewModel
                    {
                        ProductID = p.ProductID,
                        ProductName = p.Name,
                        Price = p.Price,
                        SoldQuantity = 0,
                        ImageUrl = p.ProductImages?.FirstOrDefault(i => i.IsMain)?.ImageURL ?? p.ProductImages?.FirstOrDefault()?.ImageURL
                    }).ToList();
                }

                // Tối ưu: Lấy toàn bộ đơn hàng 6 tháng gần nhất trong 1 truy vấn duy nhất
                var currentMonth = DateTime.UtcNow;
                var sixMonthsAgo = new DateTime(currentMonth.Year, currentMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
                var ordersLast6Months = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.OrderDate >= sixMonthsAgo && o.Status != "Cancelled")
                    .Select(o => new { o.OrderDate, o.FinalAmount })
                    .ToListAsync();

                for (int i = 5; i >= 0; i--)
                {
                    var m = currentMonth.AddMonths(-i);
                    model.ChartLabels.Add($"T{m.Month}/{m.Year}");

                    var rev = ordersLast6Months
                        .Where(o => o.OrderDate.Year == m.Year && o.OrderDate.Month == m.Month)
                        .Sum(o => o.FinalAmount);

                    model.ChartRevenueData.Add(rev);
                }

                // Tính toán tỷ lệ tăng trưởng doanh thu so với tháng trước thực tế
                var thisMonthRev = model.ChartRevenueData.LastOrDefault();
                var lastMonthRev = model.ChartRevenueData.Count >= 2 ? model.ChartRevenueData[model.ChartRevenueData.Count - 2] : 0;
                if (lastMonthRev > 0)
                {
                    model.RevenueGrowthPercentage = (double)Math.Round(((thisMonthRev - lastMonthRev) / lastMonthRev) * 100, 1);
                }
                else if (thisMonthRev > 0)
                {
                    model.RevenueGrowthPercentage = 100;
                }
                else
                {
                    model.RevenueGrowthPercentage = 0;
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi tải số liệu Dashboard: " + ex.Message;
            }

            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> GetRevenueData()
        {
            var labels = new List<string>();
            var revenues = new List<decimal>();

            var currentMonth = DateTime.UtcNow;
            var sixMonthsAgo = new DateTime(currentMonth.Year, currentMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
            var ordersLast6Months = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderDate >= sixMonthsAgo && o.Status != "Cancelled")
                .Select(o => new { o.OrderDate, o.FinalAmount })
                .ToListAsync();

            for (int i = 5; i >= 0; i--)
            {
                var m = currentMonth.AddMonths(-i);
                labels.Add($"T{m.Month}/{m.Year}");

                var rev = ordersLast6Months
                    .Where(o => o.OrderDate.Year == m.Year && o.OrderDate.Month == m.Month)
                    .Sum(o => o.FinalAmount);

                revenues.Add(rev);
            }

            var statusCounts = await _context.Orders
                .AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusLabels = statusCounts.Select(s => s.Status ?? "Pending").ToList();
            var statusData = statusCounts.Select(s => s.Count).ToList();

            return Json(new { months = labels, revenues = revenues, statusLabels = statusLabels, statusData = statusData }, JsonRequestBehavior.AllowGet);
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
