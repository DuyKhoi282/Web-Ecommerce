using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Helpers;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    [AllowAnonymous]
    public class SepayWebhookController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SepayWebhookController()
        {
            _context = new ApplicationDbContext();
        }

        public class SepayWebhookModel
        {
            public long id { get; set; }
            public string gateway { get; set; }
            public string transactionDate { get; set; }
            public string accountNumber { get; set; }
            public string code { get; set; }
            public string content { get; set; }
            public string transferType { get; set; }
            public decimal transferAmount { get; set; }
            public decimal accumulated { get; set; }
            public string subAccount { get; set; }
            public string referenceCode { get; set; }
            public string description { get; set; }
        }

        // ==============================================
        // POST: /SepayWebhook
        // ==============================================
        [HttpPost]
        public async Task<ActionResult> Index()
        {
            try
            {
                // 1. Kiểm tra API Key xác thực nếu đã cấu hình
                string configuredKey = SepayHelper.ApiKey?.Trim();
                if (!string.IsNullOrEmpty(configuredKey))
                {
                    string authHeader = Request.Headers["Authorization"] ?? "";
                    string queryKey = Request.QueryString["apiKey"] ?? "";
                    bool isAuthorized = authHeader.Contains(configuredKey) || queryKey == configuredKey;
                    if (!isAuthorized)
                    {
                        Response.StatusCode = 401;
                        return Json(new { success = false, message = "Unauthorized: Invalid API Key" });
                    }
                }

                // 2. Đọc payload JSON gửi từ SePay
                string requestBody;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    return Json(new { success = false, message = "Empty payload" });
                }

                var payload = JsonConvert.DeserializeObject<SepayWebhookModel>(requestBody);
                if (payload == null)
                {
                    return Json(new { success = false, message = "Invalid JSON format" });
                }

                // Chỉ nhận giao dịch tiền vào (tiền vào tài khoản shop)
                if (!string.Equals(payload.transferType, "in", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = true, message = "Ignored: transferType is not 'in'" });
                }

                // 3. Trích xuất OrderID từ nội dung chuyển khoản hoặc mô tả
                int? orderId = SepayHelper.ExtractOrderId(payload.content, payload.description);
                if (!orderId.HasValue)
                {
                    return Json(new { success = true, message = "Ignored: No Order ID detected in transfer content" });
                }

                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails.Select(od => od.Product))
                    .FirstOrDefaultAsync(o => o.OrderID == orderId.Value);

                if (order == null)
                {
                    return Json(new { success = false, message = $"Order #{orderId.Value} not found" });
                }

                // Nếu đơn đã được xác nhận thanh toán rồi -> Trả 200 idempotent
                if (order.PaymentStatus == "Paid")
                {
                    return Json(new { success = true, message = $"Order #{orderId.Value} was already Paid" });
                }

                // Nếu đơn bị hủy trước đó do quá hạn 15 phút, nhưng khách vẫn chuyển khoản đến:
                if (order.Status == "Cancelled")
                {
                    string noteLate = $"[Cảnh báo SePay] Khách chuyển khoản sau khi đơn đã hủy. Số tiền: {payload.transferAmount:N0}đ (GD #{payload.id}, Ref: {payload.referenceCode}). Cần liên hệ khách để xử lý.";
                    order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? noteLate : $"{order.Notes}\n{noteLate}";
                    order.PaymentStatus = "Paid";
                    order.PaymentDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Order was cancelled, logged late payment note for admin review" });
                }

                // Kiểm tra số tiền chuyển có đủ không (cho phép sai số nhỏ nếu có làm tròn)
                if (payload.transferAmount < order.FinalAmount)
                {
                    string noteUnderpaid = $"[Cảnh báo SePay] Nhận thiếu tiền: {payload.transferAmount:N0}đ / {order.FinalAmount:N0}đ (GD #{payload.id}).";
                    order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? noteUnderpaid : $"{order.Notes}\n{noteUnderpaid}";
                    await _context.SaveChangesAsync();
                    return Json(new { success = false, message = "Transfer amount is less than order amount" });
                }

                // 4. CẬP NHẬT TỰ ĐỘNG: Đã thanh toán & Xác nhận đơn hàng
                order.PaymentStatus = "Paid";
                order.PaymentDate = DateTime.UtcNow;

                if (order.Status == "Pending")
                {
                    order.Status = "Confirmed";
                }

                string autoNote = $"[SePay Tự Động] Khớp giao dịch thành công #{payload.id} - Số tiền: {payload.transferAmount:N0}đ - Ref: {payload.referenceCode} - Lúc {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC.";
                order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? autoNote : $"{order.Notes}\n{autoNote}";

                await _context.SaveChangesAsync();

                // 5. Gửi email xác nhận thanh toán thành công cho khách hàng
                try
                {
                    if (order.User != null && !string.IsNullOrWhiteSpace(order.User.Email))
                    {
                        var emailService = new EmailService();
                        string customerName = System.Web.HttpUtility.HtmlEncode(order.User.FullName ?? "Quý khách");

                        string emailHtml = $@"
<!DOCTYPE html>
<html lang='vi'>
<head><meta charset='UTF-8'></head>
<body style='margin:0;padding:0;background:#f4f5f7;font-family:Arial,Helvetica,sans-serif;color:#333;'>
    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#f4f5f7;padding:24px 10px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='600' cellpadding='0' cellspacing='0' style='width:100%;max-width:600px;background:#fff;border-radius:12px;overflow:hidden;'>
                    <tr>
                        <td style='background:#0d47a1;padding:26px 24px;text-align:center;color:#fff;'>
                            <h1 style='margin:0;font-size:24px;'>The Chill Shop</h1>
                            <p style='margin:6px 0 0;font-size:13px;opacity:0.9;'>Xác nhận thanh toán tự động</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:28px 24px;'>
                            <h2 style='margin:0 0 16px;color:#16a34a;font-size:20px;'>
                                Thanh toán đơn hàng #{order.OrderID} thành công!
                            </h2>
                            <p>Xin chào <strong>{customerName}</strong>,</p>
                            <p>Hệ thống vừa ghi nhận giao dịch chuyển khoản VietQR thành công cho đơn hàng <strong>#{order.OrderID}</strong> của bạn.</p>
                            
                            <div style='background:#f0fdf4;border:1px solid #bbf7d0;border-radius:10px;padding:16px;margin:20px 0;'>
                                <p style='margin:0 0 8px;'><strong>Mã đơn hàng:</strong> #{order.OrderID}</p>
                                <p style='margin:0 0 8px;'><strong>Số tiền đã thanh toán:</strong> <span style='color:#0d47a1;font-weight:bold;font-size:16px;'>{order.FinalAmount:N0}đ</span></p>
                                <p style='margin:0 0 8px;'><strong>Phương thức:</strong> Chuyển khoản VietQR / SePay</p>
                                <p style='margin:0;'><strong>Trạng thái:</strong> <span style='color:#16a34a;font-weight:bold;'>Đã xác nhận & Đang chuẩn bị hàng</span></p>
                            </div>

                            <p>Đội ngũ The Chill Shop đang tiến hành kiểm tra và đóng gói sản phẩm để chuyển giao cho đơn vị vận chuyển sớm nhất.</p>
                            <p style='margin-top:24px;'>Cảm ơn bạn đã tin tưởng mua sắm tại The Chill Shop!</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:16px 24px;background:#f8f8f8;text-align:center;font-size:12px;color:#888;'>
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
                            Subject = $"[The Chill Shop] Thanh toán thành công đơn hàng #{order.OrderID}",
                            Body = emailHtml
                        });
                    }
                }
                catch (Exception emailEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[SepayWebhook Email Error] {emailEx}");
                }

                return Json(new
                {
                    success = true,
                    message = $"Order #{order.OrderID} confirmed and marked as Paid",
                    orderId = order.OrderID
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SepayWebhook Exception] {ex}");
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
