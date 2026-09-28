# 🗄️ Thư Mục Kịch Bản Cơ Sở Dữ Liệu PostgreSQL (SQL Scripts)

Thư mục này dùng để lưu trữ toàn bộ các file kịch bản SQL (`.sql`) khởi tạo và đồng bộ cơ sở dữ liệu **PostgreSQL** của dự án theo phương pháp **Database First**:

---

### 📌 Danh Sách Scripts:
- `01_Init_PostgreSQL_Database.sql`: Script DDL tạo toàn bộ database, các bảng (12 thực thể chuẩn hóa 3NF), khóa chính, khóa ngoại, chỉ mục và dữ liệu khởi tạo ban đầu (Categories, Users mẫu, Roles mặc định).
- `02_Update_<TênTínhNăng>.sql`: Script bổ sung hoặc hiệu chỉnh bảng/cột cho từng sprint (ví dụ: `02_Update_Vouchers.sql`, `03_Update_FlashSale.sql`).

---

### 🚀 Hướng Dẫn Thực Thi Scripts Trên PostgreSQL:

#### Cách 1: Sử dụng công cụ đồ họa pgAdmin 4
1. Mở **pgAdmin 4** và kết nối vào PostgreSQL Server (mặc định Port: `5432`).
2. Nhấp chuột phải vào **Databases** &rarr; Chọn **Create** &rarr; **Database...** &rarr; Đặt tên CSDL là `webecommerce_db`.
3. Nhấp chuột phải vào `webecommerce_db` vừa tạo &rarr; Chọn **Query Tool**.
4. Mở nội dung file `01_Init_PostgreSQL_Database.sql` (hoặc nhấn `Ctrl + O` để mở file).
5. Nhấn phím **F5** (hoặc nút **Execute**) để chạy script tạo bảng và nạp dữ liệu mẫu.

#### Cách 2: Sử dụng dòng lệnh `psql`
```bash
psql -U postgres -h localhost -p 5432 -d webecommerce_db -f Database/Scripts/01_Init_PostgreSQL_Database.sql
```

---

### 🔄 Đồng Bộ Vào Dự Án ASP.NET MVC 5 (Database First với Npgsql):
1. Sau khi chạy script tạo bảng trên PostgreSQL thành công.
2. Mở Solution trong Visual Studio, mở file `.edmx` trong thư mục `Models/`.
3. Nhấp chuột phải lên vùng trống &rarr; Chọn **Update Model from Database...** &rarr; Chọn kết nối Npgsql tới database `webecommerce_db`.
4. Chọn các bảng mới cần cập nhật và nhấn **Finish** để Entity Framework 6 tự động sinh lại DbContext và Model classes tương ứng.
