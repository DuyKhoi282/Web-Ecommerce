# 🛒 Hệ Thống Thương Mại Điện Tử Trực Tuyến (Online Shopping E-Commerce)

[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg?style=flat-square)]()
[![Platform](https://img.shields.io/badge/.NET_Framework-4.8-blue.svg?style=flat-square)]()
[![Framework](https://img.shields.io/badge/ASP.NET-MVC_5-purple.svg?style=flat-square)]()
[![ORM](https://img.shields.io/badge/Entity_Framework-6.4-orange.svg?style=flat-square)]()
[![Database](https://img.shields.io/badge/Database-PostgreSQL_17+-336791.svg?style=flat-square&logo=postgresql&logoColor=white)]()
[![Provider](https://img.shields.io/badge/Provider-Npgsql-004880.svg?style=flat-square)]()
[![Git](https://img.shields.io/badge/Git_Flow-Weekly_CI-brightgreen.svg?style=flat-square)]()

> **Đồ án Cuối kỳ:** Học phần Phát triển Ứng dụng Web (`ITE1265E`)  
> **Đề tài:** Topic 1 – Online Shopping Website  
> **Nhóm thực hiện:** Nhóm 5 – Lớp A03E (GVHD: TS. Hoàng Văn Hiếu)  
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

Hệ thống Website Bán Hàng Trực Tuyến là một ứng dụng web thương mại điện tử đa tầng hoàn chỉnh, được xây dựng trên nền tảng **ASP.NET MVC 5** (.NET Framework 4.8) kết hợp hệ quản trị cơ sở dữ liệu quan hệ mã nguồn mở mạnh mẽ **PostgreSQL**, sử dụng thư viện **Npgsql** làm ADO.NET / EF6 Data Provider. Cấu trúc CSDL được tạo trước bằng script SQL, sau đó Entity Framework 6 ánh xạ vào các bảng có sẵn.

Mục tiêu cốt lõi của đồ án:
- Hiện thực hóa quy trình mua sắm khép kín: Khám phá sản phẩm &rarr; Tìm kiếm / Lọc đa tiêu chí &rarr; Giỏ hàng Ajax &rarr; Áp dụng Voucher / Flash Sale &rarr; Thanh toán (COD / Chuyển khoản QR) &rarr; Quản lý đơn hàng &rarr; Đánh giá phản hồi 5 sao có kiểm chứng.
- Phân quyền chặt chẽ 3 cấp (**Customer**, **Store Manager**, **Administrator**) với ASP.NET Identity 2.2 và `CustomAuthorizeAttribute`.
- Áp dụng nguyên tắc **Phòng vệ biên (Boundary Defense)** với 100% Action CSDL/File/Thanh toán được bọc `try-catch`, đảm bảo ứng dụng không bao giờ bị dừng đột ngột (Yellow Screen of Death).
- Quản lý và đồng bộ cơ sở dữ liệu đồng nhất giữa 4 thành viên bằng **script SQL đánh số** (`Database/Scripts/01_...sql`, `02_...sql`, ...): toàn bộ bảng, khóa chính, khóa ngoại, chỉ mục được tạo trên PostgreSQL; các class Model và `ApplicationDbContext` (EF6) được viết tay để ánh xạ vào bảng có sẵn, với `Database.SetInitializer<ApplicationDbContext>(null)` để EF không tự tạo hay sửa bảng.

---

## 2. Ngăn Xếp Công Nghệ (Tech Stack)

| Lớp (Layer) | Công Nghệ & Thư Viện | Mục Đích Sử Dụng |
| :--- | :--- | :--- |
| **Backend Framework** | ASP.NET MVC 5, .NET Framework 4.8 | Xây dựng kiến trúc Model-View-Controller chuẩn doanh nghiệp |
| **Cơ sở dữ liệu** | PostgreSQL (17.x / 16 / 15 / 14) | Hệ quản trị CSDL quan hệ ACID mạnh mẽ, tối ưu lưu trữ và chỉ mục |
| **Database Provider** | Npgsql 4.1, EntityFramework6.Npgsql 6.4 | Data Provider cung cấp kết nối ADO.NET và dịch vụ EF6 cho PostgreSQL |
| **ORM / Data Access** | Entity Framework 6.4 | Ánh xạ bảng PostgreSQL có sẵn sang class Model, truy vấn LINQ an toàn |
| **Bảo mật & Phân quyền** | ASP.NET Identity 2.2, OWIN 4.2 Cookie Authentication | Xác thực người dùng, mã hóa mật khẩu PBKDF2, phân quyền Role-based |
| **Frontend UI** | Razor View Engine, Tailwind CSS (CDN), Bootstrap 5 (trang Admin), Material Symbols | Giao diện hiện đại, responsive trên Mobile và Desktop |
| **Client Scripting** | JavaScript (Fetch API ở trang khách), jQuery 3.7 Ajax (trang Admin) | Cập nhật giỏ hàng, wishlist, voucher không tải lại trang |
| **Tiện ích mở rộng** | iTextSharp, MailKit, Chart.js | Xuất hóa đơn PDF, gửi email tự động, vẽ biểu đồ doanh thu Admin |

---

## 3. Kiến Trúc & Ma Trận Phân Bổ Nhân Sự

Hệ thống được thiết kế theo chuỗi giá trị và trật tự phụ thuộc kỹ thuật công bằng, chặt chẽ giữa 4 thành viên:

```
[Trần Quốc Duy Khôi: Team Lead]            [Nguyễn Trần Công Hậu]
  ├── Architecture & Foundation              ├── Category CRUD (Cha - Con)
  ├── Authentication (OTP Email, Lockout)    ├── Product CRUD & Pricing
  ├── User Profile Management                ├── Multiple Images (IsMain)
  ├── User Management (Lock/Unlock, Role)    ├── Search Autocomplete & Multi-Filter
  ├── Role-based Access Control (3 Roles)    └── Inventory Management & Low Stock (< 5)
  ├── Admin Layout & Dashboard KPIs                 │
  ├── Chart.js (6 Months & Order Status)            │
  └── Weekly Code Review & Integration Flow         ▼
         │                          [Nguyễn Lê Đàm Văn]
         │                            ├── Ajax Shopping Cart (Cookie per Account)
         │                            ├── Checkout (COD / VietQR)
         │                            ├── Order ACID Transaction & Inventory Deduction
         │                            ├── Order Lifecycle & Cancellation
         │                            └── Voucher & Flash Sale Countdown
         │                                          │
         └──────────────────────────────────────────┴────────► [Nguyễn Hoàng Trọng Phúc]
                                                                 ├── Verified Reviews (1-5★)
                                                                 ├── Wishlist (Ajax Toggle)
                                                                 ├── Export PDF Invoice (iTextSharp)
                                                                 └── MailKit Automated Notifications
```

### Chi Tiết Phân Công Trách Nhiệm:

| Thành Viên | Vai Trò & Phụ Trách | Controller / Deliverables Chính |
| :--- | :--- | :--- |
| **Trần Quốc Duy Khôi** *(Team Lead)* | **Kiến trúc, Xác thực, Phân quyền & Dashboard:**<br>• **Kiến trúc & Nền tảng:** Solution setup ASP.NET MVC 5, cấu hình EF6 + Npgsql cho PostgreSQL, Git Flow & tích hợp tuần hoàn.<br>• **Xác thực & Mật khẩu:** Register (xác nhận email bằng mã OTP), Login Cookie Auth + Remember Me, khóa tài khoản sau 5 lần sai, Logout, Forgot & Reset Password.<br>• **Hồ sơ cá nhân & Bảo mật:** Xem/sửa Profile, Upload Avatar (kiểm tra nội dung file ảnh), Đổi mật khẩu; phòng vệ Anti-CSRF, băm mật khẩu Identity PBKDF2.<br>• **Quản trị User & Tài khoản:** Danh sách User phân trang, tìm kiếm theo tên/email/SĐT, xem chi tiết và đơn hàng gần đây; Khóa/Mở khóa tài khoản; đổi vai trò trong transaction.<br>• **Phân quyền & Admin Layout:** Admin layout riêng (`_AdminLayout.cshtml`), Sidebar menu; phân quyền 3 vai trò bằng `CustomAuthorizeAttribute`, trang 403 Forbidden.<br>• **Dashboard Quản trị:** Thẻ KPI (doanh thu, đơn hàng, khách hàng, sản phẩm sắp hết hàng), top sản phẩm bán chạy, biểu đồ Chart.js doanh thu 6 tháng và tỷ lệ trạng thái đơn. | `AccountController`<br>`ManageController`<br>`AdminUserController`<br>`AdminDashboardController`<br>`CustomAuthorizeAttribute`<br>`Admin Layout & Security Views` |
| **Nguyễn Trần Công Hậu** | **Quản lý Hàng hóa, Kho & Tìm kiếm / Lọc:**<br>• **Danh mục Sản phẩm:** CRUD Category 2 cấp cha - con, bật/tắt hiển thị, chặn xóa danh mục còn sản phẩm.<br>• **Quản lý Hàng hóa:** CRUD Product (Giá gốc, giá khuyến mãi, tồn kho, trạng thái Còn hàng/Hết hàng/Ngừng kinh doanh).<br>• **Bộ sưu tập hình ảnh:** Tải lên và quản lý nhiều ảnh sản phẩm (Multiple Images Upload, chọn ảnh đại diện `IsMain`).<br>• **Tìm kiếm đa năng:** Tìm kiếm theo từ khóa có gợi ý tự động (Autocomplete), sắp xếp và phân trang.<br>• **Bộ lọc động đa tiêu chí:** Lọc sản phẩm theo danh mục, khoảng giá, số sao tối thiểu.<br>• **Quản lý Kho & Tồn kho:** Điều chỉnh số lượng tồn kho, cảnh báo sản phẩm sắp hết hàng (&lt; 5). | `CategoryController`<br>`ProductController`<br>`ShopController`<br>`SearchController`<br>`InventoryController`<br>`Product & Filter Views` |
| **Nguyễn Lê Đàm Văn** | **Giỏ hàng, Đặt hàng & Khuyến mãi:**<br>• **Giỏ hàng Ajax:** Thêm/tăng/giảm số lượng, xóa món, tính tổng tiền tức thời không tải lại trang; giỏ lưu trong cookie riêng cho từng tài khoản (bắt buộc đăng nhập).<br>• **Quy trình Thanh toán:** Trang Checkout, địa chỉ giao hàng, COD hoặc chuyển khoản VietQR (mã QR điền sẵn số tiền và mã đơn).<br>• **Xử lý Đơn hàng:** Tạo đơn hàng, kiểm tra và trừ tồn kho trong `DbContextTransaction`, ngăn đặt hàng khi hết kho.<br>• **Vòng đời Đơn hàng:** Cập nhật tiến trình (Pending &rarr; Confirmed &rarr; Processing &rarr; Shipped &rarr; Delivered/Cancelled), cập nhật trạng thái thanh toán, hủy đơn có lý do và hoàn kho, gửi email cho khách khi đơn được xác nhận, bị hủy hoặc giao thành công.<br>• **Mã ưu đãi (Voucher):** Quản lý và áp dụng mã giảm giá theo %, số tiền cố định, kiểm tra điều kiện tối thiểu qua Ajax (`ApplyPromoCode`).<br>• **Sự kiện Flash Sale:** Khung giờ vàng giảm giá kèm đồng hồ đếm ngược trên trang chủ (Countdown Timer). | `CartController`<br>`OrderController`<br>`AdminOrderController`<br>`CookieCartHelper`<br>`Order & Checkout Views` |
| **Nguyễn Hoàng Trọng Phúc** | **Trải nghiệm Khách hàng, Hóa đơn & Email:**<br>• **Đánh giá & Xếp hạng:** Đánh giá 1 - 5 sao và bình luận (chỉ với sản phẩm thuộc đơn đã `Delivered`), tính điểm trung bình.<br>• **Danh sách yêu thích:** Quản lý Wishlist (bấm tim trên thẻ sản phẩm bằng Ajax, lưu theo tài khoản khách).<br>• **Xuất hóa đơn PDF:** Tạo và tải hóa đơn PDF cho đơn hàng (iTextSharp).<br>• **Email thông báo tự động:** Gửi email HTML xác nhận và cập nhật trạng thái đơn qua MailKit / SMTP. | `ReviewController`<br>`WishlistController`<br>`ExportController` (PDF)<br>`EmailService` (MailKit)<br>`Review & Wishlist Views` |

---

## 4. Ma Trận Phân Quyền 3 Cấp (RBAC)

| Nghiệp vụ / Tính Năng Hệ Thống | Customer | Store Manager | Administrator |
| :--- | :---: | :---: | :---: |
| Xem sản phẩm, tìm kiếm, lọc, xem chi tiết (không cần đăng nhập) | ✅ | ✅ | ✅ |
| Xem và cập nhật Profile cá nhân, Upload Avatar, Đổi mật khẩu | ✅ | ✅ | ✅ |
| Giỏ hàng Ajax, Checkout đặt hàng, xem lịch sử mua cá nhân | ✅ | ❌ | ❌ |
| Tự hủy đơn hàng cá nhân (khi trạng thái `Pending` hoặc `Confirmed`) | ✅ | ❌ | ❌ |
| Đánh giá 1-5 sao (bắt buộc đơn hàng trạng thái `Delivered`) | ✅ | ❌ | ❌ |
| Quản lý CRUD Danh mục, Sản phẩm, Upload ảnh, Kho hàng | ❌ | ✅ | ✅ |
| Quản lý và cập nhật tiến trình đơn hàng | ❌ | ✅ | ✅ |
| Thiết lập Voucher khuyến mãi và khung giờ Flash Sale | ❌ | ✅ | ✅ |
| Xem Dashboard thống kê (doanh thu, đơn hàng, biểu đồ Chart.js) | ❌ | ✅ | ✅ |
| Quản trị User (Xem danh sách, tìm kiếm User, xem chi tiết Profile) | ❌ | ❌ | ✅ *(Độc quyền)* |
| Quản lý tài khoản (Khóa/Mở tài khoản Lock/Unlock) | ❌ | ❌ | ✅ *(Độc quyền)* |
| Phân quyền vai trò (Gán và Chuyển đổi Role: Customer/Manager/Admin) | ❌ | ❌ | ✅ *(Độc quyền)* |
| Kiểm duyệt và xóa bỏ bình luận / đánh giá vi phạm | ❌ | ❌ | ✅ *(Độc quyền)* |

---

## 5. Đặc Tả Nghiệp Vụ Cốt Lõi

### 5.1. Quy trình Mua hàng & Thanh toán (Payment Contract)
1. **COD (Cash On Delivery):**
   - Đặt hàng &rarr; `PaymentStatus = "Unpaid"` (Chưa thanh toán).
   - Khi Store Manager đổi trạng thái giao hàng thành `Delivered` &rarr; Hệ thống tự động chuyển `PaymentStatus = "Paid"` (Đã thanh toán) và lưu `PaymentDate`.
2. **Chuyển khoản Ngân hàng (VietQR):**
   - Trang chi tiết đơn hiển thị mã VietQR điền sẵn số tiền và nội dung chuyển khoản `DH [MÃ ĐƠN]`; đơn vẫn ở `PaymentStatus = "Unpaid"`.
   - Hệ thống không tự đối soát giao dịch: Store Manager kiểm tra tiền đã vào tài khoản rồi đánh dấu `PaymentStatus = "Paid"`.
   - Đơn chuyển khoản chưa `Paid` thì không được chuyển sang `Shipped` hoặc `Delivered`.

### 5.2. Quản lý Tồn kho & Giao dịch Toàn vẹn (ACID Transaction)
- Thao tác tạo đơn hàng và trừ tồn kho sản phẩm được bọc hoàn toàn trong `DbContextTransaction`:
```csharp
using (var transaction = db.Database.BeginTransaction())
{
    try
    {
        // 1. Tạo đơn hàng và chi tiết đơn hàng
        // 2. Kiểm tra và trừ số lượng tồn kho từng sản phẩm
        // 3. Tăng lượt dùng voucher (nếu có)
        db.SaveChanges();
        transaction.Commit();
        // 4. Chỉ xóa cookie giỏ hàng sau khi commit thành công
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
   - Bắt mọi lỗi ngoại lệ (`DbEntityValidationException` bắt riêng, sau đó `Exception`), ghi log bằng `Trace.TraceError` và hiển thị thông báo thân thiện qua `ModelState` hoặc `TempData["ErrorMessage"]`.
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
- **`develop`**: Nhánh tích hợp chung hàng tuần. Toàn bộ tính năng hoàn thiện được merge vào đây sau khi review. *(Trên GitHub nhánh này đang có tên `develope`; tạo Pull Request vào `develope`.)*
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
- `chore`: Cấu hình build, package, gitignore (ví dụ: `chore(nuget): add iTextSharp package`)

---

## 8. Yêu Cầu Môi Trường & Cài Đặt

### 8.1. Yêu Cầu Tiên Quyết (Prerequisites)
- **Hệ điều hành:** Windows 10 / 11
- **IDE:** Visual Studio 2022 bản mới (hỗ trợ file solution `.slnx`; khuyến nghị bản Community)
  - Workload cần cài: **ASP.NET and web development**, **.NET Framework 4.8 targeting pack**.
- **Cơ sở dữ liệu:** PostgreSQL 17.x (hoặc 16 / 15 / 14) & công cụ quản trị **pgAdmin 4** (hoặc DBeaver / Datagrip / `psql`).

### 8.2. Các Bước Cài Đặt & Chạy Ứng Dụng

1. **Clone mã nguồn về máy:**
   ```bash
   git clone https://github.com/DuyKhoi282/Web-Ecommerce.git
   cd Web-Ecommerce
   ```

2. **Mở dự án trong Visual Studio:**
   - Mở file Solution `WebEcommerce/WebEcommerce.slnx` bằng Visual Studio.

3. **Khôi phục thư viện NuGet (Restore NuGet Packages):**
   - Click chuột phải vào Solution &rarr; Chọn **Restore NuGet Packages**.
   - Dự án tích hợp sẵn: `Npgsql` (phiên bản hỗ trợ .NET 4.8) và `EntityFramework6.Npgsql`.

4. **Khởi tạo Cơ sở dữ liệu PostgreSQL:**
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

6. **Lưu ý khi đổi cấu trúc CSDL:**
   - Dự án không dùng file `.edmx`. Khi cần thêm/sửa bảng: viết script SQL mới theo số thứ tự tiếp theo trong `Database/Scripts/`, rồi cập nhật class Model tương ứng (và `DbSet` trong `ApplicationDbContext` nếu là bảng mới).

7. **Khởi chạy ứng dụng (F5) — Tự động Seed tài khoản mẫu:**
   - Nhấn **F5** hoặc nút **IIS Express**. Khi app khởi động, `Startup.cs` tự động tạo các tài khoản sau:

   | Email | Mật khẩu | Vai trò |
   | :--- | :--- | :--- |
   | `admin@thechillshop.vn` | `Admin@123456` | Administrator |
   | `manager@thechillshop.vn` | `Manager@123456` | StoreManager |
   | `manager2@thechillshop.vn` | `Manager@123456` | StoreManager |
   | `customer1@gmail.com` | `Customer@123456` | Customer |
   | `customer2@gmail.com` | `Customer@123456` | Customer |
   | `customer3@gmail.com` | `Customer@123456` | Customer |
   | `customer4@gmail.com` … `customer10@gmail.com` | `Customer@123456` | Customer |

8. **Bơm dữ liệu mẫu đầy đủ:**
   - Sau khi app đã chạy thành công, mở **pgAdmin** &rarr; chạy lần lượt các script trong `Database/Scripts/`:
     1. `02_Seed_Data.sql`: thêm **32 sản phẩm** (8 danh mục có cha-con), **50 ảnh placeholder**, **5 voucher**, **20 đơn hàng** (đủ 6 trạng thái), đánh giá và wishlist mẫu.
     2. `03_FlashSale_Tables.sql`: tạo bảng `FlashSales`, `FlashSaleItems` và đợt Flash Sale mẫu (dùng sản phẩm từ bước 1).
     3. `04_Update_Orders_Cancellation.sql`: chỉ cần với CSDL tạo từ bản `01` cũ (thiếu cột lý do hủy đơn); chạy lại cũng không sao.
     4. `05_Update_Authentic_Product_Images.sql`: thay ảnh placeholder bằng ảnh sản phẩm thật.
     5. `09_Seed_Demo_Data.sql`: đợt Flash Sale thứ 2 (sắp diễn ra), ảnh thứ 3 cho 7 sản phẩm, 1 sản phẩm hết hàng (Ghế DXRacer). Chạy lại không nhân bản dữ liệu.

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
*Đồ án Môn học Phát triển Ứng dụng Web (ITE1265E) &bull; Nhóm 5 (4 thành viên) &bull; Năm học 2026*
