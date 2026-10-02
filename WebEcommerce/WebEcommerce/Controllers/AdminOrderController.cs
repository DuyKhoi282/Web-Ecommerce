using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Models;
using WebEcommerce.Filters;

namespace WebEcommerce.Controllers
{
    [CustomAuthorize(Roles = "Administrator,StoreManager")]
    public class AdminOrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminOrderController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: /AdminOrder/Index
        public async Task<ActionResult> Index(string status = "")
        {
            try
            {
                ViewBag.Title = "Quản lý Đơn hàng";
                ViewBag.CurrentStatus = status;

                var query = _context.Orders.Include(o => o.User).AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.Status == status);
                }

                var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminOrder/Index] {ex}");
                TempData["ErrorMessage"] = "Không thể tải danh sách đơn hàng.";
                return View(new System.Collections.Generic.List<Order>());
            }
        }

        // GET: /AdminOrder/Detail/5
        public async Task<ActionResult> Detail(int id)
        {
            try
            {
                ViewBag.Title = "Chi tiết Đơn hàng #" + id;

                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails.Select(od => od.Product.ProductImages))
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
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải chi tiết đơn hàng.";
                return RedirectToAction("Index");
            }
        }

        // POST: /AdminOrder/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateStatus(int orderId, string newStatus)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderDetails.Select(od => od.Product))
                        .FirstOrDefaultAsync(o => o.OrderID == orderId);

                    if (order == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
                    }

                    // Validate valid statuses
                    var validStatuses = new[] { "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled" };
                    if (!validStatuses.Contains(newStatus))
                    {
                        TempData["ErrorMessage"] = "Trạng thái không hợp lệ.";
                        return RedirectToAction("Detail", new { id = orderId });
                    }

                    // If cancelling from a non-cancelled state, restore stock
                    if (newStatus == "Cancelled" && order.Status != "Cancelled")
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            detail.Product.StockQuantity += detail.Quantity;
                            _context.Entry(detail.Product).State = EntityState.Modified;
                        }
                    }
                    // If moving from cancelled to another state, subtract stock
                    else if (order.Status == "Cancelled" && newStatus != "Cancelled")
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            if (detail.Product.StockQuantity < detail.Quantity)
                            {
                                TempData["ErrorMessage"] = $"Sản phẩm '{detail.Product.Name}' không đủ tồn kho để khôi phục đơn hàng.";
                                return RedirectToAction("Detail", new { id = orderId });
                            }
                            detail.Product.StockQuantity -= detail.Quantity;
                            _context.Entry(detail.Product).State = EntityState.Modified;
                        }
                    }

                    order.Status = newStatus;
                    
                    // Update PaymentStatus if Delivered (optional logic depending on business rules)
                    if (newStatus == "Delivered" && order.PaymentMethod == "COD")
                    {
                        order.PaymentStatus = "Paid";
                        order.PaymentDate = DateTime.UtcNow;
                    }

                    _context.Entry(order).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{orderId} thành '{newStatus}'.";
                    return RedirectToAction("Detail", new { id = orderId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"[AdminOrder/UpdateStatus] {ex}");
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật trạng thái đơn hàng.";
                    return RedirectToAction("Detail", new { id = orderId });
                }
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
