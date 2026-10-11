-- ============================================================================
-- FILE: 02_Seed_Data.sql
-- MỤC ĐÍCH: Bổ sung dữ liệu mẫu đầy đủ để toàn nhóm test
-- THỰC HIỆN: Chạy SAU khi đã chạy 01_Init_PostgreSQL_Database.sql
--            và đã khởi động ứng dụng ít nhất 1 lần (để Startup.cs seed Admin)
-- ============================================================================
-- SỐ LƯỢNG: 8 danh mục (cha+con) | 32 sản phẩm | 50 ảnh | 5 voucher
--           3 tài khoản mẫu | 20 đơn hàng | đánh giá + wishlist
-- ============================================================================

-- ============================================================================
-- PHẦN 1: DANH MỤC SẢN PHẨM (8 danh mục: 5 cha + 3 con)
-- ============================================================================

INSERT INTO "Categories" ("CategoryID", "ParentCategoryID", "Name", "Description", "DisplayOrder", "IsActive") VALUES
-- Danh mục cha (đã có 5 từ script 01, bổ sung thêm)
(6, NULL, 'Máy ảnh & Quay phim',  'Máy ảnh mirrorless, DSLR, action cam và phụ kiện nhiếp ảnh', 6, TRUE),
(7, NULL, 'Thiết bị Gaming',       'Chuột, bàn phím cơ, tai nghe gaming và ghế gaming chuyên nghiệp', 7, TRUE),
(8, NULL, 'Nhà thông minh',        'Loa thông minh, đèn LED thông minh và thiết bị IoT smarthome', 8, TRUE),
-- Danh mục con (ParentCategoryID trỏ về danh mục cha)
(9,  1, 'Điện thoại iPhone',  'Các dòng iPhone chính hãng Apple mới nhất', 1, TRUE),
(10, 1, 'Điện thoại Samsung', 'Các dòng Galaxy S, A, M chính hãng Samsung', 2, TRUE),
(11, 2, 'Laptop Gaming',      'Laptop gaming hiệu năng cao cho game thủ chuyên nghiệp', 1, TRUE),
(12, 2, 'Laptop Văn phòng',   'Laptop mỏng nhẹ, pin trâu cho dân văn phòng', 2, TRUE),
(13, 3, 'Tai nghe & Loa',     'Tai nghe không dây, có dây và loa Bluetooth di động', 1, TRUE)
ON CONFLICT ("CategoryID") DO NOTHING;

SELECT setval(pg_get_serial_sequence('"Categories"', 'CategoryID'), 13);

-- ============================================================================
-- PHẦN 2: SẢN PHẨM MẪU (32 sản phẩm, nhiều danh mục, đủ loại trạng thái)
-- ============================================================================
-- Status: 1=Còn hàng | 2=Hết hàng | 3=Ngừng bán
-- DiscountPrice NULL = không có giảm giá

INSERT INTO "Products" ("ProductID", "CategoryID", "Name", "Description", "Price", "DiscountPrice", "StockQuantity", "Status", "ViewCount") VALUES
-- [Danh mục 9 - iPhone]
(9,  9,  'iPhone 14 128GB Đỏ',                  'Chip A15 Bionic, camera 12MP Dual, pin 3279mAh.',                          22990000, 19990000, 30, 1, 280),
(10, 9,  'iPhone 13 128GB Đêm Khuya',            'Màn hình Super Retina XDR 6.1 inch, chip A15 Bionic.',                     18490000, 15990000, 22, 1, 195),
(11, 9,  'iPhone 15 256GB Hồng',                 'Cổng USB-C, camera 48MP chính, chip A16 Bionic.',                          25990000, 23490000, 15, 1, 310),
(12, 9,  'iPhone 12 64GB Trắng',                 'Màn OLED 6.1 inch Super Retina XDR, 5G.',                                  14990000, NULL,      3,  1, 88),  -- Sắp hết

