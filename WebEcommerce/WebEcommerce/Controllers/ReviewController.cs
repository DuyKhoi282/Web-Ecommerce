
using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: /Review/CreateOrderReview/5
        // Hiển thị form đánh giá hoặc chỉnh sửa đánh giá đơn hàng và sản phẩm
        [HttpGet]
        public async Task<ActionResult> CreateOrderReview(int orderId)
        {
            var userId = User.Identity.GetUserId();

            var order = await _context.Orders
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefaultAsync(o =>
                    o.OrderID == orderId &&
                    o.UserID == userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Order");
            }

            if (order.Status != "Delivered")
            {
                TempData["ErrorMessage"] =
                    "Bạn chỉ có thể đánh giá đơn hàng đã giao thành công.";

                return RedirectToAction("Detail", "Order",
                    new { id = orderId });
            }

            // Lấy đánh giá tổng thể hiện có của đơn hàng.
            var existingOrderReview =
                await _context.OrderReviews.FirstOrDefaultAsync(r =>
                    r.OrderID == orderId &&
                    r.UserID == userId);

            // Lấy các đánh giá sản phẩm hiện có của đơn hàng.
            var existingProductReviews =
                await _context.Reviews
                    .Where(r =>
                        r.OrderID == orderId &&
                        r.UserID == userId)
                    .ToListAsync();

            // Truyền dữ liệu cũ sang View để điền vào form.
            ViewBag.OrderId = orderId;
            ViewBag.ExistingOrderReview = existingOrderReview;
            ViewBag.ExistingProductReviews = existingProductReviews;
            ViewBag.IsEdit = existingOrderReview != null;

            return View(order);
        }

        // POST: /Review/SubmitOrderReview
        // Tạo mới hoặc cập nhật đánh giá đơn hàng và sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SubmitOrderReview(
            int orderId,
            int orderRating,
            string orderComment,
            int[] productIds,
            int[] productRatings,
            string[] productComments)
        {
            var userId = User.Identity.GetUserId();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o =>
                    o.OrderID == orderId &&
                    o.UserID == userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Order");
            }

            if (order.Status != "Delivered")
            {
                TempData["ErrorMessage"] =
                    "Chỉ được đánh giá đơn hàng đã giao thành công.";

                return RedirectToAction("Detail", "Order",
                    new { id = orderId });
            }

            // Kiểm tra số sao tổng thể.
            if (orderRating < 1 || orderRating > 5)
            {
                TempData["ErrorMessage"] =
                    "Vui lòng chọn số sao tổng thể từ 1 đến 5.";

                return RedirectToAction("CreateOrderReview",
                    new { orderId });
            }

            // Chuẩn hóa các mảng nhận từ form.
            productIds = productIds ?? new int[0];
            productRatings = productRatings ?? new int[0];
            productComments = productComments ?? new string[0];

            // Đảm bảo dữ liệu của từng sản phẩm tương ứng với nhau.
            if (productIds.Length != productRatings.Length ||
                productIds.Length != productComments.Length)
            {
                TempData["ErrorMessage"] =
                    "Dữ liệu đánh giá sản phẩm không hợp lệ.";

                return RedirectToAction("CreateOrderReview",
                    new { orderId });
            }

            // Không cho gửi trùng một sản phẩm trong cùng form.
            if (productIds.Distinct().Count() != productIds.Length)
            {
                TempData["ErrorMessage"] =
                    "Danh sách sản phẩm đánh giá bị trùng.";

                return RedirectToAction("CreateOrderReview",
                    new { orderId });
            }

            // Chỉ chấp nhận sản phẩm thực sự thuộc đơn hàng này.
            var purchasedProductIds = order.OrderDetails
                .Select(d => d.ProductID)
                .Distinct()
                .ToList();

            if (productIds.Any(id =>
                !purchasedProductIds.Contains(id)))
            {
                TempData["ErrorMessage"] =
                    "Có sản phẩm không thuộc đơn hàng này.";

                return RedirectToAction("CreateOrderReview",
                    new { orderId });
            }

            // Kiểm tra số sao của tất cả sản phẩm.
            if (productRatings.Any(r => r < 1 || r > 5))
            {
                TempData["ErrorMessage"] =
                    "Số sao sản phẩm phải nằm trong khoảng từ 1 đến 5.";

                return RedirectToAction("CreateOrderReview",
                    new { orderId });
            }

            using (var transaction =
                _context.Database.BeginTransaction())
            {
                try
                {
                    // ==========================================
                    // 1. TẠO MỚI HOẶC CẬP NHẬT ĐÁNH GIÁ ĐƠN HÀNG
                    // ==========================================

                    var orderReview =
                        await _context.OrderReviews.FirstOrDefaultAsync(r =>
                            r.OrderID == orderId &&
                            r.UserID == userId);

                    if (orderReview == null)
                    {
                        // Chưa có đánh giá: tạo mới.
                        orderReview = new OrderReview
                        {
                            OrderID = order.OrderID,
                            UserID = userId,
                            CreatedAt = DateTime.UtcNow,
                            IsApproved = true
                        };

                        _context.OrderReviews.Add(orderReview);
                    }

                    // Đã có đánh giá: cập nhật nội dung.
                    orderReview.Rating = orderRating;

                    orderReview.Comment =
                        string.IsNullOrWhiteSpace(orderComment)
                            ? null
                            : orderComment.Trim();

                    orderReview.IsApproved = true;

                    // ==========================================
                    // 2. TẠO MỚI HOẶC CẬP NHẬT ĐÁNH GIÁ SẢN PHẨM
                    // ==========================================

                    for (int i = 0; i < productIds.Length; i++)
                    {
                        int currentProductId = productIds[i];
                        int currentRating = productRatings[i];
                        string currentComment = productComments[i];

                        // Tìm đánh giá hiện có của đúng người dùng,
                        // đúng đơn hàng và đúng sản phẩm.
                        var review =
                            await _context.Reviews.FirstOrDefaultAsync(r =>
                                r.OrderID == orderId &&
                                r.ProductID == currentProductId &&
                                r.UserID == userId);

                        if (review == null)
                        {
                            // Chưa đánh giá sản phẩm này: tạo mới.
                            review = new Review
                            {
                                OrderID = orderId,
                                ProductID = currentProductId,
                                UserID = userId,
                                CreatedAt = DateTime.UtcNow,
                                IsApproved = true
                            };

                            _context.Reviews.Add(review);
                        }

                        // Đánh giá đã tồn tại thì cập nhật số sao,
                        // bình luận và trạng thái duyệt.
                        review.Rating = currentRating;

                        review.Comment =
                            string.IsNullOrWhiteSpace(currentComment)
                                ? null
                                : currentComment.Trim();

                        review.IsApproved = true;
                    }

                    // ==========================================
                    // 3. LƯU TẤT CẢ THAY ĐỔI
                    // ==========================================

                    await _context.SaveChangesAsync();

                    transaction.Commit();

                    TempData["SuccessMessage"] =
                        "Đánh giá đơn hàng và sản phẩm đã được lưu thành công!";

                    return RedirectToAction("Detail", "Order",
                        new { id = orderId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    System.Diagnostics.Debug.WriteLine(
                        $"[Review/SubmitOrderReview] {ex}");

                    TempData["ErrorMessage"] =
                        "Không thể lưu đánh giá. Vui lòng thử lại.";

                    return RedirectToAction("CreateOrderReview",
                        new { orderId });
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
