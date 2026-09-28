-- ============================================================================
-- FILE: 99_Reset_Data.sql
-- MỤC ĐÍCH: Xoá toàn bộ dữ liệu nghiệp vụ, giữ lại cấu trúc bảng
-- ⚠️  CẢNH BÁO: KHÔNG THỂ HOÀN TÁC — Chỉ dùng trong môi trường development!
-- ============================================================================
-- CÓ 2 CHẾ ĐỘ:
--   MODE A: Xoá data nghiệp vụ (giữ lại Admin account + Roles)  ← KHUYÊN DÙNG
--   MODE B: Xoá TOÀN BỘ kể cả Users/Roles (reset về trạng thái trống hoàn toàn)
-- ============================================================================

-- ============================================================================
-- MODE A: XOÁ DATA NGHIỆP VỤ (Giữ lại Admin + Roles + Customer mẫu)
-- ============================================================================

BEGIN;

-- Bước 1: Xoá theo thứ tự khoá ngoại (con trước, cha sau)
TRUNCATE TABLE "Reviews"          RESTART IDENTITY CASCADE;
TRUNCATE TABLE "Wishlists"        RESTART IDENTITY CASCADE;
TRUNCATE TABLE "OrderDetails"     RESTART IDENTITY CASCADE;
TRUNCATE TABLE "Orders"           RESTART IDENTITY CASCADE;
TRUNCATE TABLE "CartItems"        RESTART IDENTITY CASCADE;
TRUNCATE TABLE "Carts"            RESTART IDENTITY CASCADE;
TRUNCATE TABLE "ProductImages"    RESTART IDENTITY CASCADE;
TRUNCATE TABLE "Products"         RESTART IDENTITY CASCADE;
TRUNCATE TABLE "Categories"       RESTART IDENTITY CASCADE;
TRUNCATE TABLE "PromotionalCodes" RESTART IDENTITY CASCADE;

-- Bước 2: Reset sequence về 1
SELECT setval(pg_get_serial_sequence('"Categories"',      'CategoryID'),     1, FALSE);
SELECT setval(pg_get_serial_sequence('"Products"',        'ProductID'),      1, FALSE);
SELECT setval(pg_get_serial_sequence('"ProductImages"',   'ImageID'),        1, FALSE);
SELECT setval(pg_get_serial_sequence('"Carts"',           'CartID'),         1, FALSE);
SELECT setval(pg_get_serial_sequence('"CartItems"',       'CartItemID'),     1, FALSE);
SELECT setval(pg_get_serial_sequence('"PromotionalCodes"','PromoCodeID'),    1, FALSE);
SELECT setval(pg_get_serial_sequence('"Orders"',          'OrderID'),        1, FALSE);
SELECT setval(pg_get_serial_sequence('"OrderDetails"',    'OrderDetailID'),  1, FALSE);
SELECT setval(pg_get_serial_sequence('"Reviews"',         'ReviewID'),       1, FALSE);
SELECT setval(pg_get_serial_sequence('"Wishlists"',       'WishlistID'),     1, FALSE);

COMMIT;

-- Kiểm tra kết quả MODE A
SELECT 'Categories'       AS "Bảng", COUNT(*) AS "Còn lại" FROM "Categories"
UNION ALL SELECT 'Products',          COUNT(*) FROM "Products"
UNION ALL SELECT 'Orders',            COUNT(*) FROM "Orders"
UNION ALL SELECT 'AspNetUsers',       COUNT(*) FROM "AspNetUsers"
ORDER BY 1;

RAISE NOTICE '✅ MODE A: Đã xoá data nghiệp vụ. Tài khoản Admin/Users được giữ lại.';

-- ============================================================================
-- MODE B: XOÁ TOÀN BỘ (bao gồm Users, Roles — reset hoàn toàn)
-- ⚠️  Bỏ comment khối dưới đây để kích hoạt MODE B
-- ============================================================================

/*
BEGIN;

TRUNCATE TABLE "Reviews"          RESTART IDENTITY CASCADE;
TRUNCATE TABLE "Wishlists"        RESTART IDENTITY CASCADE;
TRUNCATE TABLE "OrderDetails"     RESTART IDENTITY CASCADE;
TRUNCATE TABLE "Orders"           RESTART IDENTITY CASCADE;
TRUNCATE TABLE "CartItems"        RESTART IDENTITY CASCADE;
TRUNCATE TABLE "Carts"            RESTART IDENTITY CASCADE;
TRUNCATE TABLE "ProductImages"    RESTART IDENTITY CASCADE;
TRUNCATE TABLE "Products"         RESTART IDENTITY CASCADE;
TRUNCATE TABLE "Categories"       RESTART IDENTITY CASCADE;
TRUNCATE TABLE "PromotionalCodes" RESTART IDENTITY CASCADE;

-- Xoá Identity tables (Users, Roles)
DELETE FROM "AspNetUserClaims";
DELETE FROM "AspNetUserLogins";
DELETE FROM "AspNetUserRoles";
DELETE FROM "AspNetUsers";
DELETE FROM "AspNetRoles";

-- Reset tất cả sequences
SELECT setval(pg_get_serial_sequence('"Categories"',      'CategoryID'),     1, FALSE);
SELECT setval(pg_get_serial_sequence('"Products"',        'ProductID'),      1, FALSE);
SELECT setval(pg_get_serial_sequence('"ProductImages"',   'ImageID'),        1, FALSE);
SELECT setval(pg_get_serial_sequence('"Carts"',           'CartID'),         1, FALSE);
SELECT setval(pg_get_serial_sequence('"CartItems"',       'CartItemID'),     1, FALSE);
SELECT setval(pg_get_serial_sequence('"PromotionalCodes"','PromoCodeID'),    1, FALSE);
SELECT setval(pg_get_serial_sequence('"Orders"',          'OrderID'),        1, FALSE);
SELECT setval(pg_get_serial_sequence('"OrderDetails"',    'OrderDetailID'),  1, FALSE);
SELECT setval(pg_get_serial_sequence('"Reviews"',         'ReviewID'),       1, FALSE);
SELECT setval(pg_get_serial_sequence('"Wishlists"',       'WishlistID'),     1, FALSE);
SELECT setval(pg_get_serial_sequence('"AspNetUserClaims"','Id'),             1, FALSE);

COMMIT;

-- Sau khi chạy MODE B: khởi động lại ứng dụng để Startup.cs tái tạo Admin account
*/