-- [Danh mục 10 - Samsung]
(13, 10, 'Samsung Galaxy A55 5G 256GB',          'Màn AMOLED 6.6 inch, camera 50MP, pin 5000mAh sạc 45W.',                   9490000,  8290000,  40, 1, 175),
(14, 10, 'Samsung Galaxy S23 FE 256GB',          'Chip Snapdragon 8 Gen 1, màn Super AMOLED 120Hz 6.4 inch.',                13990000, 11490000, 18, 1, 142),
(15, 10, 'Samsung Galaxy Z Flip5 256GB',         'Màn gập FlexCam 3.4 inch Flex Window, Snapdragon 8 Gen 2.',                22490000, 19990000, 8,  1, 98),
(16, 10, 'Samsung Galaxy A34 5G 128GB',          'Màn Super AMOLED 6.6 inch 120Hz, camera 48MP.',                            7990000,  6990000,  0,  2, 220), -- Hết hàng

-- [Danh mục 11 - Laptop Gaming]
(17, 11, 'ASUS ROG Strix G16 RTX 4070',          'Intel Core i9-13980HX, RAM 16GB, SSD 1TB, màn 165Hz QHD.',                 47990000, 44990000, 6,  1, 115),
(18, 11, 'Acer Nitro 5 AN515 RTX 4060',          'Intel Core i7-13700H, RAM 16GB DDR5, SSD 512GB.',                          28990000, 26490000, 12, 1, 98),
(19, 11, 'Lenovo LOQ 15APH8 RTX 4060',           'AMD Ryzen 7 7745HX, RAM 16GB, 512GB SSD, màn 144Hz.',                      26990000, 24490000, 9,  1, 87),
(20, 11, 'MSI Katana 15 RTX 4070',               'Intel Core i7-13620H, RAM 16GB DDR5, 1TB SSD NVMe.',                       34990000, NULL,      4,  1, 72),  -- Sắp hết

-- [Danh mục 12 - Laptop Văn phòng]
(21, 12, 'MacBook Air M2 8GB/256GB Midnight',    'Chip M2, màn Liquid Retina 13.6 inch, pin 18 giờ.',                        27990000, 25490000, 20, 1, 265),
(22, 12, 'LG Gram 14 2024 Intel Core Ultra 7',   'Siêu nhẹ 999g, pin 72Wh, MIL-STD-810H chịu va đập.',                      35990000, 33490000, 7,  1, 65),
(23, 12, 'Asus Zenbook 14 OLED UX3405',          'Core Ultra 7 155H, màn OLED 2.8K 120Hz, sạc 65W nhanh.',                   29990000, 27490000, 11, 1, 90),
(24, 12, 'HP EliteBook 840 G10 Core i7',         'Bảo mật doanh nghiệp, vân tay + khuôn mặt, pin 53Wh.',                    32990000, NULL,      5,  1, 45),

-- [Danh mục 13 - Tai nghe & Loa]
(25, 13, 'Sony WH-1000XM5 Chống ồn',            'Chống ồn Industry-leading, pin 30 giờ, kết nối Multipoint.',               8990000,  7890000,  25, 1, 380),
(26, 13, 'JBL Charge 5 Loa Bluetooth',           'Chống nước IP67, pin 20 giờ, âm bass mạnh.',                               2990000,  2690000,  33, 1, 155),
(27, 13, 'Bose QuietComfort 45',                 'Chống ồn Acoustic Noise Cancelling, pin 24 giờ.',                          7490000,  6790000,  14, 1, 98),
(28, 13, 'Samsung Galaxy Buds2 Pro',             'Chống ồn ANC thế hệ 2, âm 3D immersive, sạc không dây.',                  3990000,  3490000,  0,  2, 180), -- Hết hàng

-- [Danh mục 4 - Đồng hồ thông minh]
(29, 4,  'Garmin Fenix 7S Solar',                'GPS đa dải, sạc năng lượng mặt trời, pin đến 36 ngày.',                   18990000, NULL,     5,  1, 62),  -- Sắp hết
(30, 4,  'Samsung Galaxy Watch6 Classic 47mm',   'Vòng bezel xoay cổ điển, theo dõi sức khỏe toàn diện.',                   8490000,  7490000,  20, 1, 110),
(31, 4,  'Xiaomi Smart Band 8 Pro',              'Màn AMOLED 1.74 inch, theo dõi 150 bài tập, GPS tích hợp.',                1490000,  1290000,  55, 1, 290),

