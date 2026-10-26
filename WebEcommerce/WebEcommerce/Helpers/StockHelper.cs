using System.Linq;
using WebEcommerce.Models;

namespace WebEcommerce.Helpers
{
    /// <summary>
    /// Cộng/trừ tồn kho nguyên tử ngay trong CSDL, tránh đọc-rồi-ghi qua EF làm 2 đơn cùng lúc bán vượt tồn
    /// hoặc ghi đè mất thay đổi của nhau.
    /// </summary>
    public static class StockHelper
    {
        /// <summary>
        /// Cộng delta (âm = trừ) vào tồn kho, tự chuyển Status 1 (Còn hàng) &lt;-&gt; 2 (Hết hàng).
        /// Trả về tồn kho mới, hoặc null nếu không đủ hàng / không có sản phẩm. Gọi trong transaction nếu nhiều bước.
        /// Entity Product đang được EF theo dõi không tự cập nhật: không gán StockQuantity/Status lên entity đó nữa.
        /// </summary>
        public static int? TryAdjust(ApplicationDbContext db, int productId, int delta)
        {
            var rows = db.Database.SqlQuery<int>(
                @"UPDATE ""Products"" SET ""StockQuantity"" = ""StockQuantity"" + {1},
                      ""Status"" = CASE WHEN ""StockQuantity"" + {1} = 0 AND ""Status"" = 1 THEN 2
                                        WHEN ""StockQuantity"" + {1} > 0 AND ""Status"" = 2 THEN 1
                                        ELSE ""Status"" END
                  WHERE ""ProductID"" = {0} AND ""StockQuantity"" + {1} >= 0
                  RETURNING ""StockQuantity""",
                productId, delta).ToList();
            return rows.Count == 1 ? rows[0] : (int?)null;
        }
    }
}
