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

                    if (totalImages == 0 || invalidImages > 0)
                    {
                        string sqlPath = HostingEnvironment.MapPath("~/App_Data/05_Update_Authentic_Product_Images.sql");
                        if (string.IsNullOrEmpty(sqlPath) || !File.Exists(sqlPath))
                        {
                            sqlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\Database\Scripts\05_Update_Authentic_Product_Images.sql");
                        }

                        if (File.Exists(sqlPath))
                        {
                            string sql = File.ReadAllText(sqlPath);
                            if (!string.IsNullOrWhiteSpace(sql))
                            {
                                db.Database.ExecuteSqlCommand(sql);
                                System.Diagnostics.Debug.WriteLine("[DatabaseConfig] Cập nhật hình ảnh sản phẩm thành công!");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfig AutoMigration Error] {ex.Message}");
            }
        }
    }
}
