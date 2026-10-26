using System;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using WebEcommerce.Models;

namespace WebEcommerce
{
    public static class DatabaseConfig
    {
        public static void Initialize()
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    // 1. Tự động cập nhật bảng Orders (các cột phục vụ hủy đơn)
                    db.Database.ExecuteSqlCommand(@"
                        ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""CancellationReason"" VARCHAR(1000);
                        ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""CancelledBy"" VARCHAR(50);
                        ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""CancelledAt"" TIMESTAMP;
                    ");

                    // 2. Tự động cập nhật bộ ảnh sản phẩm chuẩn nếu chưa có hoặc còn link lỗi
                    var totalImages = db.Database.SqlQuery<int>(@"SELECT COUNT(*)::int FROM ""ProductImages""").FirstOrDefault();
                    var invalidImages = db.Database.SqlQuery<int>(@"SELECT COUNT(*)::int FROM ""ProductImages"" WHERE ""ImageURL"" LIKE '%upload.wikimedia.org%'").FirstOrDefault();

                    if ((totalImages == 0 || invalidImages > 0)
                        && RunScriptFile(db, "05_Update_Authentic_Product_Images.sql"))
                    {
                        System.Diagnostics.Debug.WriteLine("[DatabaseConfig] Cập nhật hình ảnh sản phẩm thành công!");
                    }

                    // 3. Script idempotent (chạy lại nhiều lần không lỗi) nên chạy mỗi lần khởi động
                    RunScriptFile(db, "06_Create_OrderReviews.sql");
                    // 08 trước 07: FK của 07 trỏ tới "FlashSaleItems" do 08 tạo
                    RunScriptFile(db, "08_Ensure_FlashSale_Tables.sql");
                    RunScriptFile(db, "07_Add_OrderDetails_FlashSaleItemID.sql");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[DatabaseConfig AutoMigration Error] {ex}");
            }
        }

        // Tìm script trong App_Data (bản deploy) rồi tới Database/Scripts (máy dev); không thấy file thì bỏ qua
        private static bool RunScriptFile(ApplicationDbContext db, string fileName)
        {
            string sqlPath = HostingEnvironment.MapPath("~/App_Data/" + fileName);
            if (string.IsNullOrEmpty(sqlPath) || !File.Exists(sqlPath))
            {
                sqlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\Database\Scripts\" + fileName);
            }

            if (!File.Exists(sqlPath)) return false;

            string sql = File.ReadAllText(sqlPath);
            if (string.IsNullOrWhiteSpace(sql)) return false;

            db.Database.ExecuteSqlCommand(sql);
            return true;
        }
    }
}
