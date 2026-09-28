# 🛒 Hệ Thống Thương Mại Điện Tử Trực Tuyến (Online Shopping E-Commerce)

[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg?style=flat-square)]()
[![Platform](https://img.shields.io/badge/.NET_Framework-4.8-blue.svg?style=flat-square)]()
[![Framework](https://img.shields.io/badge/ASP.NET-MVC_5-purple.svg?style=flat-square)]()
[![ORM](https://img.shields.io/badge/Entity_Framework-6_(Database_First)-orange.svg?style=flat-square)]()
[![Database](https://img.shields.io/badge/Database-PostgreSQL_17+-336791.svg?style=flat-square&logo=postgresql&logoColor=white)]()
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
| **Cơ sở dữ liệu** | PostgreSQL (17.x / 16 / 15 / 14) | Hệ quản trị CSDL quan hệ ACID mạnh mẽ, tối ưu lưu trữ và chỉ mục |
| **Database Provider** | Npgsql, EntityFramework6.Npgsql | Data Provider cung cấp kết nối ADO.NET và dịch vụ EF6 cho PostgreSQL |
| **ORM / Data Access** | Entity Framework 6 (Database First) | Ánh xạ CSDL sang đối tượng Model theo Database First, truy vấn LINQ an toàn |
| **Bảo mật & Phân quyền** | ASP.NET Identity 2.0, OWIN Cookie Authentication | Xác thực người dùng, mã hóa mật khẩu PBKDF2, phân quyền Role-based |
| **Frontend UI** | Bootstrap 5, Razor View Engine, FontAwesome | Giao diện hiện đại, responsive hoàn toàn trên Mobile và Desktop |
| **Client Scripting** | JavaScript, jQuery, Ajax | Cập nhật giỏ hàng mượt mà không tải lại trang (Single Page Feel) |
| **Tiện ích mở rộng** | Rotativa / iTextSharp, MailKit, Chart.js | Xuất hóa đơn PDF, gửi email tự động, vẽ biểu đồ doanh thu Admin |

---

## 3. Kiến Trúc & Ma Trận Phân Bổ Nhân Sự

Hệ thống được thiết kế theo chuỗi giá trị và trật tự phụ thuộc kỹ thuật công bằng, chặt chẽ giữa 4 thành viên:

```
[Thành viên 1: Team Lead]                  [Thành viên 2]
  ├── Architecture & Foundation              ├── Category CRUD (Cha - Con)
  ├── Authentication & Passwords             ├── Product CRUD & Pricing
  ├── User Profile Management                ├── Multiple Images (IsMain)
  ├── User & Account Management (Lock/Unlock)├── Search Autocomplete & Multi-Filter
  ├── Role-based Access Control (RBAC 3 Cấp) └── Inventory Management & Low Stock (< 5)
  ├── Admin Panel Layout & Navigation               │
  ├── Security Defense (Anti-CSRF, PBKDF2)          │
  └── Weekly Code Review & Integration Flow         ▼
         │                          [Thành viên 3]
         │                            ├── Ajax Shopping Cart
         │                            ├── Checkout (COD / QR Bank Transfer)
         │                            ├── Order ACID Transaction & Inventory Deduction
         │                            ├── Order Lifecycle Management
         │                            └── Voucher & Flash Sale Realtime
         │                                          │
         └──────────────────────────────────────────┴────────► [Thành viên 4]
                                                                 ├── Admin Dashboard & Executive KPIs
                                                                 ├── Chart.js Analytics (12 Months & Status)
                                                                 ├── Export PDF Invoice (Rotativa)
                                                                 ├── MailKit Automated Notifications
                                                                 └── Verified Reviews (1-5★) & Wishlist
```

### Chi Tiết Phân Công Trách Nhiệm:

| Thành Viên | Vai Trò & Phụ Trách | Controller / Deliverables Chính |
| :--- | :--- | :--- |
| **Thành viên 1** *(Team Lead)* | **Kiến trúc, Xác thực & Phân quyền Hệ thống:**<br>• **Kiến trúc & Nền tảng:** Solution setup ASP.NET MVC 5, cấu hình EF 6 Database First (Npgsql / PostgreSQL 17), Git Flow & tích hợp tuần hoàn.<br>• **Xác thực & Mật khẩu:** Register, Login Cookie Auth, Logout, Change Password, Forgot & Reset Password.<br>• **Hồ sơ cá nhân & Bảo mật:** Xem/sửa Profile, Upload Avatar, Địa chỉ, SĐT; phòng vệ Anti-CSRF, băm mật khẩu Identity PBKDF2.<br>• **Quản trị User & Tài khoản:** Xem danh sách User phân trang, tìm kiếm đa tiêu chí, xem chi tiết; Khóa/Mở khóa tài khoản (Lock/Unlock).<br>• **Phân quyền & Admin Layout:** Thiết kế Admin layout riêng biệt (`_AdminLayout.cshtml`), Sidebar menu; phân quyền 3 vai trò (Customer/StoreManager/Administrator), trang 403 Forbidden. | `AccountController`<br>`ManageController`<br>`AdminUsersController`<br>`Identity Models & Services`<br>`Admin Layout & Security Views` |
| **Thành viên 2** | **Quản lý Hàng hóa, Kho & Tìm kiếm / Lọc:**<br>• **Danh mục Sản phẩm:** CRUD Category (Thêm/Sửa/Ẩn/Xóa, cấu trúc danh mục cha - con).<br>• **Quản lý Hàng hóa:** CRUD Product (Giá gốc, giá khuyến mãi, trạng thái Còn hàng/Hết hàng/Ngừng bán).<br>• **Bộ sưu tập hình ảnh:** Tải lên và quản lý nhiều ảnh sản phẩm (Multiple Images Upload, chọn ảnh đại diện `IsMain`).<br>• **Tìm kiếm đa năng:** Tìm kiếm theo từ khóa (Keyword autocomplete / full-text search) có phân trang.<br>• **Bộ lọc động đa tiêu chí:** Lọc sản phẩm theo danh mục, khoảng giá, xếp hạng sao trung bình.<br>• **Quản lý Kho & Tồn kho:** Theo dõi nhập/xuất số lượng tồn kho, phát cảnh báo sản phẩm sắp hết hàng (&lt; 5). | `CategoryController`<br>`ProductController`<br>`SearchController`<br>`InventoryController`<br>`Product & Filter Views` |
| **Thành viên 3** | **Giỏ hàng, Đặt hàng & Khuyến mãi:**<br>• **Giỏ hàng Ajax:** Thêm/sửa số lượng, xóa món, tính tổng tiền tức thời không tải lại trang (Ajax Cart).<br>• **Quy trình Thanh toán:** Trang Checkout, địa chỉ giao hàng, COD hoặc chuyển khoản ngân hàng (QR Demo).<br>• **Xử lý Đơn hàng:** Tạo đơn hàng, kiểm tra và trừ tồn kho (ACID Transaction), ngăn đặt hàng khi hết kho.<br>• **Vòng đời Đơn hàng:** Cập nhật tiến trình (Pending &rarr; Confirmed &rarr; Processing &rarr; Shipped &rarr; Delivered/Cancelled).<br>• **Mã ưu đãi (Voucher):** Quản lý và áp dụng mã giảm giá theo %, số tiền cố định, kiểm tra điều kiện tối thiểu.<br>• **Sự kiện Flash Sale:** Khung giờ vàng giảm giá sốc kèm đồng hồ đếm ngược thời gian thực (Countdown Timer). | `CartController`<br>`CheckoutController`<br>`OrderController`<br>`PromotionController`<br>`Order & Checkout Views` |
| **Thành viên 4** | **Dashboard Thống kê, Báo cáo & Trải nghiệm Khách hàng:**<br>• **Dashboard Quản trị:** Báo cáo tổng doanh thu, tổng số đơn hàng, số khách hàng mới, top sản phẩm bán chạy.<br>• **Phân tích Biểu đồ Chart.js:** Trực quan hóa doanh thu 12 tháng (Bar Chart), tỷ lệ trạng thái đơn hàng (Pie/Doughnut Chart).<br>• **Xuất hóa đơn PDF:** Tự động tạo và xuất hóa đơn điện tử định dạng PDF chuyên nghiệp (iTextSharp / Rotativa).<br>• **Email thông báo tự động:** Dịch vụ gửi Email tự động xác nhận đơn và cập nhật trạng thái đơn (MailKit / SMTP).<br>• **Đánh giá & Xếp hạng:** Đánh giá 1 - 5 sao và bình luận (ràng buộc chỉ tài khoản đã nhận hàng Delivered), tính điểm trung bình.<br>• **Danh sách yêu thích:** Quản lý Wishlist (Thêm/Xóa sản phẩm quan tâm, lưu trữ theo tài khoản khách). | `DashboardController`<br>`AnalyticsController`<br>`ExportController` (PDF)<br>`EmailService` (MailKit)<br>`ReviewController`<br>`WishlistController`<br>`Dashboard & Review Views` |

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
- **Cơ sở dữ liệu:** PostgreSQL 17.x (hoặc 16 / 15 / 14) & công cụ quản trị **pgAdmin 4** (hoặc DBeaver / Datagrip / `psql`).

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

7. **Khởi chạy ứng dụng (F5) — Tự động Seed tài khoản mẫu:**
   - Nhấn **F5** hoặc nút **IIS Express**. Khi app khởi động, `Startup.cs` tự động tạo các tài khoản sau:

   | Email | Mật khẩu | Vai trò |
   | :--- | :--- | :--- |
   | `admin@thechillshop.vn` | `Admin@123456` | Administrator |
   | `manager@thechillshop.vn` | `Manager@123456` | StoreManager |
   | `customer1@gmail.com` | `Customer@123456` | Customer |
   | `customer2@gmail.com` | `Customer@123456` | Customer |
   | `customer3@gmail.com` | `Customer@123456` | Customer |

8. **Bơm dữ liệu mẫu đầy đủ:**
   - Sau khi app đã chạy thành công, mở **pgAdmin** &rarr; chạy `Database/Scripts/02_Seed_Data.sql`.
   - Script tự động thêm **32 sản phẩm** (8 danh mục có cha-con), **50 ảnh placeholder**, **5 voucher**, **20 đơn hàng** (đủ 6 trạng thái), đánh giá và wishlist mẫu.

### 8.3. Reset Dữ Liệu (Môi Trường Development)

Khi cần làm sạch DB để test lại từ đầu, chạy `Database/Scripts/99_Reset_Data.sql`:
- **Mode A** *(mặc định)*: Xoá data nghiệp vụ, giữ nguyên tài khoản &rarr; Chạy lại `02_Seed_Data.sql`.
- **Mode B** *(bỏ comment phần cuối file)*: Xoá toàn bộ kể cả Users &rarr; Restart app rồi chạy lại `02_Seed_Data.sql`.

---



## 9. Tài Liệu Dự Án & Liên Kết

- 📘 **Kế hoạch Dự án Chi tiết (Google Doc):** [Xem tại đây](https://docs.google.com/document/d/19nresKEal1v_3HIkgCVnojSfoyZ2L_QFqHWWGKiCBzk/edit)
- 📊 **Bảng Theo dõi Tiến độ & Lỗi (Google Sheet):** [Xem tại đây](https://docs.google.com/spreadsheets/d/1hFEw0uHRQyusHnVuXd2TZvta6G9NYp33tC06bYCQFrU/edit)
- 🐙 **Kho lưu trữ GitHub:** [https://github.com/DuyKhoi282/Web-Ecommerce.git](https://github.com/DuyKhoi282/Web-Ecommerce.git)
- 📜 **Hướng dẫn Đóng góp & Quy tắc Git:** [CONTRIBUTING.md](CONTRIBUTING.md)

---
*Đồ án Môn học Phát triển Ứng dụng Web (ITE1265E) &bull; Nhóm 4 Thành viên &bull; Năm học 2026*
