using System;
using System.Data.Entity;
using System.Linq;
using WebEcommerce.Models;

namespace WebEcommerce.Helpers
{
    /// <summary>
    /// Quy tắc giá Flash Sale dùng chung cho Cart và Order.
    /// Giá flash chỉ áp dụng khi suất còn lại đủ cho toàn bộ số lượng của dòng hàng.
    /// </summary>
    public static class FlashSaleHelper
    {
        /// <summary>
        /// Suất flash đang diễn ra của sản phẩm còn đủ chỗ cho quantity; trùng nhiều đợt thì lấy giá rẻ nhất.
        /// </summary>
        public static FlashSaleItem FindActiveItem(ApplicationDbContext db, int productId, int quantity)
        {
            var now = DateTime.UtcNow;
            return db.FlashSaleItems
                .AsNoTracking()
                .Where(f => f.ProductID == productId
                         && f.FlashSale.IsActive
                         && f.FlashSale.StartTime <= now
                         && f.FlashSale.EndTime > now
                         && f.MaxQuantity - f.SoldQuantity >= quantity)
                .OrderBy(f => f.FlashSalePrice)
                .ThenBy(f => f.FlashSaleItemID)
                .FirstOrDefault();
        }

        public static decimal GetUnitPrice(ApplicationDbContext db, Product product, int quantity)
        {
            return FindActiveItem(db, product.ProductID, quantity)?.FlashSalePrice ?? GetRegularPrice(product);
        }

        /// <summary>Giá bán khi không có Flash Sale: giá khuyến mãi nếu có, ngược lại giá gốc.</summary>
        public static decimal GetRegularPrice(Product product)
        {
            if (product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0)
                return product.DiscountPrice.Value;
            return product.Price;
        }

        /// <summary>
        /// Giữ suất flash nguyên tử ngay trong CSDL (chống 2 đơn cùng lúc vượt MaxQuantity).
        /// Trả về false nếu suất đã bị đơn khác dùng hết; gọi trong transaction của đơn hàng.
        /// </summary>
        public static bool TryReserve(ApplicationDbContext db, int flashSaleItemId, int quantity)
        {
            return db.Database.ExecuteSqlCommand(
                @"UPDATE ""FlashSaleItems"" SET ""SoldQuantity"" = ""SoldQuantity"" + {1}
                  WHERE ""FlashSaleItemID"" = {0} AND ""SoldQuantity"" + {1} <= ""MaxQuantity""",
                flashSaleItemId, quantity) == 1;
        }

        /// <summary>
        /// Hoàn suất flash khi hủy đơn, vào đúng FlashSaleItem đơn đã giữ (không âm); gọi trong transaction hủy đơn.
        /// </summary>
        public static void Release(ApplicationDbContext db, int flashSaleItemId, int quantity)
        {
            db.Database.ExecuteSqlCommand(
                @"UPDATE ""FlashSaleItems"" SET ""SoldQuantity"" = GREATEST(""SoldQuantity"" - {1}, 0)
                  WHERE ""FlashSaleItemID"" = {0}",
                flashSaleItemId, quantity);
        }
    }
}
