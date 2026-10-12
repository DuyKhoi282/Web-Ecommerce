BEGIN;

DELETE FROM "ProductImages";

-- Product 1: iPhone 15 Pro Max 256GB Titan Tự Nhiên
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(1, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/09/iphone-15-pro-titan-trang.jpg', true, 1),
(1, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/10/iphone-15-pro-max-titan-tu-nhien-3.jpg', false, 2);

-- Product 2: Samsung Galaxy S24 Ultra 5G 512GB
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(2, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/11/Samsung-Galaxy-S24-Ultra-3.jpeg', true, 1),
(2, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/11/render-Galaxy-S24-series-7.jpeg', false, 2);

-- Product 3: MacBook Pro 14 M3 Pro 18GB/512GB Space Black
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(3, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/10/apple-macbook-pro-m3-pro-space-black-3.jpg', true, 1),
(3, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/10/apple-macbook-pro-m3-pro-space-black-2.jpg', false, 2);

-- Product 4: Dell XPS 13 Plus 9320 Core i7-1360P
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(4, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/t/e/text_ng_n_7__4_128.png', true, 1),
(4, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/t/e/text_ng_n_19__3.png', false, 2);

-- Product 5: Tai nghe Apple AirPods Pro 2 USB-C
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(5, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/a/i/airpods_pro_2_sep24_pdp_image_position_2__vn-vi.jpg', true, 1),
(5, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/a/i/airpods_pro_2_sep24_pdp_image_position_7__vn-vi.jpg', false, 2);

-- Product 6: Apple Watch Series 9 GPS 45mm Nhôm
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(6, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/Watch/Apple/Series-9/apple-watch-series-9-5.JPG', true, 1),
(6, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:90/plain/https://cellphones.com.vn/media/wysiwyg/Watch/Apple/Series-9/apple-watch-series-9-45mm-4g-vien-thep-day-cao-su-2.jpg', false, 2);

-- Product 7: Loa Bluetooth Marshall Acton III
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(7, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/m/a/marshall_acton_iii_1.png', true, 1),
(7, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/m/a/marshall_acton_iii_7.png', false, 2);

-- Product 8: Củ sạc nhanh Anker 65W GaNPrime 3 cổng
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(8, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/g/r/group_563_16_.png', true, 1),
(8, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/g/r/group_563_13_.png', false, 2);

-- Product 9: iPhone 14 128GB Đỏ
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(9, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/01/Apple-iPhone-14-Plus-10.jpg', true, 1),
(9, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/10/iphone-14-series-co-may-loai-1.jpg', false, 2);

-- Product 10: iPhone 13 128GB Đêm Khuya
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(10, 'https://media.itechstore.com.np/f_webp/img/product/variants/1f3f34ae-0924-4728-a15b-4c8b1615f03a/iphone-13-midnight-main.png', true, 1),
(10, 'https://thumbs.dreamstime.com/b/apple-iphone-smart-phone-midnight-color-different-colors-touch-screen-world-technology-kyiv-ukraine-september-229864254.jpg', false, 2);

-- Product 11: iPhone 15 256GB Hồng
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(11, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/12/iphone-mau-hong-8.jpg', true, 1),
(11, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/09/tinh-nang-iPhone-15-1.jpeg', false, 2);

-- Product 12: iPhone 12 64GB Trắng
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(12, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/_/0/_0002_iphone_12_white_hero_2-up_cropped_screen__usen_5_1_2.jpg', true, 1),
(12, 'https://cellphones.com.vn/sforum/wp-content/uploads/2022/04/Apple-iPhone-SE-2022-vs-iPhone-3.jpeg', false, 2);

-- Product 13: Samsung Galaxy A55 5G 256GB
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(13, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/s/m/sm-a556_galaxy_a55_awesome_iceblue_ui_2_1.png', true, 1),
(13, 'https://cdn.cellphones.com.vn/media/wysiwyg/Phone/Samsung/samsung_a/samsung-galaxy-a55-1.jpg', false, 2);

-- Product 14: Samsung Galaxy S23 FE 256GB
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(14, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/06/Samsung-Galaxy-S23-FE-thiet-ke-1.jpeg', true, 1),
(14, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/03/Samsung-Galaxy-S23-FE-6.jpg', false, 2);

-- Product 15: Samsung Galaxy Z Flip5 256GB
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(15, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/07/Galaxy-Z-Flip-5-and-Fold-5-offic.jpeg', true, 1),
(15, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/05/Samsung-Galaxy-Z-Flip-5-Colors-1.jpeg', false, 2);

-- Product 16: Samsung Galaxy A34 5G 128GB
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(16, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/s/m/sm-a346_galaxy_a34_5g_awesome_graphite_front_2.png', true, 1),
(16, 'https://cellphones.com.vn/media/catalog/product/s/m/sm-a346_galaxy_a34_5g_awesome_silver_front_2_1_1.png', false, 2);

-- Product 17: ASUS ROG Strix G16 RTX 4070
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(17, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/04/ROG-Strix-G16-2023-5.jpg', true, 1),
(17, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/laptop/Asus/Rog/laptop-asus-rog-strix-g16-g615lw-s5219w-1.jpg', false, 2);

-- Product 18: Acer Nitro 5 AN515 RTX 4060
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(18, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/_/0/_0001_61369_laptop_acer_gaming_nitro_5_2__1_1.jpg', true, 1),
(18, 'https://cellphones.com.vn/sforum/wp-content/uploads/2020/05/CPS-Acer-Nitro-5-2020-6.jpg', false, 2);

-- Product 19: Lenovo LOQ 15APH8 RTX 4060
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(19, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:90/plain/https://cellphones.com.vn/media/wysiwyg/laptop/Lenovo/LOQ/lenovo-loq-13.jpg', true, 1),
(19, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:90/plain/https://cellphones.com.vn/media/wysiwyg/laptop/Lenovo/LOQ/lenovo-loq-16.jpg', false, 2);

-- Product 20: MSI Katana 15 RTX 4070
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(20, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/laptop/MSI/Katana/laptop-msi-katana-15-hx-b14wgk-023vn-6.jpg', true, 1),
(20, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:90/plain/https://cellphones.com.vn/media/wysiwyg/laptop/MSI/Katana/msi-katana-gia.jpg', false, 2);

-- Product 21: MacBook Air M2 8GB/256GB Midnight
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(21, 'https://cdn.cellphones.com.vn/media/wysiwyg/laptop/macbook-air-m2-1.jpg', true, 1),
(21, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/06/Macbook-Air-M2-15inch-4.jpg', false, 2);

-- Product 22: LG Gram 14 2024 Intel Core Ultra 7
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(22, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:90/plain/https://cellphones.com.vn/media/wysiwyg/laptop/LG/LG-Gram-2024/laptop-lg-gram-2024-14z90s-g-ah75a5-2.jpg', true, 1),
(22, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/laptop/LG/LG-Gram-2024/laptop-lg-gram-2024-14z90s-g-ah75a5-2.jpg', false, 2);

-- Product 23: Asus Zenbook 14 OLED UX3405
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(23, 'https://press.asus.com/assets/w_1024,h_1024/81924e9b-0644-4e0f-85eb-c5e748f84180/ASUS%20Zenbook%2014%20OLED_UX3405MA_Product%20photo_3S_Foggy%20Silver_03.PNG', true, 1),
(23, 'https://laptopmedia.com/wp-content/uploads/2024/01/8-4-e1704539640224.jpg', false, 2);

-- Product 24: HP EliteBook 840 G10 Core i7
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(24, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/l/a/laptop-hp-elitebook-840-g10-877m6pa_3_.png', true, 1),
(24, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/l/a/laptop-hp-elitebook-840-g10-877m6pa.png', false, 2);

-- Product 25: Sony WH-1000XM5 Chống ồn
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(25, 'https://cellphones.com.vn/sforum/wp-content/uploads/2022/06/Sony-WH-1000MX5-4.jpg', true, 1),
(25, 'https://cellphones.com.vn/sforum/wp-content/uploads/2022/06/Sony-WH-1000MX5-6.jpg', false, 2);

-- Product 26: JBL Charge 5 Loa Bluetooth
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(26, 'https://cellphones.com.vn/sforum/wp-content/uploads/2021/10/jbl-charge-5-15-scaled.jpg', true, 1),
(26, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/l/o/loa-jbl-charge-5-xanh-nhat-teal-4.jpg', false, 2);

-- Product 27: Bose QuietComfort 45
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(27, 'https://pisces.bbystatic.com/image2/BestBuy_US/images/products/6517/6517766cv16d.jpg', true, 1),
(27, 'https://pisces.bbystatic.com/image2/BestBuy_US/images/products/6517/6517766_sd.jpg', false, 2);

-- Product 28: Samsung Galaxy Buds2 Pro
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(28, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/1/_/1_444.png', true, 1),
(28, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/s/a/samsung-galaxy-buds-2-pro-_8_.png', false, 2);

-- Product 29: Garmin Fenix 7S Solar
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(29, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/l/f/lf-lg-83ac89a4-d8c9-4c69-a491-e5b798a8d39c_1.jpg', true, 1),
(29, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/d/o/dong-ho-thong-minh-garmin-fenix-7s-solar-5.jpg', false, 2);

-- Product 30: Samsung Galaxy Watch6 Classic 47mm
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(30, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/s/m/sm-r960_001_front_silver_1.png', true, 1),
(30, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/s/m/sm-r960_003_l_perspective_silver_1_1.png', false, 2);

-- Product 31: Xiaomi Smart Band 8 Pro
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(31, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/09/xiaomi-band-8-ra-mat-09.jpg', true, 1),
(31, 'https://cellphones.com.vn/sforum/wp-content/uploads/2024/03/Xiaomi-Smart-Band-8-Pro_01.png', false, 2);

-- Product 32: Sony Alpha ZV-E10 Kit 16-50mm
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(32, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/f/u/fuji_4__16.png', true, 1),
(32, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/May-anh/Sony/may-anh-sony-zv-e10-12.jpg', false, 2);

-- Product 33: GoPro Hero12 Black
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(33, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/09/Go-Pro-Hero12-Black-22.jpg', true, 1),
(33, 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/09/Go-Pro-Hero12-Black-3.jpg', false, 2);

-- Product 34: Chuột Gaming Logitech G Pro X Superlight 2
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(34, 'https://cdn2.cellphones.com.vn/x/media/catalog/product/c/h/chuot-gaming-logitech-pro-x-superlight-2-dex_4_.png', true, 1),
(34, 'https://cdn2.cellphones.com.vn/x,webp,q100/media/wysiwyg/phu-kien/chuot/chuot-khong-day/Logitech/Chuot-khong-day-Logitech-G-Pro-X-Superlight-1.jpeg', false, 2);

-- Product 35: Bàn phím cơ Keychron K2 Pro RGB
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(35, 'https://cdn.shopify.com/s/files/1/0059/0630/1017/t/5/assets/keychronk2prowirelesscustommechanicalkeyboard-1670291563312.jpg?v=1670291565', true, 1),
(35, 'https://cdn.shopify.com/s/files/1/0059/0630/1017/t/5/assets/k2proiso1-1679368596956.jpg?v=1679368600', false, 2);

-- Product 36: Ghế Gaming DXRacer Formula Series
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(36, 'https://www.dxracer-europe.com/img/bilder/artiklar/zoom/38047_2.jpg?m=1674747264?w=500', true, 1),
(36, 'https://sumbar-computer.com/storage/ip/rOCWUNkxm24ENfvaKpEN.jpg', false, 2);

-- Product 37: Loa thông minh Amazon Echo Dot Gen5
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(37, 'https://m.media-amazon.com/images/G/35/kindle/journeys/Mvq9IW0GZYhQQxsK3yOI2P3mAwEpOFjw7I006Eo32BTY3D/Y2YwZDUxOWQt._CB608570751_.jpg', true, 1),
(37, 'https://pisces.bbystatic.com/image2/BestBuy_US/images/products/6522/6522268cv12d.jpg', false, 2);

-- Product 38: Bóng đèn thông minh Philips Hue White
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(38, 'https://www.smartgearcompare.nl/wp-content/uploads/2022/08/Philips-Hue-E14-Single-Bulb-White-Ambiance-Bluetooth.jpg', true, 1),
(38, 'https://smoothsales.com.au/cdn/shop/products/e27a602022_1_1024x1024.jpg?v=1664338151', false, 2);

-- Product 39: Camera an ninh TP-Link Tapo C200
INSERT INTO "ProductImages" ("ProductID", "ImageURL", "IsMain", "DisplayOrder") VALUES
(39, 'https://cellphones.com.vn/sforum/wp-content/uploads/2020/10/TP-Link-C200-2.jpg', true, 1),
(39, 'https://cdn2.cellphones.com.vn/insecure/rs:fill:0:0/q:100/plain/https://cellphones.com.vn/media/wysiwyg/camera/camera-an-ninh/TP-Link/Camera-IP-Wifi-TP-Link-Tapo-C200-360-1080P-2MP-2.jpg', false, 2);

COMMIT;
