using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Filters;
using WebEcommerce.Models;

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

        // ==============================================
        // GET: /AdminOrder
        // ==============================================
        [HttpGet]
        public async Task<ActionResult> Index(string status = "")
        {
            try
            {
                var query = _context.Orders
                    .AsNoTracking()
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
        public async Task<ActionResult> UpdateStatus(
    int orderId,
    string newStatus,
    string cancellationReason) // Phúc thêm cancellationReason 
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
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

                // Chặn xuất kho/giao hàng (Shipped) và hoàn thành (Delivered) cho đơn chuyển khoản khi chưa thanh toán
                if ((newStatus == "Shipped" || newStatus == "Delivered") && order.PaymentMethod == "BankTransfer" && order.PaymentStatus != "Paid")
                {
                    TempData["ErrorMessage"] = $"Không thể chuyển đơn hàng chuyển khoản sang \"{newStatus}\" khi chưa nhận thanh toán! Đơn hàng chuyển khoản bắt buộc phải được xác nhận thanh toán trước khi xuất kho / giao hàng.";
                    return RedirectToAction("Detail", new { id = orderId });
                }

                // Kiểm tra lý do khi Manager hủy đơn
                if (newStatus == "Cancelled")
                {
                    cancellationReason = (cancellationReason ?? "").Trim();

                    if (string.IsNullOrWhiteSpace(cancellationReason))
                    {
                        TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy đơn hàng.";
                        return RedirectToAction("Detail", new { id = orderId });
                    }

                    if (cancellationReason.Length > 1000)
                    {
                        TempData["ErrorMessage"] = "Lý do hủy không được vượt quá 1000 ký tự.";
                        return RedirectToAction("Detail", new { id = orderId });
                    }
                }

                bool updateSucceeded = false;
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

                        // Lưu thông tin hủy nếu Manager hủy đơn
                        if (newStatus == "Cancelled")
                        {
                            order.CancellationReason = cancellationReason;
                            order.CancelledBy = "Manager";
                            order.CancelledAt = DateTime.UtcNow;
                        }

                        order.Status = newStatus;

                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        updateSucceeded = true;

                        TempData["SuccessMessage"] =
                            $"Đã cập nhật trạng thái đơn hàng #{orderId} thành \"{newStatus}\".";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"[AdminOrder/UpdateStatus Transaction] {ex}");
                        TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật trạng thái.";
                    }
                }

                // Chỉ gửi email khi cập nhật trạng thái thành công
                if (updateSucceeded && newStatus == "Confirmed")
                {
                    try
                    {
                        if (order.User != null &&
                            !string.IsNullOrWhiteSpace(order.User.Email))
                        {
                            var emailService = new EmailService();

                            var productRows = string.Join("", order.OrderDetails.Select(d =>
                                $@"
                <tr>
                    <td style='padding:12px 8px;border-bottom:1px solid #eee;'>
                        {System.Web.HttpUtility.HtmlEncode(d.Product?.Name ?? "Sản phẩm")}
                    </td>
                    <td style='padding:12px 8px;text-align:center;border-bottom:1px solid #eee;'>
                        {d.Quantity}
                    </td>
                    <td style='padding:12px 8px;text-align:right;border-bottom:1px solid #eee;'>
                        {d.UnitPrice:N0}đ
                    </td>
                    <td style='padding:12px 8px;text-align:right;border-bottom:1px solid #eee;'>
                        {d.Subtotal:N0}đ
                    </td>
                </tr>"
                            ));

                            var htmlBody = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0;padding:0;background:#f4f5f7;font-family:Arial,Helvetica,sans-serif;color:#333;'>
    <table role='presentation' width='100%' cellpadding='0' cellspacing='0'
           style='background:#f4f5f7;padding:24px 10px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='600' cellpadding='0' cellspacing='0'
                       style='width:100%;max-width:600px;background:#fff;border-radius:12px;overflow:hidden;'>
                    <tr>
                        <td style='background:#d70018;padding:26px 24px;text-align:center;color:#fff;'>
                            <h1 style='margin:0;font-size:25px;'>The Chill Shop</h1>
                            <p style='margin:8px 0 0;font-size:14px;'>Công nghệ trong tầm tay</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:28px 24px;'>
                            <h2 style='margin:0 0 16px;color:#d70018;font-size:22px;'>
                                Đơn hàng đã được xác nhận!
                            </h2>

                            <p>Xin chào
                                <strong>{System.Web.HttpUtility.HtmlEncode(order.User.FullName ?? "Quý khách")}</strong>,
                            </p>

                            <p>Đơn hàng của bạn đã được xác nhận và đang được chuẩn bị.</p>

                            <p><strong>Mã đơn hàng:</strong> #{order.OrderID}</p>
                            <p><strong>Ngày đặt:</strong> {order.OrderDate:dd/MM/yyyy HH:mm}</p>

                            <h3 style='margin:24px 0 10px;'>Chi tiết đơn hàng</h3>

                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0'
                                   style='border-collapse:collapse;font-size:13px;'>
                                <tr style='background:#fff0f1;'>
                                    <th align='left' style='padding:12px 8px;'>Sản phẩm</th>
                                    <th style='padding:12px 8px;'>SL</th>
                                    <th align='right' style='padding:12px 8px;'>Đơn giá</th>
                                    <th align='right' style='padding:12px 8px;'>Thành tiền</th>
                                </tr>
                                {productRows}
                            </table>

                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0'
                                   style='margin-top:18px;font-size:14px;'>
                                <tr>
                                    <td style='padding:6px 0;'>Tạm tính</td>
                                    <td align='right' style='padding:6px 0;'>{order.TotalAmount:N0}đ</td>
                                </tr>
                                <tr>
                                    <td style='padding:6px 0;'>Giảm giá</td>
                                    <td align='right' style='padding:6px 0;'>-{order.DiscountAmount:N0}đ</td>
                                </tr>
                                <tr>
                                    <td style='padding:12px 0;border-top:1px solid #ddd;font-weight:bold;'>
                                        Tổng thanh toán
                                    </td>
                                    <td align='right'
                                        style='padding:12px 0;border-top:1px solid #ddd;font-weight:bold;color:#d70018;font-size:18px;'>
                                        {order.FinalAmount:N0}đ
                                    </td>
                                </tr>
                            </table>

                            <h3 style='margin:24px 0 10px;'>Thông tin nhận hàng</h3>
                            <p style='margin:6px 0;'>
                                <strong>Địa chỉ:</strong>
                                {System.Web.HttpUtility.HtmlEncode(order.ShippingAddress ?? "")}
                            </p>
                            <p style='margin:6px 0;'>
                                <strong>Số điện thoại:</strong>
                                {System.Web.HttpUtility.HtmlEncode(order.ReceiverPhone ?? "")}
                            </p>
                            <p style='margin:6px 0;'>
                                <strong>Thanh toán:</strong>
                                {System.Web.HttpUtility.HtmlEncode(order.PaymentMethod ?? "")}
                            </p>

                            <p style='margin-top:24px;'>
                                Cảm ơn bạn đã mua sắm tại The Chill Shop!
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:18px 24px;background:#f8f8f8;text-align:center;font-size:12px;color:#777;'>
                            The Chill Shop · Công nghệ trong tầm tay
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                            await emailService.SendAsync(new IdentityMessage
                            {
                                Destination = order.User.Email,
                                Subject = $"Xác nhận đơn hàng #{order.OrderID} - The Chill Shop",
                                Body = htmlBody
                            });
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Email lỗi không làm đảo ngược đơn hàng đã xác nhận.
                        System.Diagnostics.Debug.WriteLine(
                            $"[Order Confirmation Email Error] Order #{orderId}: {emailEx}"
                        );
                    }
                }


                if (updateSucceeded && newStatus == "Cancelled")
                {
                    try
                    {
                        if (order.User != null &&
                            !string.IsNullOrWhiteSpace(order.User.Email))
                        {
                            var emailService = new EmailService();

                            string customerName =
                                System.Web.HttpUtility.HtmlEncode(
                                    order.User.FullName ?? "Quý khách");

                            string safeReason =
                                System.Web.HttpUtility.HtmlEncode(
                                    order.CancellationReason ?? "");

                            string htmlBody = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0;padding:0;background:#f4f5f7;
             font-family:Arial,Helvetica,sans-serif;color:#333;'>
    <table role='presentation' width='100%' cellpadding='0' cellspacing='0'
           style='background:#f4f5f7;padding:24px 10px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='600' cellpadding='0' cellspacing='0'
                       style='width:100%;max-width:600px;background:#fff;
                              border-radius:12px;overflow:hidden;'>
                    <tr>
                        <td style='background:#d70018;padding:26px 24px;
                                   text-align:center;color:#fff;'>
                            <h1 style='margin:0;font-size:25px;'>The Chill Shop</h1>
                            <p style='margin:8px 0 0;font-size:14px;'>
                                Công nghệ trong tầm tay
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:28px 24px;'>
                            <h2 style='margin:0 0 16px;color:#d70018;font-size:22px;'>
                                Thông báo hủy đơn hàng
                            </h2>

                            <p>Xin chào <strong>{customerName}</strong>,</p>

                            <p>
                                Chúng tôi rất tiếc phải thông báo rằng đơn hàng
                                của bạn đã được hủy bởi bộ phận quản lý của
                                The Chill Shop.
                            </p>

                            <div style='background:#fff0f1;border-radius:8px;
                                        padding:16px;margin:20px 0;'>
                                <p style='margin:0 0 10px;'>
                                    <strong>Mã đơn hàng:</strong> #{order.OrderID}
                                </p>
                                <p style='margin:0 0 10px;'>
                                    <strong>Ngày đặt:</strong>
                                    {order.OrderDate.ToLocalTime():dd/MM/yyyy HH:mm}
                                </p>
                                <p style='margin:0 0 8px;'>
                                    <strong>Lý do hủy:</strong>
                                </p>
                                <p style='margin:0;white-space:pre-line;'>
                                    {safeReason}
                                </p>
                            </div>

                            <p>
                                Chúng tôi thành thật xin lỗi vì sự bất tiện này.
                                Nếu bạn cần được hỗ trợ hoặc có thắc mắc về đơn
                                hàng, vui lòng liên hệ bộ phận chăm sóc khách hàng
                                của The Chill Shop.
                            </p>

                            <p>
                                Cảm ơn bạn đã thông cảm và quan tâm đến
                                The Chill Shop.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:18px 24px;background:#f8f8f8;
                                   text-align:center;font-size:12px;color:#777;'>
                            The Chill Shop · Công nghệ trong tầm tay
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                            await emailService.SendAsync(new IdentityMessage
                            {
                                Destination = order.User.Email,
                                Subject = $"Thông báo hủy đơn hàng #{order.OrderID} - The Chill Shop",
                                Body = htmlBody
                            });
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Email thất bại không làm hoàn tác đơn hàng đã hủy.
                        System.Diagnostics.Debug.WriteLine(
                            $"[Order Cancellation Email Error] Order #{orderId}: {emailEx}");
                    }
                }

               
                    // ==============================================
                    // EMAIL CẢM ƠN KHI ĐƠN HÀNG ĐÃ GIAO THÀNH CÔNG
                    // ==============================================
                if (updateSucceeded && newStatus == "Delivered")
                {
                    try
                    {
                        if (order.User != null &&
                            !string.IsNullOrWhiteSpace(order.User.Email))
                        {
                            var emailService = new EmailService();

                            string customerName =
                                System.Web.HttpUtility.HtmlEncode(
                                    order.User.FullName ?? "Quý khách");

                            string orderDate =
                                order.OrderDate.ToLocalTime()
                                    .ToString("dd/MM/yyyy HH:mm");

                            string htmlBody = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport'
          content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0;padding:0;background:#f4f5f7;
             font-family:Arial,Helvetica,sans-serif;color:#333;'>
    <table role='presentation' width='100%'
           cellpadding='0' cellspacing='0'
           style='background:#f4f5f7;padding:24px 10px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='600'
                       cellpadding='0' cellspacing='0'
                       style='width:100%;max-width:600px;
                              background:#fff;border-radius:12px;
                              overflow:hidden;'>
                    <tr>
                        <td style='background:#d70018;
                                   padding:26px 24px;
                                   text-align:center;color:#fff;'>
                            <h1 style='margin:0;font-size:25px;'>
                                The Chill Shop
                            </h1>
                            <p style='margin:8px 0 0;font-size:14px;'>
                                Công nghệ trong tầm tay
                            </p>
                        </td>
                    </tr>

                    <tr>
                        <td style='padding:28px 24px;'>
                            <h2 style='margin:0 0 16px;
                                       color:#16803d;font-size:22px;'>
                                Đơn hàng đã được giao thành công!
                            </h2>

                            <p>
                                Xin chào <strong>{customerName}</strong>,
                            </p>

                            <p>
                                The Chill Shop xin thông báo rằng đơn hàng
                                của bạn đã được cập nhật sang trạng thái
                                giao hàng thành công.
                            </p>

                            <div style='background:#f0fdf4;
                                        border:1px solid #bbf7d0;
                                        border-radius:8px;
                                        padding:18px;margin:20px 0;'>
                                <p style='margin:0 0 10px;'>
                                    <strong>Mã đơn hàng:</strong>
                                    #{order.OrderID}
                                </p>

                                <p style='margin:0 0 10px;'>
                                    <strong>Ngày đặt:</strong>
                                    {orderDate}
                                </p>

                                <p style='margin:0 0 10px;'>
                                    <strong>Trạng thái:</strong>
                                    Đã giao thành công
                                </p>

                                <p style='margin:0;'>
                                    <strong>Tổng thanh toán:</strong>
                                    <span style='color:#d70018;
                                                 font-size:18px;
                                                 font-weight:bold;'>
                                        {order.FinalAmount:N0}đ
                                    </span>
                                </p>
                            </div>

                            <p>
                                Cảm ơn bạn đã tin tưởng và lựa chọn
                                The Chill Shop. Sự hài lòng của bạn
                                là động lực để chúng tôi không ngừng
                                cải thiện chất lượng sản phẩm và dịch vụ.
                            </p>

                            <p>
                                Hy vọng bạn hài lòng với sản phẩm đã nhận.
                                Chúng tôi rất mong được phục vụ bạn
                                trong những lần mua sắm tiếp theo.
                            </p>

                            <p style='margin-top:24px;'>
                                Trân trọng,<br>
                                <strong>Đội ngũ The Chill Shop</strong>
                            </p>
                        </td>
                    </tr>

                    <tr>
                        <td style='padding:18px 24px;
                                   background:#f8f8f8;
                                   text-align:center;
                                   font-size:12px;color:#777;'>
                            The Chill Shop · Công nghệ trong tầm tay
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                            await emailService.SendAsync(new IdentityMessage
                            {
                                Destination = order.User.Email,
                                Subject =
                                    $"Cảm ơn bạn đã mua hàng - Đơn #{order.OrderID} | The Chill Shop",
                                Body = htmlBody
                            });
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Email lỗi không làm hoàn tác trạng thái Delivered.
                        System.Diagnostics.Debug.WriteLine(
                            $"[Order Delivered Email Error] Order #{orderId}: {emailEx}");
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

        // ==============================================
        // POST: /AdminOrder/UpdatePaymentStatus
        // ==============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdatePaymentStatus(int orderId, string paymentStatus)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index");
                }

                if (paymentStatus != "Paid" && paymentStatus != "Unpaid")
                {
                    TempData["ErrorMessage"] = "Trạng thái thanh toán không hợp lệ.";
                    return RedirectToAction("Detail", new { id = orderId });
                }

                // Không cho phép hoàn tác về Unpaid khi đơn hàng đã xuất kho hoặc đã hoàn thành
                if (paymentStatus == "Unpaid" && (order.Status == "Shipped" || order.Status == "Delivered"))
                {
                    TempData["ErrorMessage"] = $"Không thể chuyển trạng thái thanh toán về \"Chưa thanh toán\" khi đơn hàng đang ở trạng thái \"{order.Status}\".";
                    return RedirectToAction("Detail", new { id = orderId });
                }

                order.PaymentStatus = paymentStatus;
                order.PaymentDate = paymentStatus == "Paid" ? DateTime.UtcNow : (DateTime?)null;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"Đã cập nhật trạng thái thanh toán đơn #{orderId} thành \"{(paymentStatus == "Paid" ? "Đã thanh toán" : "Chưa thanh toán")}\".";

                return RedirectToAction("Detail", new { id = orderId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminOrder/UpdatePaymentStatus] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật trạng thái thanh toán.";
                return RedirectToAction("Detail", new { id = orderId });
            }
        }

        /// <summary>
        /// Kiểm tra transition hợp lệ
        /// Pending -> Confirmed -> (Processing) -> Shipped -> Delivered
        /// Pending/Confirmed/Processing/Shipped -> Cancelled
        /// </summary>
        private bool IsValidTransition(string currentStatus, string newStatus)
        {
            switch (currentStatus)
            {
                case "Pending":
                    return newStatus == "Confirmed" || newStatus == "Cancelled";
                case "Confirmed":
                    return newStatus == "Processing" || newStatus == "Shipped" || newStatus == "Cancelled";
                case "Processing":
                    return newStatus == "Shipped" || newStatus == "Cancelled";
                case "Shipped":
                    return newStatus == "Delivered" || newStatus == "Cancelled";
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