-- [Danh mục 6 - Máy ảnh]
(32, 6,  'Sony Alpha ZV-E10 Kit 16-50mm',        'Cảm biến APS-C 24.2MP, quay 4K, lý tưởng cho vlogger.',                   16990000, 14990000, 8,  1, 74),
(33, 6,  'GoPro Hero12 Black',                   'Quay 5.3K60fps, chống nước 10m không cần vỏ bảo vệ.',                     9990000,  8990000,  15, 1, 65),

-- [Danh mục 7 - Gaming]
(34, 7,  'Chuột Gaming Logitech G Pro X Superlight 2', 'Cảm biến HERO 25K, 60g siêu nhẹ, không dây 2.4GHz.',              2190000,  1990000,  30, 1, 145),
(35, 7,  'Bàn phím cơ Keychron K2 Pro RGB',     'Switch Gateron, layout TKL, kết nối 3 thiết bị Bluetooth.',                2490000,  2190000,  22, 1, 188),
(36, 7,  'Ghế Gaming DXRacer Formula Series',   'Khung thép, đệm da PU cao cấp, điều chỉnh 4D, tải 120kg.',                7990000,  6990000,  6,  1, 77),

-- [Danh mục 8 - Nhà thông minh]
(37, 8,  'Loa thông minh Amazon Echo Dot Gen5', 'Tích hợp Alexa, âm thanh 360°, hub zigbee tích hợp.',                     1990000,  1690000,  40, 1, 90),
(38, 8,  'Bóng đèn thông minh Philips Hue White','Điều khiển qua app/giọng nói, 16M màu, bền 15.000 giờ.',                  990000,   NULL,     60, 1, 120),
(39, 8,  'Camera an ninh TP-Link Tapo C200',    'Full HD 1080P, xoay 360°, phát hiện chuyển động, lưu trữ cloud.', 690000, 590000, 0, 3, 55) -- Ngừng bán
ON CONFLICT ("ProductID") DO NOTHING;

SELECT setval(pg_get_serial_sequence('"Products"', 'ProductID'), 39);

-- ============================================================================
-- PHẦN 3: ẢNH SẢN PHẨM (ProductImages — dùng ảnh picsum placeholder)
-- ============================================================================

INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
-- Sản phẩm gốc (1-8)
(1, 'https://upload.wikimedia.org/wikipedia/commons/a/a7/IPhone_15_pro_max.jpg', TRUE,  1),
(1, 'https://upload.wikimedia.org/wikipedia/commons/a/a7/IPhone_15_pro_max.jpg', FALSE, 2),
(2, 'https://upload.wikimedia.org/wikipedia/commons/4/4b/Samsung_Galaxy_S24_Ultra.jpg', TRUE,  1),
(2, 'https://upload.wikimedia.org/wikipedia/commons/4/4b/Samsung_Galaxy_S24_Ultra.jpg', FALSE, 2),
(3, 'https://upload.wikimedia.org/wikipedia/commons/0/05/MacBook_Pro_14-inch_%282021%29.jpg', TRUE,  1),
(3, 'https://upload.wikimedia.org/wikipedia/commons/0/05/MacBook_Pro_14-inch_%282021%29.jpg', FALSE, 2),
(4, 'https://upload.wikimedia.org/wikipedia/commons/8/87/Dell_XPS_13_9300.jpg', TRUE,  1),
(5, 'https://upload.wikimedia.org/wikipedia/commons/9/90/AirPods_Pro.jpg', TRUE,  1),
(5, 'https://upload.wikimedia.org/wikipedia/commons/9/90/AirPods_Pro.jpg', FALSE, 2),
(5, 'https://upload.wikimedia.org/wikipedia/commons/9/90/AirPods_Pro.jpg', FALSE, 3),
(6, 'https://upload.wikimedia.org/wikipedia/commons/6/6c/Apple_Watch_Series_7_Midnight_Aluminum_Case_with_Midnight_Sport_Band_Front.jpg', TRUE,  1),
(7, 'https://upload.wikimedia.org/wikipedia/commons/0/0b/Marshall_Acton_II.jpg', TRUE,  1),
(8, 'https://upload.wikimedia.org/wikipedia/commons/3/30/Anker_PowerPort_Atom_PD_1.jpg', TRUE,  1),
-- Sản phẩm mới (9-39)
(9,  'https://upload.wikimedia.org/wikipedia/commons/a/a7/IPhone_15_pro_max.jpg',  TRUE,  1),
(9,  'https://upload.wikimedia.org/wikipedia/commons/a/a7/IPhone_15_pro_max.jpg',  FALSE, 2),
(10, 'https://upload.wikimedia.org/wikipedia/commons/f/fb/IPhone_13_Pro_Max_-_Blue.jpg', TRUE,  1),
(11, 'https://upload.wikimedia.org/wikipedia/commons/a/a7/IPhone_15_pro_max.jpg', TRUE,  1),
(11, 'https://upload.wikimedia.org/wikipedia/commons/a/a7/IPhone_15_pro_max.jpg', FALSE, 2),
(12, 'https://upload.wikimedia.org/wikipedia/commons/5/5f/IPhone_12_Pro_Max.jpg', TRUE,  1),
(13, 'https://upload.wikimedia.org/wikipedia/commons/7/7b/Samsung_Galaxy_A54_5G.jpg', TRUE,  1),
(13, 'https://upload.wikimedia.org/wikipedia/commons/7/7b/Samsung_Galaxy_A54_5G.jpg', FALSE, 2),
(14, 'https://upload.wikimedia.org/wikipedia/commons/c/c8/Samsung_Galaxy_S23_Ultra.jpg', TRUE,  1),
(15, 'https://upload.wikimedia.org/wikipedia/commons/5/52/Samsung_Galaxy_Z_Flip_3.jpg', TRUE,  1),
(16, 'https://upload.wikimedia.org/wikipedia/commons/7/7b/Samsung_Galaxy_A54_5G.jpg', TRUE,  1),
(17, 'https://upload.wikimedia.org/wikipedia/commons/4/47/Asus_ROG_Zephyrus_G14_%282020%29.jpg', TRUE,  1),
(17, 'https://upload.wikimedia.org/wikipedia/commons/4/47/Asus_ROG_Zephyrus_G14_%282020%29.jpg', FALSE, 2),
(18, 'https://upload.wikimedia.org/wikipedia/commons/6/67/Acer_Nitro_5_%282020%29.jpg', TRUE,  1),
(19, 'https://upload.wikimedia.org/wikipedia/commons/4/4c/Lenovo_Legion_5.jpg', TRUE,  1),
(20, 'https://upload.wikimedia.org/wikipedia/commons/8/87/MSI_GS65_Stealth.jpg', TRUE,  1),
(21, 'https://upload.wikimedia.org/wikipedia/commons/b/bd/MacBook_Air_M1.jpg', TRUE,  1),
(21, 'https://upload.wikimedia.org/wikipedia/commons/b/bd/MacBook_Air_M1.jpg', FALSE, 2),
(22, 'https://upload.wikimedia.org/wikipedia/commons/d/da/LG_Gram_17_%282021%29.jpg', TRUE,  1),
(23, 'https://upload.wikimedia.org/wikipedia/commons/e/ea/Asus_Zenbook_14.jpg', TRUE,  1),
(24, 'https://upload.wikimedia.org/wikipedia/commons/b/bb/HP_EliteBook_840_G5.jpg', TRUE,  1),
(25, 'https://upload.wikimedia.org/wikipedia/commons/8/88/Sony_WH-1000XM4.jpg', TRUE,  1),
(25, 'https://upload.wikimedia.org/wikipedia/commons/8/88/Sony_WH-1000XM4.jpg', FALSE, 2),
(26, 'https://upload.wikimedia.org/wikipedia/commons/d/d4/JBL_Charge_3.jpg', TRUE,  1),
(27, 'https://upload.wikimedia.org/wikipedia/commons/c/c5/Bose_QuietComfort_35_II.jpg', TRUE,  1),
(28, 'https://upload.wikimedia.org/wikipedia/commons/2/2f/Samsung_Galaxy_Buds_Pro.jpg', TRUE,  1),
(29, 'https://upload.wikimedia.org/wikipedia/commons/3/3f/Garmin_Fenix_6.jpg', TRUE,  1),
(30, 'https://upload.wikimedia.org/wikipedia/commons/7/7a/Samsung_Galaxy_Watch_4.jpg', TRUE,  1),
(31, 'https://upload.wikimedia.org/wikipedia/commons/0/05/Xiaomi_Mi_Band_5.jpg', TRUE,  1),
(31, 'https://upload.wikimedia.org/wikipedia/commons/0/05/Xiaomi_Mi_Band_5.jpg', FALSE, 2),
(32, 'https://upload.wikimedia.org/wikipedia/commons/5/5a/Sony_Alpha_ZV-E10.jpg', TRUE,  1),
(33, 'https://upload.wikimedia.org/wikipedia/commons/d/d2/GoPro_HERO9_Black.jpg', TRUE,  1),
(34, 'https://upload.wikimedia.org/wikipedia/commons/6/69/Logitech_G_Pro_Wireless.jpg', TRUE,  1),
(35, 'https://upload.wikimedia.org/wikipedia/commons/5/5a/Keychron_K2.jpg', TRUE,  1),
(36, 'https://upload.wikimedia.org/wikipedia/commons/3/36/DXRacer_Gaming_Chair.jpg', TRUE,  1),
(37, 'https://upload.wikimedia.org/wikipedia/commons/2/23/Amazon_Echo_Dot_3rd_Gen.jpg', TRUE,  1),
(38, 'https://upload.wikimedia.org/wikipedia/commons/3/3f/Philips_Hue_Bulb.jpg', TRUE,  1),
(39, 'https://upload.wikimedia.org/wikipedia/commons/2/2d/TP-Link_Tapo_C200.jpg', TRUE,  1);

