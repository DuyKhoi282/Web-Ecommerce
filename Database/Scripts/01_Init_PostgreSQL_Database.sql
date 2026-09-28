-- ============================================================================
-- ĐỒ ÁN MÔN HỌC: PHÁT TRIỂN ỨNG DỤNG WEB (ITE1265E)
-- ĐỀ TÀI: TOPIC 1 - WEBSITE BÁN HÀNG TRỰC TUYẾN (ONLINE SHOPPING E-COMMERCE)
-- HỆ QUẢN TRỊ CƠ SỞ DỮ LIỆU: PostgreSQL 14/15/16
-- PHƯƠNG PHÁP: DATABASE FIRST (ENTITY FRAMEWORK 6 + NPGSQL PROVIDER)
-- ============================================================================

-- 1. BẢNG PHÂN QUYỀN (AspNetRoles)
CREATE TABLE IF NOT EXISTS "AspNetRoles" (
    "Id" VARCHAR(128) PRIMARY KEY,
    "Name" VARCHAR(256) NOT NULL UNIQUE
);

-- 2. BẢNG NGƯỜI DÙNG (AspNetUsers)
CREATE TABLE IF NOT EXISTS "AspNetUsers" (
    "Id" VARCHAR(128) PRIMARY KEY,
    "UserName" VARCHAR(256) NOT NULL UNIQUE,
    "Email" VARCHAR(256) UNIQUE,
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash" TEXT,
    "SecurityStamp" TEXT,
    "PhoneNumber" VARCHAR(50),
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEndDateUtc" TIMESTAMP WITH TIME ZONE,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "AccessFailedCount" INT NOT NULL DEFAULT 0,
    "FullName" VARCHAR(150),
    "Address" VARCHAR(300),
    "Avatar" VARCHAR(300),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 3. BẢNG LIÊN KẾT NGƯỜI DÙNG & VAI TRÒ (AspNetUserRoles)
CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
    "UserId" VARCHAR(128) NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "RoleId" VARCHAR(128) NOT NULL REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("UserId", "RoleId")
);

-- 3.1 BẢNG CLAIMS NGƯỜI DÙNG (AspNetUserClaims)
CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" VARCHAR(128) NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "ClaimType" TEXT,
    "ClaimValue" TEXT
);

-- 3.2 BẢNG ĐĂNG NHẬP BÊN THỨ 3 (AspNetUserLogins)
CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
    "LoginProvider" VARCHAR(128) NOT NULL,
    "ProviderKey" VARCHAR(128) NOT NULL,
    "UserId" VARCHAR(128) NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("LoginProvider", "ProviderKey", "UserId")
);

