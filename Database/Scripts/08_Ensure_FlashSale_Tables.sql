-- =====================================================
-- 08. ENSURE "FlashSales" + "FlashSaleItems"
-- Chỉ tạo bảng (giống phần DDL của 03), không có dữ liệu mẫu,
-- để DatabaseConfig tự chạy lúc khởi động trên máy chưa chạy 03.
-- Thay cho khối CREATE TABLE viết thẳng trong Startup.cs trước đây.
-- Database: PostgreSQL
-- =====================================================

CREATE TABLE IF NOT EXISTS "FlashSales" (
    "FlashSaleID" SERIAL PRIMARY KEY,
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "StartTime" TIMESTAMP NOT NULL,
    "EndTime" TIMESTAMP NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS "FlashSaleItems" (
    "FlashSaleItemID" SERIAL PRIMARY KEY,
    "FlashSaleID" INT NOT NULL REFERENCES "FlashSales"("FlashSaleID") ON DELETE CASCADE,
    "ProductID" INT NOT NULL REFERENCES "Products"("ProductID") ON DELETE CASCADE,
    "FlashSalePrice" NUMERIC(18, 2) NOT NULL,
    "MaxQuantity" INT NOT NULL DEFAULT 50,
    "SoldQuantity" INT NOT NULL DEFAULT 0
);
