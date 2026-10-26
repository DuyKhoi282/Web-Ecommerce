-- =====================================================
-- 09. DỮ LIỆU DEMO BỔ SUNG (theo yêu cầu đề bài)
-- - Đợt Flash Sale thứ 2 (sắp diễn ra, bắt đầu sau 2 ngày)
-- - Ảnh thứ 3 cho 7 sản phẩm nổi bật
-- - 1 sản phẩm hết hàng (Status = 2)
-- Chạy tay SAU 02, 03, 05. Chạy lại nhiều lần không nhân bản dữ liệu.
-- Không cho DatabaseConfig tự chạy: là dữ liệu chứ không phải schema,
-- chạy mỗi lần khởi động sẽ ghi đè thao tác của admin (vd. nhập thêm hàng).
-- Tài khoản demo (manager2, customer4..10) do Startup.cs seed.
-- Database: PostgreSQL
-- =====================================================

BEGIN;

-- 1. Flash Sale thứ 2 (thời gian lưu theo UTC như FlashSaleController)
INSERT INTO "FlashSales" ("Title", "Description", "StartTime", "EndTime", "IsActive")
SELECT 'FLASH SALE PHỤ KIỆN - GIẢM ĐẾN 25%',
       'Tai nghe, loa, chuột và bàn phím giá sốc trong 48 giờ. Số lượng có hạn!',
       date_trunc('hour', NOW() AT TIME ZONE 'UTC') + INTERVAL '2 days',
       date_trunc('hour', NOW() AT TIME ZONE 'UTC') + INTERVAL '4 days',
       TRUE
WHERE NOT EXISTS (SELECT 1 FROM "FlashSales" WHERE "Title" = 'FLASH SALE PHỤ KIỆN - GIẢM ĐẾN 25%');

INSERT INTO "FlashSaleItems" ("FlashSaleID", "ProductID", "FlashSalePrice", "MaxQuantity", "SoldQuantity")
SELECT fs."FlashSaleID", v.product_id, v.price, v.max_qty, 0
FROM "FlashSales" fs
CROSS JOIN (VALUES
    (25, 6990000, 20),   -- Sony WH-1000XM5: 7,890,000 -> 6,990,000
    (26, 2290000, 30),   -- JBL Charge 5: 2,690,000 -> 2,290,000
    (34, 1690000, 25),   -- Logitech G Pro X Superlight 2: 1,990,000 -> 1,690,000
    (35, 1890000, 25)    -- Keychron K2 Pro: 2,190,000 -> 1,890,000
) AS v(product_id, price, max_qty)
JOIN "Products" p ON p."ProductID" = v.product_id AND p."Status" <> 3
WHERE fs."Title" = 'FLASH SALE PHỤ KIỆN - GIẢM ĐẾN 25%'
  AND NOT EXISTS (SELECT 1 FROM "FlashSaleItems" i
                  WHERE i."FlashSaleID" = fs."FlashSaleID" AND i."ProductID" = v.product_id);

-- 2. Ảnh thứ 3 (ảnh phụ) cho 7 sản phẩm
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder")
SELECT v.product_id, v.url, false, 3
FROM (VALUES
    (1,  'https://cellphones.com.vn/sforum/wp-content/uploads/2023/10/iphone-15-pro-max-titan-tu-nhien-1.jpg'),
    (2,  'https://cellphones.com.vn/sforum/wp-content/uploads/2023/11/Samsung-Galaxy-S24-Ultra-1.jpeg'),
    (3,  'https://cellphones.com.vn/sforum/wp-content/uploads/2023/10/apple-macbook-pro-m3-pro-space-black-1.jpg'),
    (5,  'https://cdn2.cellphones.com.vn/x/media/catalog/product/a/i/airpods_pro_2_sep24_pdp_image_position_4__vn-vi.jpg'),
    (7,  'https://cdn2.cellphones.com.vn/x/media/catalog/product/m/a/marshall_acton_iii_2.png'),
    (21, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/06/Macbook-Air-M2-15inch-5.jpg'),
    (25, 'https://cellphones.com.vn/sforum/wp-content/uploads/2022/06/Sony-WH-1000MX5-5.jpg')
) AS v(product_id, url)
JOIN "Products" p ON p."ProductID" = v.product_id
WHERE NOT EXISTS (SELECT 1 FROM "ProductImages" pi
                  WHERE pi."ProductID" = v.product_id AND pi."ImageURL" = v.url);

-- 3. Sản phẩm hết hàng: Ghế DXRacer (#36). Bỏ qua nếu CSDL đã có sản phẩm hết hàng
UPDATE "Products" SET "StockQuantity" = 0, "Status" = 2
WHERE "ProductID" = 36 AND "Status" = 1
  AND NOT EXISTS (SELECT 1 FROM "Products" WHERE "Status" = 2);

COMMIT;
