using System;
using System.Data.Entity;
using System.Threading.Tasks;
using WebEcommerce.Models;

namespace WebEcommerce.Helpers
{
    public static class OrderCancellationHelper
    {
        /// <summary>
        /// Thực hiện hủy đơn hàng, hoàn trả tồn kho, hoàn suất Flash Sale và hoàn voucher
        /// </summary>
        public static bool CancelOrder(ApplicationDbContext context, Order order, string cancelledBy, string cancellationReason)
        {
            if (order == null || order.Status == "Cancelled")
                return false;

            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Hoàn trả tồn kho và suất Flash Sale cho từng sản phẩm
                    if (order.OrderDetails != null)
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            if (detail.ProductID > 0)
                            {
                                StockHelper.TryAdjust(context, detail.ProductID, detail.Quantity);

                                if (detail.FlashSaleItemID.HasValue)
                                {
                                    FlashSaleHelper.Release(context, detail.FlashSaleItemID.Value, detail.Quantity);
                                }
                            }
                        }
                    }

                    // 2. Hoàn lại lượt dùng mã giảm giá nếu có
                    if (order.PromoCodeID.HasValue)
                    {
                        var promo = context.PromotionalCodes.Find(order.PromoCodeID.Value);
                        if (promo != null && promo.CurrentUsage > 0)
                        {
                            promo.CurrentUsage -= 1;
                        }
                    }

                    // 3. Cập nhật thông tin hủy
                    order.Status = "Cancelled";
                    order.CancelledBy = cancelledBy ?? "System";
                    order.CancellationReason = cancellationReason ?? "Đơn hàng đã bị hủy.";
                    order.CancelledAt = DateTime.UtcNow;

                    context.SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"[OrderCancellationHelper.CancelOrder] {ex}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Async version của CancelOrder
        /// </summary>
        public static async Task<bool> CancelOrderAsync(ApplicationDbContext context, Order order, string cancelledBy, string cancellationReason)
        {
            if (order == null || order.Status == "Cancelled")
                return false;

            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    if (order.OrderDetails != null)
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            if (detail.ProductID > 0)
                            {
                                StockHelper.TryAdjust(context, detail.ProductID, detail.Quantity);

                                if (detail.FlashSaleItemID.HasValue)
                                {
                                    FlashSaleHelper.Release(context, detail.FlashSaleItemID.Value, detail.Quantity);
                                }
                            }
                        }
                    }

                    if (order.PromoCodeID.HasValue)
                    {
                        var promo = await context.PromotionalCodes.FindAsync(order.PromoCodeID.Value);
                        if (promo != null && promo.CurrentUsage > 0)
                        {
                            promo.CurrentUsage -= 1;
                        }
                    }

                    order.Status = "Cancelled";
                    order.CancelledBy = cancelledBy ?? "System";
                    order.CancellationReason = cancellationReason ?? "Đơn hàng đã bị hủy.";
                    order.CancelledAt = DateTime.UtcNow;

                    await context.SaveChangesAsync();
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"[OrderCancellationHelper.CancelOrderAsync] {ex}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Kiểm tra đơn chuyển khoản đã quá thời hạn (15 phút) chưa; nếu quá hạn thì tự động hủy và hoàn trả kho
        /// </summary>
        public static bool CheckAndCancelIfExpired(ApplicationDbContext context, Order order, int? timeoutMinutes = null)
        {
            if (order == null || order.Status != "Pending" || order.PaymentMethod != "BankTransfer" || order.PaymentStatus == "Paid")
                return false;

            int timeout = timeoutMinutes ?? SepayHelper.PaymentTimeoutMinutes;
            var expiredTime = order.OrderDate.AddMinutes(timeout);

            if (DateTime.UtcNow > expiredTime)
            {
                return CancelOrder(context, order, "System", $"Đơn hàng đã tự động hủy do quá hạn thời gian thanh toán ({timeout} phút).");
            }

            return false;
        }

        /// <summary>
        /// Async version của CheckAndCancelIfExpired
        /// </summary>
        public static async Task<bool> CheckAndCancelIfExpiredAsync(ApplicationDbContext context, Order order, int? timeoutMinutes = null)
        {
            if (order == null || order.Status != "Pending" || order.PaymentMethod != "BankTransfer" || order.PaymentStatus == "Paid")
                return false;

            int timeout = timeoutMinutes ?? SepayHelper.PaymentTimeoutMinutes;
            var expiredTime = order.OrderDate.AddMinutes(timeout);

            if (DateTime.UtcNow > expiredTime)
            {
                return await CancelOrderAsync(context, order, "System", $"Đơn hàng đã tự động hủy do quá hạn thời gian thanh toán ({timeout} phút).");
            }

            return false;
        }
    }
}