-- 4. BẢNG DANH MỤC SẢN PHẨM (Categories)
CREATE TABLE IF NOT EXISTS "Categories" (
    "CategoryID" SERIAL PRIMARY KEY,
    "ParentCategoryID" INT REFERENCES "Categories"("CategoryID") ON DELETE RESTRICT,
    "Name" VARCHAR(150) NOT NULL,
    "Description" TEXT,
    "ImageURL" VARCHAR(300),
    "DisplayOrder" INT NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 5. BẢNG SẢN PHẨM (Products)
CREATE TABLE IF NOT EXISTS "Products" (
    "ProductID" SERIAL PRIMARY KEY,
    "CategoryID" INT NOT NULL REFERENCES "Categories"("CategoryID") ON DELETE RESTRICT,
    "Name" VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "Price" NUMERIC(18, 2) NOT NULL,
    "DiscountPrice" NUMERIC(18, 2),
    "StockQuantity" INT NOT NULL DEFAULT 0,
    "Status" INT NOT NULL DEFAULT 1, -- 1: Còn hàng, 2: Hết hàng, 3: Ngừng bán
    "ViewCount" INT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 6. BẢNG HÌNH ẢNH SẢN PHẨM (ProductImages)
CREATE TABLE IF NOT EXISTS "ProductImages" (
    "ImageID" SERIAL PRIMARY KEY,
    "ProductID" INT NOT NULL REFERENCES "Products"("ProductID") ON DELETE CASCADE,
    "ImageURL" VARCHAR(300) NOT NULL,
    "IsMain" BOOLEAN NOT NULL DEFAULT FALSE,
    "DisplayOrder" INT NOT NULL DEFAULT 0
);

-- 7. BẢNG GIỎ HÀNG (Carts)
CREATE TABLE IF NOT EXISTS "Carts" (
    "CartID" SERIAL PRIMARY KEY,
    "UserID" VARCHAR(128) REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 8. BẢNG CHI TIẾT GIỎ HÀNG (CartItems)
CREATE TABLE IF NOT EXISTS "CartItems" (
    "CartItemID" SERIAL PRIMARY KEY,
    "CartID" INT NOT NULL REFERENCES "Carts"("CartID") ON DELETE CASCADE,
    "ProductID" INT NOT NULL REFERENCES "Products"("ProductID") ON DELETE CASCADE,
    "Quantity" INT NOT NULL DEFAULT 1 CHECK ("Quantity" > 0),
    "UnitPriceAtAddition" NUMERIC(18, 2) NOT NULL
);

-- 9. BẢNG MÃ GIẢM GIÁ (PromotionalCodes)
CREATE TABLE IF NOT EXISTS "PromotionalCodes" (
    "PromoCodeID" SERIAL PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Description" TEXT,
    "DiscountType" VARCHAR(20) NOT NULL DEFAULT 'Percentage', -- 'Percentage' hoặc 'Fixed'
    "DiscountValue" NUMERIC(18, 2) NOT NULL,
    "MinOrderAmount" NUMERIC(18, 2) NOT NULL DEFAULT 0,
    "MaxUsage" INT NOT NULL DEFAULT 100,
    "CurrentUsage" INT NOT NULL DEFAULT 0,
    "StartDate" TIMESTAMP NOT NULL,
    "EndDate" TIMESTAMP NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 10. BẢNG ĐƠN HÀNG (Orders)
CREATE TABLE IF NOT EXISTS "Orders" (
    "OrderID" SERIAL PRIMARY KEY,
    "UserID" VARCHAR(128) REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL,
    "PromoCodeID" INT REFERENCES "PromotionalCodes"("PromoCodeID") ON DELETE SET NULL,
    "OrderDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "TotalAmount" NUMERIC(18, 2) NOT NULL,
    "DiscountAmount" NUMERIC(18, 2) NOT NULL DEFAULT 0,
    "FinalAmount" NUMERIC(18, 2) NOT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Confirmed, Processing, Shipped, Delivered, Cancelled
    "ShippingAddress" VARCHAR(300) NOT NULL,
    "ReceiverPhone" VARCHAR(50) NOT NULL,
    "PaymentMethod" VARCHAR(50) NOT NULL DEFAULT 'COD', -- COD, BankTransfer
    "PaymentStatus" VARCHAR(50) NOT NULL DEFAULT 'Unpaid', -- Unpaid, PendingConfirmation, Paid
    "PaymentDate" TIMESTAMP,
    "Notes" TEXT
);

-- 11. BẢNG CHI TIẾT ĐƠN HÀNG (OrderDetails)
CREATE TABLE IF NOT EXISTS "OrderDetails" (
    "OrderDetailID" SERIAL PRIMARY KEY,
    "OrderID" INT NOT NULL REFERENCES "Orders"("OrderID") ON DELETE CASCADE,
    "ProductID" INT NOT NULL REFERENCES "Products"("ProductID") ON DELETE RESTRICT,
    "Quantity" INT NOT NULL DEFAULT 1 CHECK ("Quantity" > 0),
    "UnitPrice" NUMERIC(18, 2) NOT NULL,
    "Subtotal" NUMERIC(18, 2) NOT NULL
);

-- 12. BẢNG ĐÁNH GIÁ SẢN PHẨM (Reviews)
CREATE TABLE IF NOT EXISTS "Reviews" (
    "ReviewID" SERIAL PRIMARY KEY,
    "ProductID" INT NOT NULL REFERENCES "Products"("ProductID") ON DELETE CASCADE,
    "UserID" VARCHAR(128) NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "OrderID" INT NOT NULL REFERENCES "Orders"("OrderID") ON DELETE CASCADE,
    "Rating" INT NOT NULL CHECK ("Rating" >= 1 AND "Rating" <= 5),
    "Comment" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsApproved" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "UQ_Review_Order_Product" UNIQUE ("OrderID", "ProductID")
);

-- 13. BẢNG SẢN PHẨM YÊU THÍCH (Wishlists)
CREATE TABLE IF NOT EXISTS "Wishlists" (
    "WishlistID" SERIAL PRIMARY KEY,
    "UserID" VARCHAR(128) NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "ProductID" INT NOT NULL REFERENCES "Products"("ProductID") ON DELETE CASCADE,
    "AddedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "UQ_Wishlist_User_Product" UNIQUE ("UserID", "ProductID")
);

-- ============================================================================
-- DỮ LIỆU KHỞI TẠO BAN ĐẦU (SEED DATA)
-- ============================================================================

-- Seed Roles
INSERT INTO "AspNetRoles" ("Id", "Name") VALUES
('role-customer', 'Customer'),
('role-manager', 'StoreManager'),
('role-admin', 'Administrator')
ON CONFLICT ("Id") DO NOTHING;

-- Seed Danh mục mẫu
INSERT INTO "Categories" ("CategoryID", "Name", "Description", "DisplayOrder", "IsActive") VALUES
(1, 'Điện thoại & Tablet', 'Các dòng smartphone, máy tính bảng chính hãng cao cấp', 1, TRUE),
(2, 'Laptop & Máy tính', 'Laptop văn phòng, gaming, đồ họa và linh kiện máy tính', 2, TRUE),
(3, 'Phụ kiện công nghệ', 'Tai nghe không dây, sạc cáp nhanh, bao da và ốp lưng cao cấp', 3, TRUE),
(4, 'Đồng hồ thông minh', 'Smartwatch theo dõi sức khỏe và thể thao phong cách', 4, TRUE),
(5, 'Thiết bị âm thanh', 'Loa Bluetooth, soundbar và micro thu âm chất lượng cao', 5, TRUE)
ON CONFLICT ("CategoryID") DO NOTHING;

-- Reset sequence cho Categories
SELECT setval(pg_get_serial_sequence('"Categories"', 'CategoryID'), coalesce(max("CategoryID"), 1)) FROM "Categories";

-- Seed Sản phẩm mẫu
INSERT INTO "Products" ("ProductID", "CategoryID", "Name", "Description", "Price", "DiscountPrice", "StockQuantity", "Status", "ViewCount") VALUES
(1, 1, 'iPhone 15 Pro Max 256GB Titan Tự Nhiên', 'Chip A17 Pro mạnh mẽ, camera tiềm vọng 5x, khung titan siêu bền nhẹ.', 32990000, 29990000, 25, 1, 150),
(2, 1, 'Samsung Galaxy S24 Ultra 5G 512GB', 'Snapdragon 8 Gen 3 for Galaxy, bút S-Pen tích hợp, tính năng Galaxy AI thông minh.', 31490000, 27990000, 18, 1, 120),
(3, 2, 'MacBook Pro 14 M3 Pro 18GB/512GB Space Black', 'Màn hình Liquid Retina XDR 120Hz, pin trâu đến 18 tiếng liên tục.', 49990000, 46490000, 10, 1, 95),
(4, 2, 'Dell XPS 13 Plus 9320 Core i7-1360P', 'Thiết kế kính tương lai, màn hình cảm ứng 3.5K OLED sắc nét.', 38500000, 35900000, 12, 1, 80),
(5, 3, 'Tai nghe Apple AirPods Pro 2 USB-C', 'Chống ồn chủ động ANC thế hệ mới, cổng sạc Type-C tiêu chuẩn.', 5990000, 5290000, 50, 1, 210),
(6, 4, 'Apple Watch Series 9 GPS 45mm Nhôm', 'Cảm ứng cử chỉ chạm đúp Double Tap, màn hình sáng 2000 nits ngoài trời.', 10990000, 9890000, 15, 1, 65),
(7, 5, 'Loa Bluetooth Marshall Acton III', 'Chất âm mộc mạc cổ điển, âm trường rộng mở, kết nối Bluetooth 5.2.', 6990000, 6490000, 8, 1, 140),
(8, 3, 'Củ sạc nhanh Anker 65W GaNPrime 3 cổng', 'Công nghệ sạc nhanh GaN, sạc đồng thời Laptop và Smartphone.', 1250000, 990000, 4, 1, 45) -- Cảnh báo sắp hết hàng (< 5)
ON CONFLICT ("ProductID") DO NOTHING;

-- Reset sequence cho Products
SELECT setval(pg_get_serial_sequence('"Products"', 'ProductID'), coalesce(max("ProductID"), 1)) FROM "Products";

-- Seed Voucher mẫu
INSERT INTO "PromotionalCodes" ("PromoCodeID", "Code", "Description", "DiscountType", "DiscountValue", "MinOrderAmount", "MaxUsage", "CurrentUsage", "StartDate", "EndDate", "IsActive") VALUES
(1, 'WELCOME2026', 'Giảm ngay 10% cho đơn hàng đầu tiên chào mừng thành viên mới', 'Percentage', 10, 500000, 500, 12, '2026-01-01', '2026-12-31', TRUE),
(2, 'TECHVIP500K', 'Giảm trực tiếp 500.000 VNĐ cho đơn hàng công nghệ từ 5 triệu', 'Fixed', 500000, 5000000, 100, 8, '2026-01-01', '2026-12-31', TRUE)
ON CONFLICT ("PromoCodeID") DO NOTHING;

SELECT setval(pg_get_serial_sequence('"PromotionalCodes"', 'PromoCodeID'), coalesce(max("PromoCodeID"), 1)) FROM "PromotionalCodes";