-- ============================================================================
-- PHẦN 4: VOUCHER MẪU (5 voucher đa dạng)
-- ============================================================================

INSERT INTO "PromotionalCodes"
  ("PromoCodeID", "Code", "Description", "DiscountType", "DiscountValue", "MinOrderAmount", "MaxUsage", "CurrentUsage", "StartDate", "EndDate", "IsActive")
VALUES
(3, 'FLASH30',      'Flash Sale giảm 30% tất cả đơn từ 2 triệu — cuối tuần này thôi!',  'Percentage', 30,     2000000,  50,  0, '2026-09-27', '2026-10-05', TRUE),
(4, 'FREESHIP',     'Giảm 50.000đ phí vận chuyển cho mọi đơn hàng',                     'Fixed',      50000,  0,        999, 0, '2026-01-01', '2026-12-31', TRUE),
(5, 'VIP1TRIEU',    'Giảm 1.000.000đ cho đơn hàng VIP từ 15 triệu trở lên',             'Fixed',      1000000,15000000, 20,  3, '2026-09-01', '2026-10-31', TRUE),
(6, 'EXPIRED2025',  'Voucher hết hạn — dùng để test validate',                           'Percentage', 20,     500000,   100, 0, '2025-01-01', '2025-12-31', FALSE),
(7, 'FULLUSED',     'Voucher đã dùng hết lượt — test trường hợp MaxUsage',               'Fixed',      200000, 300000,   10,  10,'2026-01-01', '2026-12-31', TRUE)
ON CONFLICT ("PromoCodeID") DO NOTHING;

SELECT setval(pg_get_serial_sequence('"PromotionalCodes"', 'PromoCodeID'), 7);

-- ============================================================================
-- PHẦN 5: TÀI KHOẢN MẪU
-- ============================================================================
-- ⚠️ GHI CHÚ QUAN TRỌNG:
--    ASP.NET Identity dùng PBKDF2 để băm mật khẩu — không thể seed trực tiếp.
--    Hãy tạo tài khoản qua giao diện web, sau đó chạy phần 6 (Orders) bên dưới.
--
--    Tài khoản cần tạo:
--    ┌─────────────────────────────────────────────────────────────────────┐
--    │  Email                      │ Password        │ Role               │
--    │─────────────────────────────────────────────────────────────────────│
--    │  customer1@gmail.com        │ Customer@123456 │ Customer           │
--    │  customer2@gmail.com        │ Customer@123456 │ Customer           │
--    │  customer3@gmail.com        │ Customer@123456 │ Customer           │
--    │  manager@thechillshop.vn    │ Manager@123456  │ StoreManager       │
--    └─────────────────────────────────────────────────────────────────────┘
--    Sau khi tạo xong, dùng query sau để lấy UserID thực tế:
--    SELECT "Id", "Email" FROM "AspNetUsers" ORDER BY "CreatedAt";
-- ============================================================================

