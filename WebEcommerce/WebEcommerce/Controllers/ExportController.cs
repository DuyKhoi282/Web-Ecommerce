using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using Microsoft.AspNet.Identity;
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
    [Authorize]
    public class ExportController : Controller
    {
        // Thông tin người bán lấy theo footer của _Layout.cshtml
        private const string CompanyName = "Công ty TNHH Thương mại Công nghệ TheChillShop";
        private const string CompanyAddress = "350-352 Võ Văn Kiệt, Phường Cô Giang, Quận 1, TP. Hồ Chí Minh";
        private const string CompanyHotline = "Hotline: 1800.2097 (7h30 - 22h00)";

        private static readonly BaseColor Primary = new BaseColor(0xD7, 0x00, 0x18);
        private static readonly BaseColor BoxBackground = new BaseColor(0xF2, 0xF3, 0xFF);
        private static readonly BaseColor TotalBackground = new BaseColor(0xFF, 0xE7, 0xE4);
        private static readonly BaseColor Muted = new BaseColor(0x64, 0x74, 0x8B);
        private static readonly BaseColor Divider = new BaseColor(0xE2, 0xE8, 0xF0);

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
                    .Include(o => o.User)
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

                var pdf = BuildInvoicePdf(order);
                return File(pdf, "application/pdf", $"HoaDon_{order.OrderID}.pdf");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Export/Invoice] {ex}");
                TempData["ErrorMessage"] = "Không thể tạo hóa đơn PDF. Vui lòng thử lại sau.";
                return RedirectToAction("Detail", "Order", new { id });
            }
        }

        private static byte[] BuildInvoicePdf(Order order)
        {
            // Font mặc định của iTextSharp không có dấu tiếng Việt → nhúng Arial Unicode của Windows
            var fontDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            var baseFont = BaseFont.CreateFont(Path.Combine(fontDir, "arial.ttf"), BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            var baseBold = BaseFont.CreateFont(Path.Combine(fontDir, "arialbd.ttf"), BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

            var normal = new Font(baseFont, 10);
            var bold = new Font(baseBold, 10);
            var small = new Font(baseFont, 9, Font.NORMAL, Muted);
            var boxTitle = new Font(baseBold, 9, Font.NORMAL, Primary);
            var headerFont = new Font(baseBold, 10, Font.NORMAL, BaseColor.WHITE);

            using (var ms = new MemoryStream())
            {
                // Lề dưới chừa chỗ cho chân trang cố định
                var doc = new Document(PageSize.A4, 40, 40, 40, 110);
                var writer = PdfWriter.GetInstance(doc, ms);
                writer.PageEvent = new InvoiceFooter(baseFont, baseBold);
                doc.Open();

                // --- Header: người bán (trái) + tiêu đề hóa đơn (phải)
                var header = new PdfPTable(2) { WidthPercentage = 100 };
                header.SetWidths(new float[] { 58, 42 });
                var seller = NoBorderCell();
                seller.AddElement(new Paragraph("TheChillShop", new Font(baseBold, 22, Font.NORMAL, Primary)));
                seller.AddElement(new Paragraph(CompanyName, small));
                seller.AddElement(new Paragraph(CompanyAddress, small));
                seller.AddElement(new Paragraph(CompanyHotline, small));
                header.AddCell(seller);
                var invoiceTitle = NoBorderCell();
                invoiceTitle.AddElement(new Paragraph("HÓA ĐƠN BÁN HÀNG", new Font(baseBold, 15)) { Alignment = Element.ALIGN_RIGHT });
                invoiceTitle.AddElement(new Paragraph("Số: HD" + order.OrderID.ToString("D6"), normal) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 4 });
                invoiceTitle.AddElement(new Paragraph("Ngày xuất: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), small) { Alignment = Element.ALIGN_RIGHT });
                header.AddCell(invoiceTitle);
                doc.Add(header);

                doc.Add(new Paragraph(new Chunk(new LineSeparator(1.5f, 100, Primary, Element.ALIGN_CENTER, -4))) { SpacingAfter = 14 });

                // --- Hai ô thông tin: khách hàng | đơn hàng
                var customerBox = InfoBox("KHÁCH HÀNG", boxTitle, normal, bold,
                    Tuple.Create("Họ tên", string.IsNullOrWhiteSpace(order.User?.FullName) ? "—" : order.User.FullName),
                    Tuple.Create("Email", order.User?.Email),
                    Tuple.Create("Điện thoại", order.ReceiverPhone),
                    Tuple.Create("Địa chỉ", order.ShippingAddress));
                var paymentStatus = order.PaymentStatus == "Paid"
                    ? "Đã thanh toán" + (order.PaymentDate.HasValue ? " (" + order.PaymentDate.Value.ToLocalTime().ToString("dd/MM/yyyy") + ")" : "")
                    : "Chờ thanh toán";
                var orderBox = InfoBox("ĐƠN HÀNG", boxTitle, normal, bold,
                    Tuple.Create("Mã đơn", "#" + order.OrderID),
                    Tuple.Create("Ngày đặt", order.OrderDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm")),
                    Tuple.Create("Trạng thái", StatusText(order.Status)),
                    Tuple.Create("Thanh toán", order.PaymentMethod == "BankTransfer" ? "Chuyển khoản VietQR"
                        : (order.PaymentMethod == "COD" ? "Khi nhận hàng (COD)" : order.PaymentMethod)),
                    Tuple.Create("TT thanh toán", paymentStatus));
                var boxes = new PdfPTable(3) { WidthPercentage = 100, SpacingAfter = 18 };
                boxes.SetWidths(new float[] { 49, 2, 49 });
                boxes.AddCell(customerBox);
                boxes.AddCell(NoBorderCell());
                boxes.AddCell(orderBox);
                doc.Add(boxes);

                // --- Bảng sản phẩm
                var items = new PdfPTable(5) { WidthPercentage = 100, HeaderRows = 1 };
                items.SetWidths(new float[] { 7, 45, 8, 20, 20 });
                var headers = new[] { "STT", "Sản phẩm", "SL", "Đơn giá", "Thành tiền" };
                for (var i = 0; i < headers.Length; i++)
                {
                    items.AddCell(new PdfPCell(new Phrase(headers[i], headerFont))
                    {
                        BackgroundColor = Primary,
                        Border = Rectangle.NO_BORDER,
                        Padding = 7,
                        HorizontalAlignment = i == 1 ? Element.ALIGN_LEFT : (i == 0 ? Element.ALIGN_CENTER : Element.ALIGN_RIGHT)
                    });
                }
                var row = 0;
                foreach (var od in order.OrderDetails)
                {
                    var background = row % 2 == 1 ? BoxBackground : BaseColor.WHITE;
                    row++;
                    items.AddCell(ItemCell(row.ToString(), normal, Element.ALIGN_CENTER, background));
                    items.AddCell(ItemCell(od.Product?.Name ?? "Sản phẩm", normal, Element.ALIGN_LEFT, background));
                    items.AddCell(ItemCell(od.Quantity.ToString(), normal, Element.ALIGN_RIGHT, background));
                    items.AddCell(ItemCell(Money(od.UnitPrice), normal, Element.ALIGN_RIGHT, background));
                    items.AddCell(ItemCell(Money(od.Subtotal), normal, Element.ALIGN_RIGHT, background));
                }
                doc.Add(items);

                // --- Tổng tiền
                var totals = new PdfPTable(2) { WidthPercentage = 50, HorizontalAlignment = Element.ALIGN_RIGHT, SpacingBefore = 12 };
                AddTotalRow(totals, "Tạm tính", Money(order.TotalAmount), normal, normal, null);
                if (order.DiscountAmount > 0)
                {
                    var label = order.PromotionalCode != null ? $"Giảm giá ({order.PromotionalCode.Code})" : "Giảm giá";
                    AddTotalRow(totals, label, "-" + Money(order.DiscountAmount), normal, normal, null);
                }
                // Checkout hiện không tính phí vận chuyển
                AddTotalRow(totals, "Phí vận chuyển", "Miễn phí", normal, normal, null);
                AddTotalRow(totals, "TỔNG THANH TOÁN", Money(order.FinalAmount), bold, new Font(baseBold, 13, Font.NORMAL, Primary), TotalBackground);
                doc.Add(totals);

                doc.Add(new Paragraph("Bằng chữ: " + VietnameseNumberHelper.ToCurrencyText(order.FinalAmount), small)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingBefore = 6
                });

                if (!string.IsNullOrWhiteSpace(order.Notes))
                {
                    var notes = new Paragraph { SpacingBefore = 16 };
                    notes.Add(new Chunk("Ghi chú: ", bold));
                    notes.Add(new Chunk(order.Notes, normal));
                    doc.Add(notes);
                }

                // --- Ô ký tên, giữ liền một khối để không bị tách sang trang sau
                var signatures = new PdfPTable(2) { WidthPercentage = 100, SpacingBefore = 28, KeepTogether = true };
                signatures.AddCell(SignatureCell("NGƯỜI MUA HÀNG", order.User?.FullName, bold, small, normal));
                signatures.AddCell(SignatureCell("NGƯỜI BÁN HÀNG", "TheChillShop", bold, small, normal));
                doc.Add(signatures);

                doc.Close();
                return ms.ToArray();
            }
        }

        private static string Money(decimal value) => string.Format("{0:N0}đ", value);

        private static string StatusText(string status)
        {
            switch (status)
            {
                case "Pending": return "Chờ xác nhận";
                case "Confirmed": return "Đã xác nhận";
                case "Processing": return "Đang chuẩn bị hàng";
                case "Shipped": return "Đang giao hàng";
                case "Delivered": return "Đã giao hàng";
                default: return status;
            }
        }

        private static PdfPCell NoBorderCell() => new PdfPCell { Border = Rectangle.NO_BORDER, Padding = 0 };

        private static PdfPCell InfoBox(string title, Font titleFont, Font labelFont, Font valueFont, params Tuple<string, string>[] rows)
        {
            var table = new PdfPTable(2) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 34, 66 });
            table.AddCell(new PdfPCell(new Phrase(title, titleFont)) { Colspan = 2, Border = Rectangle.NO_BORDER, PaddingBottom = 6 });
            foreach (var r in rows)
            {
                table.AddCell(new PdfPCell(new Phrase(r.Item1, labelFont)) { Border = Rectangle.NO_BORDER, PaddingBottom = 4 });
                table.AddCell(new PdfPCell(new Phrase(r.Item2 ?? "", valueFont)) { Border = Rectangle.NO_BORDER, PaddingBottom = 4 });
            }
            var box = new PdfPCell { BackgroundColor = BoxBackground, Border = Rectangle.NO_BORDER, Padding = 10 };
            box.AddElement(table);
            return box;
        }

        private static PdfPCell SignatureCell(string title, string name, Font titleFont, Font hintFont, Font nameFont)
        {
            var cell = new PdfPCell { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER };
            cell.AddElement(new Paragraph(title, titleFont) { Alignment = Element.ALIGN_CENTER });
            cell.AddElement(new Paragraph("(Ký, ghi rõ họ tên)", hintFont) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 48 });
            cell.AddElement(new Paragraph(name ?? "", nameFont) { Alignment = Element.ALIGN_CENTER });
            return cell;
        }

        private static PdfPCell ItemCell(string text, Font font, int align, BaseColor background) =>
            new PdfPCell(new Phrase(text, font))
            {
                Padding = 7,
                HorizontalAlignment = align,
                BackgroundColor = background,
                Border = Rectangle.BOTTOM_BORDER,
                BorderColor = Divider
            };

        private static void AddTotalRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont, BaseColor background)
        {
            table.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Border = Rectangle.NO_BORDER, Padding = 5, BackgroundColor = background, VerticalAlignment = Element.ALIGN_MIDDLE });
            table.AddCell(new PdfPCell(new Phrase(value, valueFont)) { Border = Rectangle.NO_BORDER, Padding = 5, BackgroundColor = background, HorizontalAlignment = Element.ALIGN_RIGHT, VerticalAlignment = Element.ALIGN_MIDDLE });
        }

        // Chân trang vẽ ở đáy mọi trang (đơn nhiều sản phẩm vẫn có footer ở từng trang)
        private class InvoiceFooter : PdfPageEventHelper
        {
            private readonly Font _thanks;
            private readonly Font _line;
            private readonly Font _note;

            public InvoiceFooter(BaseFont baseFont, BaseFont baseBold)
            {
                _thanks = new Font(baseBold, 10, Font.NORMAL, Primary);
                _line = new Font(baseFont, 8.5f);
                _note = new Font(baseFont, 8, Font.NORMAL, Muted);
            }

            public override void OnEndPage(PdfWriter writer, Document document)
            {
                var cb = writer.DirectContent;
                var center = (document.Left + document.Right) / 2;
                var top = document.BottomMargin - 20;

                cb.SetColorStroke(Divider);
                cb.SetLineWidth(0.8f);
                cb.MoveTo(document.Left, top);
                cb.LineTo(document.Right, top);
                cb.Stroke();

                ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER, new Phrase("Cảm ơn bạn đã mua sắm tại TheChillShop!", _thanks), center, top - 18, 0);
                ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER, new Phrase("100% hàng chính hãng  •  Bảo hành 1 đổi 1 trong 30 ngày lỗi nhà sản xuất", _line), center, top - 34, 0);
                ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER, new Phrase("Mua hàng 1800.2097  •  Khiếu nại 1800.2063  •  Bảo hành 1800.2064", _line), center, top - 47, 0);
                ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER, new Phrase("Chứng từ được tạo tự động từ hệ thống TheChillShop, không thay thế hóa đơn GTGT.", _note), center, top - 62, 0);
            }
        }
    }
}
