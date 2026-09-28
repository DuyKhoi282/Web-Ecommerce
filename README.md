# 🛒 Hệ Thống Thương Mại Điện Tử Trực Tuyến (Online Shopping E-Commerce)

[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg?style=flat-square)]()
[![Platform](https://img.shields.io/badge/.NET_Framework-4.8-blue.svg?style=flat-square)]()
[![Framework](https://img.shields.io/badge/ASP.NET-MVC_5-purple.svg?style=flat-square)]()
[![ORM](https://img.shields.io/badge/Entity_Framework-6_(Database_First)-orange.svg?style=flat-square)]()
[![Database](https://img.shields.io/badge/Database-PostgreSQL_14+-336791.svg?style=flat-square&logo=postgresql&logoColor=white)]()
[![Provider](https://img.shields.io/badge/Provider-Npgsql-004880.svg?style=flat-square)]()
[![Git](https://img.shields.io/badge/Git_Flow-Weekly_CI-brightgreen.svg?style=flat-square)]()

> **Đồ án Cuối kỳ:** Học phần Phát triển Ứng dụng Web (`ITE1265E`)  
> **Đề tài:** Topic 1 – Online Shopping Website  
> **Quy mô dự án:** 4 thành viên &bull; Thời lượng 8 tuần &bull; Tiếp cận Tích hợp Sớm Tuần hoàn (Weekly Integration)

---

## 📑 Mục Lục
- [1. Giới Thiệu Dự Án](#1-giới-thiệu-dự-án)
- [2. Ngăn Xếp Công Nghệ (Tech Stack)](#2-ngăn-xếp-công-nghệ-tech-stack)
- [3. Kiến Trúc & Ma Trận Phân Bổ Nhân Sự](#3-kiến-trúc--ma-trận-phân-bổ-nhân-sự)
- [4. Ma Trận Phân Quyền 3 Cấp (RBAC)](#4-ma-trận-phân-quyền-3-cấp-rbac)
- [5. Đặc Tả Nghiệp Vụ Cốt Lõi](#5-đặc-tả-nghiệp-vụ-cốt-lõi)
- [6. Quy Chuẩn Kỹ Thuật Bắt Buộc](#6-quy-chuẩn-kỹ-thuật-bắt-buộc)
- [7. Quy Chuẩn Git & Nhánh Phát Triển](#7-quy-chuẩn-git--nhánh-phát-triển)
- [8. Yêu Cầu Môi Trường & Cài Đặt](#8-yêu-cầu-môi-trường--cài-đặt)
- [9. Tài Liệu Dự Án & Liên Kết](#9-tài-liệu-dự-án--liên-kết)

---

## 1. Giới Thiệu Dự Án

Hệ thống Website Bán Hàng Trực Tuyến là một ứng dụng web thương mại điện tử đa tầng hoàn chỉnh, được xây dựng trên nền tảng **ASP.NET MVC 5** (.NET Framework 4.8) kết hợp hệ quản trị cơ sở dữ liệu quan hệ mã nguồn mở mạnh mẽ **PostgreSQL**, sử dụng thư viện **Npgsql** làm ADO.NET / EF6 Data Provider và triển khai theo phương pháp **Database First**.

Mục tiêu cốt lõi của đồ án:
- Hiện thực hóa quy trình mua sắm khép kín: Khám phá sản phẩm &rarr; Tìm kiếm / Lọc đa tiêu chí &rarr; Giỏ hàng Ajax &rarr; Áp dụng Voucher / Flash Sale &rarr; Thanh toán (COD / Chuyển khoản QR) &rarr; Quản lý đơn hàng &rarr; Đánh giá phản hồi 5 sao có kiểm chứng.
- Phân quyền chặt chẽ 3 cấp (**Customer**, **Store Manager**, **Administrator**) với ASP.NET Identity 2.0.
- Áp dụng nguyên tắc **Phòng vệ biên (Boundary Defense)** với 100% Action CSDL/File/Thanh toán được bọc `try-catch`, đảm bảo ứng dụng không bao giờ bị dừng đột ngột (Yellow Screen of Death).
- Quản lý và đồng bộ cơ sở dữ liệu đồng nhất giữa 4 thành viên thông qua **Entity Framework 6 Database First**: Toàn bộ bảng, khóa chính, khóa ngoại, chỉ mục được khởi tạo trước bằng script SQL chuẩn trên PostgreSQL, sau đó reverse-engineer tự động sinh `DbContext` và `Model` entities trong Visual Studio.

---

## 2. Ngăn Xếp Công Nghệ (Tech Stack)

| Lớp (Layer) | Công Nghệ & Thư Viện | Mục Đích Sử Dụng |
| :--- | :--- | :--- |
| **Backend Framework** | ASP.NET MVC 5, .NET Framework 4.8 | Xây dựng kiến trúc Model-View-Controller chuẩn doanh nghiệp |
| **Cơ sở dữ liệu** | PostgreSQL (14 / 15 / 16) | Hệ quản trị CSDL quan hệ ACID mạnh mẽ, tối ưu lưu trữ và chỉ mục |
| **Database Provider** | Npgsql, EntityFramework6.Npgsql | Data Provider cung cấp kết nối ADO.NET và dịch vụ EF6 cho PostgreSQL |
| **ORM / Data Access** | Entity Framework 6 (Database First) | Ánh xạ CSDL sang đối tượng Model theo Database First, truy vấn LINQ an toàn |
| **Bảo mật & Phân quyền** | ASP.NET Identity 2.0, OWIN Cookie Authentication | Xác thực người dùng, mã hóa mật khẩu PBKDF2, phân quyền Role-based |
| **Frontend UI** | Bootstrap 5, Razor View Engine, FontAwesome | Giao diện hiện đại, responsive hoàn toàn trên Mobile và Desktop |
| **Client Scripting** | JavaScript, jQuery, Ajax | Cập nhật giỏ hàng mượt mà không tải lại trang (Single Page Feel) |
| **Tiện ích mở rộng** | Rotativa / iTextSharp, MailKit, Chart.js | Xuất hóa đơn PDF, gửi email tự động, vẽ biểu đồ doanh thu Admin |

---

## 3. Kiến Trúc & Ma Trận Phân Bổ Nhân Sự

Hệ thống được thiết kế theo chuỗi giá trị và trật tự phụ thuộc kỹ thuật giữa 4 thành viên:

```
[Thành viên 1: Team Lead]
  ├── Architecture & Foundation (Solution Setup, EF 6, MVC Pattern, Git Flow)
  ├── Authentication & Passwords (Register, Login, Logout, Change/Reset Password)
  ├── User Profile Management (Profile, Avatar Upload, Address, Phone)
  ├── User Management (Admin User List, Search by Name/Email, View Details)
  ├── Account Management (Lock/Unlock, Activate/Deactivate Accounts)
  ├── Role Management (Customer, StoreManager, Administrator, Assign/Change Role)
  ├── Authorization ([Authorize], Role-based Access, Protect Admin/Manager, 403 Forbidden)
  ├── Admin Panel (Dedicated Admin Layout, Navigation Menu, Centralized Views)
  ├── Dashboard & Analytics (Total Revenue, Orders, Users, Top Selling, Low Stock < 5)
  ├── Chart.js Integration (12-Month Revenue Bar Chart, Order Status Pie Chart)
  ├── Security Defense (Anti-CSRF, PBKDF2 Password Hashing, Error Handling)
  └── Weekly Integration & Code Review (PR Auditing, Merge Conflict Resolution)
         │
         ▼
[Thành viên 2] ────────────► [Thành viên 3] ────────────► [Thành viên 4]
  • Category CRUD              • Ajax Cart                  • Wishlist
  • Product CRUD               • Checkout (COD/Bank)        • Verified Reviews
  • Multiple Images            • Order Management           • Export PDF Invoice
  • Search & Multi-Filter      • Voucher & Flash Sale       • MailKit Notifications
```

### Chi Tiết Phân Công Trách Nhiệm:

| Thành Viên | Vai Trò & Phụ Trách | Controller / Deliverables Chính |
| :--- | :--- | :--- |
| **Thành viên 1** *(Team Lead)* | **Authentication, User Management, Authorization & Administration:**<br>• Khởi tạo Solution, cấu hình EF 6 Code First Context & Git workflow.<br>• Xác thực: Register, Login Cookie Auth, Logout, Change Password, Forgot/Reset Password.<br>• Quản lý hồ sơ: Xem/sửa Profile cá nhân, Upload Avatar, cập nhật Địa chỉ, Số điện thoại.<br>• Quản trị User: Xem danh sách người dùng phân trang, tìm kiếm User (tên/email/phone), xem chi tiết User Profile.<br>• Quản lý tài khoản: Khóa/Mở khóa (Lock/Unlock), Kích hoạt/Vô hiệu hóa (Activate/Deactivate).<br>• Phân quyền 3 vai trò: Customer / StoreManager / Administrator, Assign & Change Role.<br>• Kiểm soát truy cập: Bộ lọc `[Authorize]`, Role-based access, bảo vệ trang Admin/Manager, trang 403 Forbidden.<br>• Admin Panel: Layout Admin riêng biệt (_AdminLayout.cshtml), menu/sidebar quản trị chuyên nghiệp.<br>• Dashboard: Báo cáo tổng doanh thu, số đơn hàng, số user, top bán chạy, cảnh báo kho &lt; 5.<br>• Thống kê trực quan: Tích hợp Chart.js vẽ biểu đồ doanh thu theo tháng và cơ cấu đơn hàng.<br>• Bảo mật hệ thống: Phòng vệ Anti-CSRF (`@Html.AntiForgeryToken`), băm mật khẩu Identity PBKDF2.<br>• Tích hợp hệ thống: Review PR, hỗ trợ xử lý merge/conflict và tích hợp code tuần hoàn. | `AccountController`<br>`ManageController`<br>`AdminController`<br>`DashboardController`<br>`Identity Models & Services`<br>`Admin Layout & Views` |
| **Thành viên 2** | **Danh mục, Sản phẩm, Tìm kiếm & Lọc:** CRUD Category, CRUD Product, tải lên nhiều ảnh, phân trang, bộ lọc đa tiêu chí (danh mục, khoảng giá, rating). | `CategoryController`<br>`ProductController`<br>`SearchController`<br>`Product Views` |
| **Thành viên 3** | **Quy trình Mua bán Khép kín:** Giỏ hàng Ajax, Checkout, Xử lý Đơn hàng (trừ tồn kho an toàn), Mã giảm giá (Voucher), Sự kiện Flash Sale đếm ngược. | `CartController`<br>`CheckoutController`<br>`OrderController`<br>`PromotionController` |
| **Thành viên 4** | **Tiện ích, Đánh giá & Dịch vụ Phụ thuộc:** Wishlist, Đánh giá 1-5 sao (ràng buộc đơn Delivered), Xuất hóa đơn PDF, Gửi email thông báo tự động (MailKit). | `WishlistController`<br>`ReviewController`<br>`ExportController`<br>`EmailService` |

---

## 4. Ma Trận Phân Quyền 3 Cấp (RBAC)

| Nghiệp vụ / Tính Năng Hệ Thống | Customer | Store Manager | Administrator |
| :--- | :---: | :---: | :---: |
| Xem sản phẩm, tìm kiếm, lọc, thao tác giỏ hàng | ✅ | ✅ | ✅ |
| Xem và cập nhật Profile cá nhân, Upload Avatar, Đổi mật khẩu | ✅ | ✅ | ✅ |
| Thực hiện Checkout đặt hàng, xem lịch sử mua cá nhân | ✅ | ❌ | ❌ |
| Tự hủy đơn hàng cá nhân (chỉ khi trạng thái `Pending`) | ✅ | ❌ | ❌ |
| Đánh giá 1-5 sao (bắt buộc đơn hàng trạng thái `Delivered`) | ✅ | ❌ | ❌ |
| Quản lý CRUD Danh mục, Sản phẩm, Upload ảnh, Kho hàng | ❌ | ✅ | ✅ |
| Quản lý và cập nhật tiến trình đơn hàng toàn hệ thống | ❌ | ✅ | ✅ |
| Thiết lập Voucher khuyến mãi và khung giờ Flash Sale | ❌ | ✅ | ✅ |
| Xem Dashboard thống kê (doanh thu, đơn hàng, biểu đồ Chart.js) | ❌ | ✅ *(Giới hạn)* | ✅ *(Toàn quyền)* |
| Quản trị User (Xem danh sách, tìm kiếm User, xem chi tiết Profile) | ❌ | ❌ | ✅ *(Độc quyền)* |
| Quản lý tài khoản (Khóa/Mở tài khoản Lock/Unlock, Activate/Deactivate) | ❌ | ❌ | ✅ *(Độc quyền)* |
| Phân quyền vai trò (Gán và Chuyển đổi Role: Customer/Manager/Admin) | ❌ | ❌ | ✅ *(Độc quyền)* |
| Kiểm duyệt và xóa bỏ bình luận / đánh giá vi phạm | ❌ | ❌ | ✅ *(Độc quyền)* |

---

## 5. Đặc Tả Nghiệp Vụ Cốt Lõi

### 5.1. Quy trình Mua hàng & Thanh toán (Payment Contract)
1. **COD (Cash On Delivery):**
   - Đặt hàng &rarr; Trạng thái `PaymentStatus = "Chưa thanh toán"` (Unpaid).
   - Khi Store Manager đổi trạng thái giao hàng thành `Delivered` &rarr; Hệ thống tự động chuyển `PaymentStatus = "Đã thanh toán"` (Paid) và lưu `PaymentDate`.
2. **Chuyển khoản Ngân hàng (Bank Transfer / QR Demo):**
   - Sinh mã QR động kèm nội dung chuyển khoản: `[MÃ ĐƠN] - [SỐ ĐIỆN THOẠI]`.
   - Khách hàng bấm *Xác nhận đã chuyển* &rarr; `PaymentStatus = "Chờ xác nhận tiền"`.
   - Quản lý xác nhận tiền đã vào tài khoản &rarr; `PaymentStatus = "Đã thanh toán"`.

### 5.2. Quản lý Tồn kho & Giao dịch Toàn vẹn (ACID Transaction)
- Thao tác tạo đơn hàng và trừ tồn kho sản phẩm được bọc hoàn toàn trong `DbContextTransaction`:
```csharp
using (var transaction = db.Database.BeginTransaction())
{
    try
    {
        // 1. Tạo đơn hàng và chi tiết đơn hàng
        // 2. Kiểm tra và trừ số lượng tồn kho từng sản phẩm
        // 3. Xóa giỏ hàng
        db.SaveChanges();
        transaction.Commit();
    }
    catch (Exception ex)
    {
        transaction.Rollback();
        // Ghi log và trả thông báo lỗi thân thiện cho người dùng
    }
}
```

---

## 6. Quy Chuẩn Kỹ Thuật Bắt Buộc

1. **Nguyên tắc Phòng vệ biên (Boundary Defense):**
   - 100% các Action Method thao tác với CSDL, File I/O, Đặt hàng, Gửi Mail **bắt buộc bọc trong khối `try-catch`**.
   - Bắt mọi lỗi ngoại lệ (`DbEntityValidationException`, `SqlException`, `Exception`), ghi log và hiển thị thông báo thân thiện qua `ModelState` hoặc `TempData["ErrorMessage"]`.
   - Tuyệt đối không để lộ trang lỗi hệ thống mặc định (Yellow Screen of Death).
2. **Xác thực dữ liệu (Data Annotations):**
   - Mọi Model/ViewModel phải có đầy đủ attributes kiểm tra: `[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]`, `[Display(Name = "...")]`.
3. **Chống tấn công Web phổ biến:**
   - Bảo vệ chống CSRF trên toàn bộ Form POST bằng `[ValidateAntiForgeryToken]` và `@Html.AntiForgeryToken()`.
   - Ngăn chặn SQL Injection thông qua Entity Framework Parametric Queries.
   - Ngăn chặn XSS bằng Razor Auto-Encoding `@Html.DisplayFor()`.

---

## 7. Quy Chuẩn Git & Nhánh Phát Triển

Để phối hợp nhịp nhàng giữa 4 thành viên và tránh xung đột code, dự án tuân thủ nghiêm ngặt quy chế Git (chi tiết xem tại [CONTRIBUTING.md](CONTRIBUTING.md)):

### 7.1. Cấu Trúc Nhánh (Branching Strategy)
- **`main`**: Nhánh phát hành chính thức, luôn trong trạng thái ổn định và có thể chạy ngay. Chỉ Team Lead được merge vào `main`.
- **`develop`**: Nhánh tích hợp chung hàng tuần. Toàn bộ tính năng hoàn thiện được merge vào đây sau khi review.
- **`feature/<tv>-<tên-chức-năng>`**: Nhánh cá nhân để phát triển tính năng riêng.
  - Ví dụ: `feature/tv2-product-crud`, `feature/tv3-ajax-cart`, `feature/tv4-pdf-export`.
- **`bugfix/<tên-lỗi>`**: Nhánh sửa lỗi phát sinh trong quá trình kiểm thử.

### 7.2. Chuẩn Đặt Tên Commit (Conventional Commits)
Cú pháp chuẩn: `<type>(<scope>): <mô tả ngắn bằng tiếng Việt hoặc tiếng Anh>`

- `feat`: Tính năng mới (ví dụ: `feat(cart): implement ajax add to cart`)
- `fix`: Sửa lỗi (ví dụ: `fix(order): fix inventory deduction calculation`)
- `docs`: Cập nhật tài liệu (ví dụ: `docs(readme): update setup guidelines`)
- `style`: Định dạng giao diện, CSS/JS (ví dụ: `style(product): responsive product grid`)
- `refactor`: Tái cấu trúc code (ví dụ: `refactor(auth): simplify role verification logic`)
- `chore`: Cấu hình build, package, gitignore (ví dụ: `chore(nuget): add Rotativa package`)

---

## 8. Yêu Cầu Môi Trường & Cài Đặt

### 8.1. Yêu Cầu Tiên Quyết (Prerequisites)
- **Hệ điều hành:** Windows 10 / 11
- **IDE:** Visual Studio 2019 hoặc Visual Studio 2022 (khuyến nghị bản Community)
  - Workload cần cài: **ASP.NET and web development**, **.NET Framework 4.8 targeting pack**.
- **Cơ sở dữ liệu:** PostgreSQL 14 / 15 / 16 & công cụ quản trị **pgAdmin 4** (hoặc DBeaver / Datagrip / `psql`).

### 8.2. Các Bước Cài Đặt & Chạy Ứng Dụng

1. **Clone mã nguồn về máy:**
   ```bash
   git clone https://github.com/DuyKhoi282/Web-Ecommerce.git
   cd Web-Ecommerce
   ```

2. **Mở dự án trong Visual Studio:**
   - Mở file Solution `WebEcommerce.sln` bằng Visual Studio.

3. **Khôi phục thư viện NuGet (Restore NuGet Packages):**
   - Click chuột phải vào Solution &rarr; Chọn **Restore NuGet Packages**.
   - Dự án tích hợp sẵn: `Npgsql` (phiên bản hỗ trợ .NET 4.8) và `EntityFramework6.Npgsql`.

4. **Khởi tạo Cơ sở dữ liệu PostgreSQL (Phương pháp Database First):**
   - Mở **pgAdmin 4**, tạo một Database mới có tên: `webecommerce_db`.
   - Nhấp chuột phải vào `webecommerce_db` &rarr; Chọn **Query Tool**.
   - Mở file kịch bản SQL tại `Database/Scripts/01_Init_PostgreSQL_Database.sql`.
   - Nhấn **F5 (Execute)** để khởi tạo toàn bộ 12 bảng dữ liệu và nạp dữ liệu mẫu ban đầu (Roles, Categories, Products, Vouchers).

5. **Cấu hình chuỗi kết nối Npgsql trong `Web.config`:**
   - Mở file `Web.config`, cập nhật thẻ `connectionStrings` với thông tin đăng nhập PostgreSQL trên máy của bạn:
     ```xml
     <connectionStrings>
       <add name="DefaultConnection" 
            connectionString="Server=localhost;Port=5432;Database=webecommerce_db;User Id=postgres;Password=your_password;" 
            providerName="Npgsql" />
     </connectionStrings>
     ```
   - Cấu hình provider Entity Framework 6 cho Npgsql trong `Web.config`:
     ```xml
     <entityFramework>
       <providers>
         <provider invariantName="Npgsql" type="Npgsql.NpgsqlServices, EntityFramework6.Npgsql" />
       </providers>
       <defaultConnectionFactory type="Npgsql.NpgsqlConnectionFactory, EntityFramework6.Npgsql" />
     </entityFramework>

     <system.data>
       <DbProviderFactories>
         <remove invariant="Npgsql" />
         <add name="Npgsql Provider" invariant="Npgsql" description=".NET Framework Data Provider for PostgreSQL" type="Npgsql.NpgsqlFactory, Npgsql" />
       </DbProviderFactories>
     </system.data>
     ```

6. **Đồng bộ Entity Data Model (EDMX / Database First):**
   - Mở file `WebEcommerceModel.edmx` trong thư mục `Models/`.
   - Nhấp chuột phải vào vùng trống &rarr; Chọn **Update Model from Database...** &rarr; Đồng bộ cấu trúc bảng từ PostgreSQL.

7. **Khởi chạy ứng dụng:**
   - Nhấn **F5** hoặc nút **IIS Express (Google Chrome / Edge)** để bắt đầu chạy ứng dụng.
   - Tài khoản mẫu mặc định (sau khi Seed Database):
     - **Admin:** `admin@ecommerce.com` / Mật khẩu: `Admin@123456`
     - **Store Manager:** `manager@ecommerce.com` / Mật khẩu: `Manager@123456`
     - **Customer:** `customer1@gmail.com` / Mật khẩu: `Customer@123456`

---

## 9. Tài Liệu Dự Án & Liên Kết

- 📘 **Kế hoạch Dự án Chi tiết (Google Doc):** [Xem tại đây](https://docs.google.com/document/d/19nresKEal1v_3HIkgCVnojSfoyZ2L_QFqHWWGKiCBzk/edit)
- 📊 **Bảng Theo dõi Tiến độ & Lỗi (Google Sheet):** [Xem tại đây](https://docs.google.com/spreadsheets/d/1hFEw0uHRQyusHnVuXd2TZvta6G9NYp33tC06bYCQFrU/edit)
- 🐙 **Kho lưu trữ GitHub:** [https://github.com/DuyKhoi282/Web-Ecommerce.git](https://github.com/DuyKhoi282/Web-Ecommerce.git)
- 📜 **Hướng dẫn Đóng góp & Quy tắc Git:** [CONTRIBUTING.md](CONTRIBUTING.md)

---
*Đồ án Môn học Phát triển Ứng dụng Web (ITE1265E) &bull; Nhóm 4 Thành viên &bull; Năm học 2026*