-- ============================================================================
-- PHẦN 6: ĐƠN HÀNG MẪU (20 đơn, đủ 6 trạng thái để test Dashboard TV4)
-- ============================================================================
-- ⚠️ Chạy sau khi đã tạo tài khoản customer qua web
--    Thay thế 'USER_ID_CUSTOMER_1/2/3' bằng Id thực từ bảng AspNetUsers

DO $$
DECLARE
    uid1 TEXT;
    uid2 TEXT;
    uid3 TEXT;
BEGIN
    -- Lấy UserID tự động (3 customer đầu tiên theo thứ tự đăng ký)
    SELECT "Id" INTO uid1 FROM "AspNetUsers"
    WHERE "Email" = 'customer1@gmail.com' LIMIT 1;

    SELECT "Id" INTO uid2 FROM "AspNetUsers"
    WHERE "Email" = 'customer2@gmail.com' LIMIT 1;

    SELECT "Id" INTO uid3 FROM "AspNetUsers"
    WHERE "Email" = 'customer3@gmail.com' LIMIT 1;

    -- Chỉ chạy nếu tìm thấy user
    IF uid1 IS NOT NULL THEN

        INSERT INTO "Orders"
          ("OrderID","UserID","PromoCodeID","OrderDate","TotalAmount","DiscountAmount","FinalAmount",
           "Status","ShippingAddress","ReceiverPhone","PaymentMethod","PaymentStatus","PaymentDate","Notes")
        VALUES
        -- Đơn Delivered (doanh thu thực cho Dashboard)
        (1, uid1, 1,   '2026-08-05 09:10:00', 29990000, 2999000, 26991000, 'Delivered',   '123 Lê Lợi, Q1, TP.HCM',          '0901234561', 'BankTransfer', 'Paid',               '2026-08-12', NULL),
        (2, uid1, NULL,'2026-08-15 14:30:00', 46490000, 0,       46490000, 'Delivered',   '123 Lê Lợi, Q1, TP.HCM',          '0901234561', 'COD',          'Paid',               '2026-08-22', NULL),
        (3, uid2, 2,   '2026-08-20 10:00:00', 8290000,  500000,  7790000,  'Delivered',   '456 Trần Hưng Đạo, Q5, TP.HCM',   '0912345672', 'COD',          'Paid',               '2026-08-27', NULL),
        (4, uid2, NULL,'2026-09-01 16:00:00', 7890000,  0,       7890000,  'Delivered',   '456 Trần Hưng Đạo, Q5, TP.HCM',   '0912345672', 'BankTransfer', 'Paid',               '2026-09-08', NULL),
        (5, uid3, NULL,'2026-09-03 11:20:00', 25490000, 0,       25490000, 'Delivered',   '789 Nguyễn Trãi, Q7, TP.HCM',     '0923456783', 'COD',          'Paid',               '2026-09-10', NULL),
        (6, uid1, NULL,'2026-09-10 09:45:00', 14990000, 0,       14990000, 'Delivered',   '123 Lê Lợi, Q1, TP.HCM',          '0901234561', 'COD',          'Paid',               '2026-09-17', NULL),
        (7, uid3, 5,   '2026-09-12 15:30:00', 44990000, 1000000, 43990000, 'Delivered',   '789 Nguyễn Trãi, Q7, TP.HCM',     '0923456783', 'BankTransfer', 'Paid',               '2026-09-19', NULL),
        (8, uid2, NULL,'2026-09-15 08:00:00', 6990000,  0,       6990000,  'Delivered',   '456 Trần Hưng Đạo, Q5, TP.HCM',   '0912345672', 'COD',          'Paid',               '2026-09-22', NULL),
        -- Đơn Shipped
        (9,  uid1, NULL,'2026-09-20 10:00:00', 8990000,  0,       8990000,  'Shipped',    '123 Lê Lợi, Q1, TP.HCM',          '0901234561', 'COD',          'Unpaid',             NULL, 'Đang trên đường giao'),
        (10, uid3, NULL,'2026-09-21 14:00:00', 26490000, 0,       26490000, 'Shipped',    '789 Nguyễn Trãi, Q7, TP.HCM',     '0923456783', 'BankTransfer', 'PendingConfirmation',NULL, NULL),
        -- Đơn Processing
        (11, uid2, 3,  '2026-09-24 09:00:00', 27990000, 8397000, 19593000, 'Processing', '456 Trần Hưng Đạo, Q5, TP.HCM',   '0912345672', 'COD',          'Unpaid',             NULL, NULL),
        (12, uid1, NULL,'2026-09-24 16:30:00', 1990000,  0,       1990000,  'Processing', '123 Lê Lợi, Q1, TP.HCM',          '0901234561', 'COD',          'Unpaid',             NULL, NULL),
        -- Đơn Confirmed
        (13, uid3, NULL,'2026-09-26 08:30:00', 2990000,  0,       2990000,  'Confirmed',  '789 Nguyễn Trãi, Q7, TP.HCM',     '0923456783', 'COD',          'Unpaid',             NULL, NULL),
        (14, uid2, 4,  '2026-09-26 11:00:00', 9890000,  50000,   9840000,  'Confirmed',  '456 Trần Hưng Đạo, Q5, TP.HCM',   '0912345672', 'BankTransfer', 'PendingConfirmation',NULL, 'Chờ xác nhận CK'),
        -- Đơn Pending (mới nhất)
        (15, uid1, NULL,'2026-09-28 07:00:00', 19990000, 0,       19990000, 'Pending',    '123 Lê Lợi, Q1, TP.HCM',          '0901234561', 'COD',          'Unpaid',             NULL, NULL),
        (16, uid3, NULL,'2026-09-28 08:15:00', 6490000,  0,       6490000,  'Pending',    '789 Nguyễn Trãi, Q7, TP.HCM',     '0923456783', 'BankTransfer', 'Unpaid',             NULL, 'Giao giờ hành chính'),
        (17, uid2, NULL,'2026-09-28 09:30:00', 1290000,  0,       1290000,  'Pending',    '456 Trần Hưng Đạo, Q5, TP.HCM',   '0912345672', 'COD',          'Unpaid',             NULL, NULL),
        -- Đơn Cancelled
        (18, uid1, NULL,'2026-09-05 10:00:00', 32990000, 0,       32990000, 'Cancelled',  '123 Lê Lợi, Q1, TP.HCM',          '0901234561', 'COD',          'Unpaid',             NULL, 'Khách huỷ: đổi màu khác'),
        (19, uid3, NULL,'2026-09-18 14:00:00', 5290000,  0,       5290000,  'Cancelled',  '789 Nguyễn Trãi, Q7, TP.HCM',     '0923456783', 'COD',          'Unpaid',             NULL, 'Hết hàng tại kho'),
        (20, uid2, NULL,'2026-09-22 09:00:00', 2190000,  0,       2190000,  'Cancelled',  '456 Trần Hưng Đạo, Q5, TP.HCM',   '0912345672', 'BankTransfer', 'Unpaid',             NULL, NULL)
        ON CONFLICT ("OrderID") DO NOTHING;

        -- Chi tiết đơn hàng
        INSERT INTO "OrderDetails" ("OrderID","ProductID","Quantity","UnitPrice","Subtotal") VALUES
        (1,  1,  1, 29990000, 29990000),
        (2,  3,  1, 46490000, 46490000),
        (3,  13, 1, 8290000,  8290000),
        (4,  25, 1, 7890000,  7890000),
        (5,  21, 1, 25490000, 25490000),
        (6,  11, 1, 14990000, 14990000),  -- iPhone 15
        (7,  17, 1, 44990000, 44990000),  -- ROG Strix
        (8,  7,  1, 6990000,  6990000),   -- Marshall Acton
        (9,  25, 1, 8990000,  8990000),   -- Sony WH
        (10, 18, 1, 26490000, 26490000),  -- Acer Nitro
        (11, 3,  1, 27990000, 27990000),  -- MacBook Pro
        (12, 37, 1, 1990000,  1990000),   -- Echo Dot
        (13, 26, 1, 2990000,  2990000),   -- JBL Charge
        (14, 6,  1, 9890000,  9890000),   -- Apple Watch
        (15, 2,  1, 19990000, 19990000),  -- Samsung S24 Ultra
        (16, 5,  1, 5290000,  5290000),   -- AirPods Pro
        (17, 31, 1, 1290000,  1290000),   -- Xiaomi Band
        (18, 1,  1, 32990000, 32990000),  -- iPhone 15 Pro
        (19, 5,  1, 5290000,  5290000),   -- AirPods
        (20, 34, 1, 2190000,  2190000)    -- Logitech G Pro
        ON CONFLICT DO NOTHING;

        PERFORM setval(pg_get_serial_sequence('"Orders"', 'OrderID'), 20);

        -- ============================================================
        -- PHẦN 7: ĐÁNH GIÁ MẪU (chỉ đơn Delivered — đúng nghiệp vụ)
        -- ============================================================
        INSERT INTO "Reviews" ("ProductID","UserID","OrderID","Rating","Comment","IsApproved") VALUES
        (1,  uid1, 1,  5, 'Máy cực đẹp, camera sắc nét, hiệu năng vượt trội. Rất hài lòng!',       TRUE),
        (3,  uid1, 2,  5, 'MacBook quá mượt mà, pin dùng cả ngày. Đóng gói chắc chắn, giao nhanh.', TRUE),
        (13, uid2, 3,  4, 'Điện thoại đẹp, màn hình sắc, nhưng sạc hơi chậm so với kỳ vọng.',       TRUE),
        (25, uid2, 4,  5, 'Tai nghe chống ồn đỉnh, đeo thoải mái cả ngày. Âm thanh rất chi tiết.',   TRUE),
        (21, uid3, 5,  4, 'MacBook Air nhẹ, đẹp, phù hợp văn phòng. Fan không kêu, rất yên tĩnh.',   TRUE),
        (11, uid1, 6,  5, 'iPhone 15 chụp ảnh cực đẹp, thiết kế hiện đại. Xứng đáng từng đồng!',    TRUE),
        (17, uid3, 7,  4, 'Laptop chiến game ngon, màn hình 165Hz rất mượt. Hơi nóng khi full tải.', TRUE),
        (7,  uid2, 8,  5, 'Loa Marshall âm thanh ấm áp, tuyệt vời cho nghe nhạc. Thiết kế vintage đẹp.', TRUE)
        ON CONFLICT DO NOTHING;

        -- ============================================================
        -- PHẦN 8: WISHLIST MẪU
        -- ============================================================
        INSERT INTO "Wishlists" ("UserID","ProductID") VALUES
        (uid1, 2), (uid1, 21), (uid1, 25), (uid1, 35),
        (uid2, 1), (uid2, 17), (uid2, 30),
        (uid3, 3), (uid3, 5),  (uid3, 29), (uid3, 34)
        ON CONFLICT DO NOTHING;

        RAISE NOTICE '✅ Seed Orders, OrderDetails, Reviews, Wishlists: THÀNH CÔNG';
    ELSE
        RAISE NOTICE '⚠️  Chưa tìm thấy customer1@gmail.com — Hãy tạo tài khoản qua web trước!';
        RAISE NOTICE '   Sau đó chạy lại file 02_Seed_Data.sql';
    END IF;
END $$;

-- ============================================================================
-- KIỂM TRA KẾT QUẢ
-- ============================================================================
SELECT 'Categories'     AS "Bảng", COUNT(*) AS "Số bản ghi" FROM "Categories"
UNION ALL SELECT 'Products',       COUNT(*) FROM "Products"
UNION ALL SELECT 'ProductImages',  COUNT(*) FROM "ProductImages"
UNION ALL SELECT 'PromotionalCodes',COUNT(*) FROM "PromotionalCodes"
UNION ALL SELECT 'Orders',         COUNT(*) FROM "Orders"
UNION ALL SELECT 'OrderDetails',   COUNT(*) FROM "OrderDetails"
UNION ALL SELECT 'Reviews',        COUNT(*) FROM "Reviews"
UNION ALL SELECT 'Wishlists',      COUNT(*) FROM "Wishlists"
ORDER BY 1;
