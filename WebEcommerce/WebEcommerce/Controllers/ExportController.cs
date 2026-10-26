using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    [Authorize]
    public class ExportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExportController()
        {
            _context = new ApplicationDbContext();
        }

        // ==============================================
        // GET: /Export/Invoice/5 — tải hóa đơn PDF của đơn thuộc khách đang đăng nhập
        // ==============================================
        [HttpGet]
        public async Task<ActionResult> Invoice(int id)
        {
            try
            {
                var userId = User.Identity.GetUserId();

                var order = await _context.Orders
                    .Include(o => o.OrderDetails.Select(od => od.Product))
                    .Include(o => o.PromotionalCode)
                    .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index", "Order");
                }

                if (order.Status == "Cancelled")
                {
                    TempData["ErrorMessage"] = "Đơn hàng đã hủy nên không thể xuất hóa đơn.";
                    return RedirectToAction("Detail", "Order", new { id });
                }

                var pdf = BuildInvoicePdf(order, User.Identity.GetUserName());
                return File(pdf, "application/pdf", $"HoaDon_{order.OrderID}.pdf");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Export/Invoice] {ex}");
                TempData["ErrorMessage"] = "Không thể tạo hóa đơn PDF. Vui lòng thử lại sau.";
                return RedirectToAction("Detail", "Order", new { id });
            }
        }

        private static byte[] BuildInvoicePdf(Order order, string customerEmail)
        {
            // Font mặc định của iTextSharp không có dấu tiếng Việt → nhúng Arial Unicode của Windows
            var fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
            var boldPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arialbd.ttf");
            var baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            var baseBold = BaseFont.CreateFont(boldPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

            var primary = new BaseColor(0xD7, 0x00, 0x18);
            var normal = new Font(baseFont, 10);
            var bold = new Font(baseBold, 10);
            var title = new Font(baseBold, 20, Font.NORMAL, primary);
            var headerFont = new Font(baseBold, 10, Font.NORMAL, BaseColor.WHITE);
            var totalFont = new Font(baseBold, 12, Font.NORMAL, primary);

            using (var ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                doc.Add(new Paragraph("TheChillShop", title));
                doc.Add(new Paragraph("HÓA ĐƠN BÁN HÀNG", bold) { SpacingAfter = 12 });

                var info = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 16 };
                info.SetWidths(new float[] { 30, 70 });
                AddInfoRow(info, "Mã đơn hàng", "#" + order.OrderID, normal, bold);
                AddInfoRow(info, "Ngày đặt", order.OrderDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), normal, bold);
                AddInfoRow(info, "Khách hàng", customerEmail, normal, bold);
                AddInfoRow(info, "Số điện thoại", order.ReceiverPhone, normal, bold);
                AddInfoRow(info, "Địa chỉ giao hàng", order.ShippingAddress, normal, bold);
                AddInfoRow(info, "Trạng thái đơn", order.Status, normal, bold);
                AddInfoRow(info, "Thanh toán",
                    order.PaymentMethod == "BankTransfer" ? "Chuyển khoản VietQR"
                        : (order.PaymentMethod == "COD" ? "Thanh toán khi nhận hàng (COD)" : order.PaymentMethod),
                    normal, bold);
                AddInfoRow(info, "TT thanh toán", order.PaymentStatus == "Paid" ? "Đã thanh toán" : "Chờ thanh toán", normal, bold);
                doc.Add(info);

                var items = new PdfPTable(4) { WidthPercentage = 100, HeaderRows = 1 };
                items.SetWidths(new float[] { 50, 10, 20, 20 });
                foreach (var h in new[] { "Sản phẩm", "SL", "Đơn giá", "Thành tiền" })
                {
                    items.AddCell(new PdfPCell(new Phrase(h, headerFont))
                    {
                        BackgroundColor = primary,
                        Padding = 6,
                        HorizontalAlignment = h == "Sản phẩm" ? Element.ALIGN_LEFT : Element.ALIGN_RIGHT
                    });
                }
                foreach (var od in order.OrderDetails)
                {
                    items.AddCell(ItemCell(od.Product?.Name ?? "Sản phẩm", normal, Element.ALIGN_LEFT));
                    items.AddCell(ItemCell(od.Quantity.ToString(), normal, Element.ALIGN_RIGHT));
                    items.AddCell(ItemCell(Money(od.UnitPrice), normal, Element.ALIGN_RIGHT));
                    items.AddCell(ItemCell(Money(od.Subtotal), normal, Element.ALIGN_RIGHT));
                }
                doc.Add(items);

                var totals = new PdfPTable(2) { WidthPercentage = 50, HorizontalAlignment = Element.ALIGN_RIGHT, SpacingBefore = 12 };
                AddTotalRow(totals, "Tạm tính", Money(order.TotalAmount), normal, normal);
                if (order.DiscountAmount > 0)
                {
                    var label = order.PromotionalCode != null ? $"Giảm giá ({order.PromotionalCode.Code})" : "Giảm giá";
                    AddTotalRow(totals, label, "-" + Money(order.DiscountAmount), normal, normal);
                }
                AddTotalRow(totals, "Tổng thanh toán", Money(order.FinalAmount), bold, totalFont);
                doc.Add(totals);

                doc.Add(new Paragraph("Cảm ơn bạn đã mua sắm tại TheChillShop!", normal)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 30
                });

                doc.Close();
                return ms.ToArray();
            }
        }

        private static string Money(decimal value) => string.Format("{0:N0}đ", value);

        private static void AddInfoRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont)
        {
            table.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Border = Rectangle.NO_BORDER, PaddingBottom = 4 });
            table.AddCell(new PdfPCell(new Phrase(value ?? "", valueFont)) { Border = Rectangle.NO_BORDER, PaddingBottom = 4 });
        }

        private static PdfPCell ItemCell(string text, Font font, int align) =>
            new PdfPCell(new Phrase(text, font)) { Padding = 6, HorizontalAlignment = align, BorderColor = BaseColor.LIGHT_GRAY };

        private static void AddTotalRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont)
        {
            table.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Border = Rectangle.NO_BORDER, Padding = 4 });
            table.AddCell(new PdfPCell(new Phrase(value, valueFont)) { Border = Rectangle.NO_BORDER, Padding = 4, HorizontalAlignment = Element.ALIGN_RIGHT });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
