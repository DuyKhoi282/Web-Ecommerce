using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    [Authorize(Roles = "StoreManager,Administrator")]
    public class AdminOrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminOrderController()
        {
            _context = new ApplicationDbContext();
        }

        // ==============================================
        // GET: /AdminOrder
        // ==============================================
        [HttpGet]
        public async Task<ActionResult> Index(string status = "")
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(o => o.Status == status);
                }

                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                ViewBag.CurrentStatus = status;
                return View(orders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminOrder/Index] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách đơn hàng.";
                return View(new System.Collections.Generic.List<Order>());
            }
        }

        // ==============================================
        // GET: /AdminOrder/Detail/5
        // ==============================================
        [HttpGet]
        public async Task<ActionResult> Detail(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails.Select(od => od.Product.ProductImages))
                    .Include(o => o.PromotionalCode)
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminOrder/Detail] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi.";
                return RedirectToAction("Index");
            }
        }

        // ==============================================
        // POST: /AdminOrder/UpdateStatus
        // ==============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateStatus(int orderId, string newStatus)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails.Select(od => od.Product))
                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index");
                }

                // Validate transition
                if (!IsValidTransition(order.Status, newStatus))
                {
                    TempData["ErrorMessage"] = $"Không thể chuyển trạng thái từ \"{order.Status}\" sang \"{newStatus}\".";
                    return RedirectToAction("Detail", new { id = orderId });
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Nếu cancel bởi Manager -> restore stock
                        if (newStatus == "Cancelled" && order.Status != "Cancelled")
                        {
                            foreach (var detail in order.OrderDetails)
                            {
                                if (detail.Product != null)
                                {
                                    detail.Product.StockQuantity += detail.Quantity;
                                    if (detail.Product.Status == 2 && detail.Product.StockQuantity > 0)
                                    {
                                        detail.Product.Status = 1;
                                    }
                                }
                            }
                        }

                        // Nếu Delivered -> update payment status cho COD
                        if (newStatus == "Delivered")
                        {
                            if (order.PaymentMethod == "COD")
                            {
                                order.PaymentStatus = "Paid";
                                order.PaymentDate = DateTime.UtcNow;
                            }
                        }

                        order.Status = newStatus;
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{orderId} thành \"{newStatus}\".";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"[AdminOrder/UpdateStatus Transaction] {ex}");
                        TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật trạng thái.";
                    }
                }

                return RedirectToAction("Detail", new { id = orderId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminOrder/UpdateStatus] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Kiểm tra transition hợp lệ
        /// Pending -> Confirmed -> Shipped -> Delivered
        /// Pending/Confirmed -> Cancelled
        /// </summary>
        private bool IsValidTransition(string currentStatus, string newStatus)
        {
            switch (currentStatus)
            {
                case "Pending":
                    return newStatus == "Confirmed" || newStatus == "Cancelled";
                case "Confirmed":
                    return newStatus == "Shipped" || newStatus == "Cancelled";
                case "Shipped":
                    return newStatus == "Delivered";
                default:
                    return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
