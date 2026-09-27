# 🗄️ Thư Mục Script Cơ Sở Dữ Liệu (SQL Scripts)

Thư mục này dùng để lưu trữ các file kịch bản SQL (`.sql`) khởi tạo và cập nhật cơ sở dữ liệu nếu nhóm lựa chọn tiếp cận theo **Database First**:

### Quy tắc đặt tên file Script:
- `01_Init_Database.sql`: Script tạo toàn bộ database, các bảng và dữ liệu khởi tạo ban đầu (Categories, Users mẫu, Roles).
- `02_Update_<TênTínhNăng>.sql`: Script thêm/sửa bảng hoặc cột cho từng sprint (ví dụ: `02_Update_Vouchers.sql`, `03_Update_FlashSale.sql`).

> **Lưu ý:** Khi có thay đổi về bảng, thành viên viết script SQL vào đây, commit lên Git để các thành viên khác cùng chạy đồng bộ trong SQL Server Management Studio (SSMS).
