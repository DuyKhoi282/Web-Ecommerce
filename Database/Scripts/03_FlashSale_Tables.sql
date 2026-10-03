-- ============================================================================
-- FLASH SALE - TẠO BẢNG VÀ DỮ LIỆU MẪU
-- Chạy script này sau 01_Init_PostgreSQL_Database.sql
-- ============================================================================

-- 14. BẢNG FLASH SALE (FlashSales)
CREATE TABLE IF NOT EXISTS "FlashSales" (
    "FlashSaleID" SERIAL PRIMARY KEY,
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "StartTime" TIMESTAMP NOT NULL,
    "EndTime" TIMESTAMP NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 15. BẢNG SẢN PHẨM FLASH SALE (FlashSaleItems)
CREATE TABLE IF NOT EXISTS "FlashSaleItems" (
    "FlashSaleItemID" SERIAL PRIMARY KEY,
    "FlashSaleID" INT NOT NULL REFERENCES "FlashSales"("FlashSaleID") ON DELETE CASCADE,
    "ProductID" INT NOT NULL REFERENCES "Products"("ProductID") ON DELETE CASCADE,
    "FlashSalePrice" NUMERIC(18, 2) NOT NULL,
    "MaxQuantity" INT NOT NULL DEFAULT 50,
    "SoldQuantity" INT NOT NULL DEFAULT 0
);

-- ============================================================================
-- DỮ LIỆU MẪU FLASH SALE
-- ============================================================================

-- Flash Sale hôm nay (chạy 24h)
INSERT INTO "FlashSales" ("FlashSaleID", "Title", "Description", "StartTime", "EndTime", "IsActive") VALUES
(1, 'Flash Sale Cuối Tuần - Giảm Sốc Tới 50%', 'Ưu đãi độc quyền chỉ trong hôm nay! Số lượng có hạn, nhanh tay kẻo hết!', CURRENT_DATE, CURRENT_DATE + INTERVAL '1 day', TRUE)
ON CONFLICT ("FlashSaleID") DO NOTHING;

SELECT setval(pg_get_serial_sequence('"FlashSales"', 'FlashSaleID'), coalesce(max("FlashSaleID"), 1)) FROM "FlashSales";

-- Gắn sản phẩm vào Flash Sale (ProductID 5,7,8 từ seed data)
INSERT INTO "FlashSaleItems" ("FlashSaleItemID", "FlashSaleID", "ProductID", "FlashSalePrice", "MaxQuantity", "SoldQuantity") VALUES
(1, 1, 5, 3990000, 30, 12),   -- AirPods Pro 2: 5,290,000 -> 3,990,000
(2, 1, 7, 4990000, 20, 8),    -- Marshall Acton III: 6,490,000 -> 4,990,000
(3, 1, 8, 690000, 50, 35)     -- Anker 65W: 990,000 -> 690,000
ON CONFLICT ("FlashSaleItemID") DO NOTHING;

SELECT setval(pg_get_serial_sequence('"FlashSaleItems"', 'FlashSaleItemID'), coalesce(max("FlashSaleItemID"), 1)) FROM "FlashSaleItems";
