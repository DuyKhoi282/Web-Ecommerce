-- ============================================================================
-- SCRIPT: BỔ SUNG ĐÁNH GIÁ MẪU CHO TOÀN BỘ SẢN PHẨM (REVIEWS SEED DATA)
-- Mục đích: Đảm bảo bộ lọc "Sao tối thiểu" (1 đến 5 sao) hoạt động hoàn hảo
-- ============================================================================

DO $$
DECLARE
    uid1 TEXT;
    uid2 TEXT;
    uid3 TEXT;
BEGIN
    SELECT "Id" INTO uid1 FROM "AspNetUsers" WHERE "Email" = 'customer1@gmail.com' LIMIT 1;
    SELECT "Id" INTO uid2 FROM "AspNetUsers" WHERE "Email" = 'customer2@gmail.com' LIMIT 1;
    SELECT "Id" INTO uid3 FROM "AspNetUsers" WHERE "Email" = 'customer3@gmail.com' LIMIT 1;

    IF uid1 IS NULL THEN
        SELECT "Id" INTO uid1 FROM "AspNetUsers" LIMIT 1;
        uid2 := uid1;
        uid3 := uid1;
    END IF;

    -- Thêm đánh giá mẫu phong phú cho các sản phẩm
    INSERT INTO "Reviews" ("ProductID", "UserID", "OrderID", "Rating", "Comment", "IsApproved", "CreatedAt") VALUES
    -- Phụ kiện công nghệ (CategoryID = 3)
    (5,  uid1, 1,  5, 'Chất âm tuyệt hảo, chống ồn chủ động đỉnh cao của AirPods Pro 2!', TRUE, NOW() - INTERVAL '2 days'),
    (5,  uid2, 2,  5, 'Kết nối iPhone cực nhạy, mic đàm thoại rõ ràng khi đi đường.', TRUE, NOW() - INTERVAL '5 days'),
    (8,  uid3, 1,  5, 'Củ sạc Anker nhỏ gọn, sạc nhanh cùng lúc cả laptop lẫn điện thoại mát rượi.', TRUE, NOW() - INTERVAL '3 days'),
    (8,  uid1, 3,  4, 'Hoàn thiện cao cấp, 65W đủ dùng cho MacBook Air và iPhone.', TRUE, NOW() - INTERVAL '7 days'),

    -- Điện thoại & Tablet
    (1,  uid2, 4,  5, 'Màu titan tự nhiên tuyệt đẹp, viền nhẹ hơn hẳn 14 Pro Max.', TRUE, NOW() - INTERVAL '10 days'),
    (2,  uid1, 5,  5, 'Màn hình chống chói Galaxy S24 Ultra quá đỉnh, bút S-Pen tiện lợi.', TRUE, NOW() - INTERVAL '4 days'),
    (9,  uid3, 6,  4, 'iPhone 14 màu đỏ nổi bật, pin trâu dùng cả ngày thoải mái.', TRUE, NOW() - INTERVAL '8 days'),
    (10, uid2, 7,  4, 'Giá hiện tại quá hời cho một chiếc iPhone mượt mà, camera đẹp.', TRUE, NOW() - INTERVAL '12 days'),
    (11, uid1, 8,  5, 'Màu hồng phấn rất xinh, Dynamic Island tiện lợi trên iPhone 15.', TRUE, NOW() - INTERVAL '15 days'),
    (12, uid3, 9,  4, 'Máy phụ hoàn hảo, thiết kế vuông vắn nhỏ gọn dễ cầm.', TRUE, NOW() - INTERVAL '20 days'),
    (13, uid2, 10, 5, 'Khung kim loại chắc chắn, màn hình Super AMOLED 120Hz mượt mà.', TRUE, NOW() - INTERVAL '6 days'),
    (14, uid1, 11, 4, 'Cấu hình mạnh trong tầm giá, màn hình đẹp và chống nước IP68.', TRUE, NOW() - INTERVAL '11 days'),
    (15, uid3, 12, 5, 'Thiết kế gập thời thượng, màn hình ngoài Flex Window dùng rất tiện.', TRUE, NOW() - INTERVAL '9 days'),
    (16, uid2, 13, 4, 'Pin 5000mAh trâu bò, loa kép nghe to rõ, giá rất sinh viên.', TRUE, NOW() - INTERVAL '14 days'),

    -- Laptop & Máy tính
    (3,  uid1, 14, 5, 'Chip M3 Pro cực mạnh, render video 4K nhanh chóng không hề nóng.', TRUE, NOW() - INTERVAL '13 days'),
    (4,  uid2, 15, 4, 'Dell XPS 13 Plus thiết kế tương lai, bàn phím phẳng gõ rất êm.', TRUE, NOW() - INTERVAL '16 days'),
    (17, uid3, 16, 5, 'ROG Strix G16 chiến game max setting mượt mà, tản nhiệt tốt.', TRUE, NOW() - INTERVAL '18 days'),
    (18, uid1, 17, 4, 'Nitro 5 giá tốt trong phân khúc RTX 4060, nâng cấp RAM dễ dàng.', TRUE, NOW() - INTERVAL '22 days'),
    (19, uid2, 18, 4, 'Lenovo LOQ hoàn thiện đầm tay, màn hình 100% sRGB chuẩn màu.', TRUE, NOW() - INTERVAL '25 days'),
    (20, uid3, 19, 4, 'MSI Katana thiết kế hầm hố, bàn phím LED 4 vùng bắt mắt.', TRUE, NOW() - INTERVAL '21 days'),
    (21, uid1, 20, 5, 'MacBook Air M2 mỏng nhẹ tinh tế, pin 15 tiếng không cần sạc.', TRUE, NOW() - INTERVAL '17 days'),
    (22, uid2, 1,  5, 'LG Gram siêu nhẹ chưa tới 1kg, màn hình chống chói sắc nét.', TRUE, NOW() - INTERVAL '19 days'),
    (23, uid3, 2,  5, 'Màn OLED rực rỡ, thời lượng pin ấn tượng trên Zenbook 14.', TRUE, NOW() - INTERVAL '23 days'),
    (24, uid1, 3,  4, 'Laptop chuẩn doanh nhân, bảo mật vân tay và khuôn mặt nhạy.', TRUE, NOW() - INTERVAL '26 days'),

    -- Đồng hồ & Âm thanh
    (6,  uid2, 4,  5, 'Apple Watch S9 sáng rõ ngoài trời, tính năng Double Tap kỳ diệu.', TRUE, NOW() - INTERVAL '8 days'),
    (7,  uid3, 5,  5, 'Loa Marshall Acton 3 bày phòng khách sang trọng, bass ấm.', TRUE, NOW() - INTERVAL '10 days'),
    (25, uid1, 6,  5, 'Chống ồn số 1 thị trường, đệm tai WH-1000XM5 đeo êm ái cả ngày.', TRUE, NOW() - INTERVAL '7 days'),
    (26, uid2, 7,  4, 'JBL Charge 5 chống nước cực tốt, mang đi dã ngoại bao vui.', TRUE, NOW() - INTERVAL '12 days'),
    (27, uid3, 8,  5, 'Bose QC45 chống ồn tự nhiên, không bị bí tai khi nghe lâu.', TRUE, NOW() - INTERVAL '15 days'),
    (28, uid1, 9,  4, 'Chất âm 24-bit sắc nét, đeo vừa vặn nhỏ gọn trong tai.', TRUE, NOW() - INTERVAL '13 days'),
    (29, uid2, 10, 5, 'Pin sạc năng lượng mặt trời dùng cả tháng, GPS Garmin siêu chuẩn.', TRUE, NOW() - INTERVAL '24 days'),
    (30, uid3, 11, 4, 'Viền xoay vật lý truyền thống trở lại rất tiện thao tác.', TRUE, NOW() - INTERVAL '16 days'),
    (31, uid1, 12, 4, 'Màn hình AMOLED lớn, hiển thị thông báo rõ ràng, giá rẻ.', TRUE, NOW() - INTERVAL '19 days'),

    -- Máy ảnh & Gaming & SmartHome
    (32, uid2, 13, 5, 'Sony ZV-E10 quay vlog lấy nét mắt siêu nhanh, màu da tự nhiên.', TRUE, NOW() - INTERVAL '21 days'),
    (33, uid3, 14, 5, 'GoPro 12 chống rung HyperSmooth 6.0 thần thánh, quay 5.3K nét căng.', TRUE, NOW() - INTERVAL '18 days'),
    (34, uid1, 15, 5, 'Chuột siêu nhẹ 60g, cảm biến Hero 2 chuẩn từng milimet.', TRUE, NOW() - INTERVAL '22 days'),
    (35, uid2, 16, 5, 'Keychron gõ êm, kết nối bluetooth 3 thiết bị mượt mà.', TRUE, NOW() - INTERVAL '27 days'),
    (36, uid3, 17, 4, 'Ghế ngồi êm, đỡ cột sống tốt cho người ngồi làm việc lâu.', TRUE, NOW() - INTERVAL '30 days'),
    (37, uid1, 18, 4, 'Echo Dot Gen 5 loa ấm hơn bản cũ, trợ lý Alexa bắt lệnh nhạy.', TRUE, NOW() - INTERVAL '28 days'),
    (38, uid2, 19, 5, 'Bóng đèn đổi màu mượt, kết nối HomeKit tức thì.', TRUE, NOW() - INTERVAL '29 days'),
    (39, uid3, 20, 4, 'Tapo C200 xoay 360 độ nét 1080p, đàm thoại 2 chiều rõ ràng.', TRUE, NOW() - INTERVAL '31 days')
    ON CONFLICT ("OrderID", "ProductID") DO UPDATE 
    SET "Rating" = EXCLUDED."Rating", "Comment" = EXCLUDED."Comment", "IsApproved" = TRUE;

    RAISE NOTICE '✅ Đã hoàn tất bổ sung đánh giá mẫu cho toàn bộ sản phẩm!';
END $$;
