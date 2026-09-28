# 🧭 Quy Chuẩn Git & Hướng Dẫn Đóng Góp Mã Nguồn (Git & Coding Guidelines)

Tài liệu này quy định toàn bộ nguyên tắc phối hợp làm việc qua Git, chuẩn commit, quy trình kiểm thử và chất lượng mã nguồn áp dụng cho toàn bộ 4 thành viên trong dự án **Web-Ecommerce** (Học phần *ITE1265E – Phát triển Ứng dụng Web*).

---

## 📌 Mục Lục
1. [Nguyên Tắc Bất Biến (Golden Rules)](#1-nguyên-tắc-bất-biến-golden-rules)
2. [Chiến Lược Phân Nhánh (Branching Strategy)](#2-chiến-lược-phân-nhánh-branching-strategy)
3. [Quy Chuẩn Commit (Conventional Commits)](#3-quy-chuẩn-commit-conventional-commits)
4. [Quy Trình Làm Việc Hàng Ngày (Daily Workflow)](#4-quy-trình-làm-việc-hàng-ngày-daily-workflow)
5. [Quy Trình Pull Request (PR) & Code Review](#5-quy-trình-pull-request-pr--code-review)
6. [Kỹ Thuật Xử Lý Xung Đột (Merge Conflicts)](#6-kỹ-thuật-xử-lý-xung-đột-merge-conflicts)
7. [Tiêu Chuẩn Lập Trình & Chất Lượng Mã Nguồn (Clean Code)](#7-tiêu-chuẩn-lập-trình--chất-lượng-mã-nguồn-clean-code)

---

## 1. Nguyên Tắc Bất Biến (Golden Rules)

1. **Tuyệt đối KHÔNG commit trực tiếp lên nhánh `main` và `develop`.** Mọi thay đổi đều phải thông qua nhánh tính năng (`feature/*`) và nộp Pull Request (PR).
2. **Tuyệt đối KHÔNG dùng `git push --force`** lên các nhánh chia sẻ chung (`main`, `develop`).
3. **Luôn chạy `git status` và `git diff` trước khi commit** để kiểm tra không bỏ sót file và không commit nhầm file rác/file nhạy cảm (như mật khẩu, connection string cá nhân, file `.suo`, `.user`, thư mục `bin/`, `obj/`).
4. **Code trước khi nộp PR bắt buộc phải Build thành công (0 Errors, 0 Warnings nghiêm trọng)** trên máy cá nhân và chạy kiểm thử không phát sinh ngoại lệ đột ngột.
5. **Tuân thủ nguyên tắc phòng vệ biên (Boundary Defense):** 100% các Action Method tương tác CSDL, File, Thanh toán phải bọc trong khối `try-catch`.

---

## 2. Chiến Lược Phân Nhánh (Branching Strategy)

Hệ thống áp dụng mô hình phân nhánh rút gọn phù hợp với nhóm 4 người và chu kỳ tích hợp tuần:

```
main (Production/Nghiệm thu)
  ▲
  │ (Merge sau khi kết thúc Sprint tuần & kiểm thử đạt DoD)
develop (Tích hợp liên tục hàng tuần)
  ▲
  ├── feature/tv1-auth-admin (Thành viên 1)
  ├── feature/tv2-product-category (Thành viên 2)
  ├── feature/tv3-cart-checkout (Thành viên 3)
  ├── feature/tv4-review-export (Thành viên 4)
  └── bugfix/order-stock-error
```

### Chi Tiết Từng Nhánh:
- **`main`**: Nhánh ổn định cao nhất, chứa mã nguồn hoàn thiện của các phiên nghiệm thu đồ án. Chỉ **Team Lead** được phép merge vào `main`.
- **`develop`**: Nhánh tích hợp chính của nhóm. Các thành viên đồng bộ code và merge các nhánh tính năng vào đây.
- **`feature/<tv>-<tên-chức-năng>`**: Nhánh tính năng cá nhân. Mỗi thành viên khi làm một chức năng mới đều tạo nhánh từ `develop`.
  - Quy tắc đặt tên: `feature/tv<số-thứ-tự>-<tên-ngắn-gọn>`
  - *Ví dụ:* `feature/tv2-category-crud`, `feature/tv3-order-processing`, `feature/tv4-invoice-pdf`.
- **`bugfix/<mô-tả-lỗi>`**: Nhánh sửa lỗi phát hiện trong quá trình tích hợp trên `develop`.
  - *Ví dụ:* `bugfix/fix-cart-empty-crash`, `bugfix/fix-login-redirect`.
- **`hotfix/<mô-tả-lỗi-nghiêm-trọng>`**: Dành cho lỗi khẩn cấp phát hiện trên `main` cần sửa tức thời.

---

## 3. Quy Chuẩn Commit (Conventional Commits)

Mỗi commit phải giải quyết một vấn đề cụ thể, không dồn quá nhiều việc không liên quan vào một commit.

### Cú pháp:
```
<type>(<scope>): <mô tả ngắn gọn bằng tiếng Việt hoặc tiếng Anh>
```

### Các tiền tố (`type`) quy định:
| Tiền tố | Mục đích | Ví dụ |
| :--- | :--- | :--- |
| `feat` | Thêm chức năng/tính năng mới | `feat(cart): implement ajax add to cart` |
| `fix` | Sửa chữa lỗi (bug) | `fix(checkout): fix null reference when cart is empty` |
| `docs` | Viết hoặc cập nhật tài liệu | `docs(readme): add PostgreSQL database setup guide` |
| `style` | Sửa giao diện, CSS, HTML, căn chỉnh lề (không đổi logic C#) | `style(product-detail): improve image gallery layout` |
| `refactor` | Tối ưu, cơ cấu lại code (không thêm tính năng, không sửa bug) | `refactor(order): extract stock deduction to private helper` |
| `perf` | Cải thiện hiệu năng truy vấn, tải trang | `perf(search): add index and eager loading for category query` |
| `test` | Thêm hoặc sửa kịch bản kiểm thử | `test(order): verify coupon discount calculation` |
| `chore` | Cập nhật cấu hình, NuGet package, file build, .gitignore | `chore(nuget): install Npgsql and EntityFramework6.Npgsql` |

### Ví dụ Commit Hợp Chuẩn Cho 4 Thành Viên:
- **Thành viên 1 (Team Lead - Auth, User Mgmt, Authz & Administration):**
  - `feat(arch): setup ASP.NET MVC 5, Npgsql provider and EF 6 Database First`
  - `feat(auth): implement register, login with cookie authentication and logout`
  - `feat(auth): add change password and forgot/reset password flows`
  - `feat(profile): view and update user profile, avatar upload, address and phone`
  - `feat(authz): configure [Authorize] filters and custom 403 Forbidden error page`
  - `feat(admin): build admin panel layout and management navigation menu`
  - `feat(user-mgmt): add paginated user list, search by keyword and view details`
  - `feat(account-mgmt): implement account lock/unlock and activate/deactivate`
  - `feat(role-mgmt): manage 3-tier roles and assign/change user roles`
  - `feat(dashboard): calculate total revenue, orders, users, top selling and low stock`
  - `feat(analytics): render monthly revenue bar chart and order status pie chart with Chart.js`
  - `security(csrf): apply ValidateAntiForgeryToken across all POST actions`
  - `chore(merge): review PRs, resolve branch conflicts and integrate develop to main`
- **Thành viên 2:**
  - `feat(category): add Category CRUD views with Data Annotations`
  - `feat(product): implement multiple images upload and preview`
  - `feat(search): add multi-criteria filter by price range and rating`
- **Thành viên 3:**
  - `feat(cart): implement ajax cart counter and quantity update`
  - `feat(order): add transaction rollback on inventory deduction failure`
  - `feat(voucher): validate coupon code expiry and usage limit`
- **Thành viên 4:**
  - `feat(wishlist): add toggle wishlist button with ajax response`
  - `feat(review): restrict 5-star rating to delivered orders only`
  - `feat(export): generate printable PDF invoice using Rotativa`

---

## 4. Quy Trình Làm Việc Hàng Ngày (Daily Workflow)

Mỗi khi bắt đầu làm một tính năng mới, thành viên thực hiện theo 6 bước chuẩn sau:

### Bước 1: Đồng bộ mã nguồn mới nhất từ nhánh `develop`
```bash
git checkout develop
git pull origin develop
```

### Bước 2: Tạo nhánh tính năng mới từ `develop`
```bash
git checkout -b feature/tv2-product-crud
```

### Bước 3: Lập trình, kiểm thử và Commit đều đặn
- Chỉ thêm những file có liên quan (`git add <tên-file>` hoặc `git add Controllers/ Models/ Views/`):
```bash
git status
git add Models/Product.cs Controllers/ProductController.cs Views/Product/
git commit -m "feat(product): create product model and crud controller"
```

### Bước 4: Cập nhật thay đổi từ `develop` trước khi đẩy lên remote
Trước khi đẩy code, luôn kéo thay đổi mới nhất của nhóm về nhánh của mình để xử lý sớm xung đột:
```bash
git fetch origin
git merge origin/develop
```
*(Nếu có xung đột, xem mục 6 để xử lý; sau đó build lại solution và kiểm tra)*

### Bước 5: Đẩy nhánh tính năng lên GitHub
```bash
git push -u origin feature/tv2-product-crud
```

### Bước 6: Tạo Pull Request (PR) trên giao diện GitHub
- Truy cập repository: [https://github.com/DuyKhoi282/Web-Ecommerce](https://github.com/DuyKhoi282/Web-Ecommerce)
- Bấm nút **Compare & pull request**.
- Điền đầy đủ thông tin theo mẫu PR Template.

---

## 5. Quy Trình Pull Request (PR) & Code Review

1. **Tiêu đề PR:** Phải theo đúng chuẩn: `[TV<Số>] <Tên module/tính năng>`
   - *Ví dụ:* `[TV3] Hoàn thiện module Giỏ hàng Ajax và Đặt hàng COD`
2. **Nội dung PR:**
   - Mô tả tóm tắt tính năng vừa phát triển.
   - Hình ảnh chụp màn hình (Screenshot) kết quả giao diện hoặc minh chứng chạy thử.
   - Danh sách kiểm thử: Những ca test đã kiểm tra thành công.
3. **Quy tắc Review & Duyệt PR:**
   - Tối thiểu **1 thành viên khác** hoặc **Team Lead** xem qua mã nguồn (Code Review) và bấm **Approve**.
   - Người review kiểm tra:
     - Code có bọc `try-catch` đúng chuẩn không?
     - Có kiểm tra `ModelState.IsValid` không?
     - Có form POST nào quên `@Html.AntiForgeryToken()` không?
     - Không có file rác bị commit nhầm.
   - Sau khi duyệt, sử dụng chế độ **Squash and merge** hoặc **Create a merge commit** vào nhánh `develop`.

---

## 6. Kỹ Thuật Xử Lý Xung Đột (Merge Conflicts)

Khi có xung đột trong quá trình merge:
1. Mở Visual Studio hoặc Visual Studio Code.
2. Tìm các file có đánh dấu xung đột:
   ```csharp
   <<<<<<< HEAD (Mã nguồn hiện tại của bạn)
   public ActionResult Checkout() { ... }
   =======
   public ActionResult Checkout(CheckoutViewModel model) { ... }
   >>>>>>> origin/develop (Mã nguồn từ nhánh develop)
   ```
3. Thảo luận trực tiếp với thành viên liên quan để giữ lại đoạn code đúng nhất.
4. Xóa các thẻ đánh dấu (`<<<<<<<`, `=======`, `>>>>>>>`).
5. Build lại toàn bộ Solution trong Visual Studio để bảo đảm **Build Succeeded**.
6. Commit hoàn thành việc giải quyết xung đột:
   ```bash
   git add .
   git commit -m "chore: resolve merge conflicts with develop"
   git push origin feature/<tên-nhánh>
   ```

---

## 7. Tiêu Chuẩn Lập Trình & Chất Lượng Mã Nguồn (Clean Code)

### 7.1. Quy ước đặt tên trong C# (C# Naming Conventions)
- **PascalCase:** Tên Class, Tên Interface (bắt đầu bằng `I`), Tên Enum, Tên Phương thức, Tên Thuộc tính (Property):
  - *Ví dụ:* `ProductController`, `IOrderService`, `PaymentStatus`, `CalculateTotal()`, `UnitPrice`.
- **camelCase:** Tên biến cục bộ (local variable), tham số phương thức (parameter):
  - *Ví dụ:* `productId`, `orderTotal`, `discountAmount`.
- **_camelCase:** Biến trường riêng tư (private field):
  - *Ví dụ:* `private readonly ApplicationDbContext _db;`

### 7.2. Quy tắc Xử lý Ngoại lệ Bắt buộc (Boundary Defense)
Mọi Action Method thao tác dữ liệu phải có khung bảo vệ sau:
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult Create(ProductViewModel model)
{
    if (!ModelState.IsValid)
    {
        ViewBag.CategoryId = new SelectList(_db.Categories, "Id", "Name", model.CategoryId);
        return View(model);
    }

    try
    {
        // Thực hiện logic thêm mới sản phẩm
        // ...
        _db.SaveChanges();
        TempData["SuccessMessage"] = "Thêm mới sản phẩm thành công!";
        return RedirectToAction("Index");
    }
    catch (DbEntityValidationException ex)
    {
        // Bắt lỗi ràng buộc dữ liệu Entity Framework
        var errorMessages = ex.EntityValidationErrors
            .SelectMany(x => x.ValidationErrors)
            .Select(x => x.ErrorMessage);
        var fullError = string.Join("; ", errorMessages);
        ModelState.AddModelError("", "Lỗi kiểm tra dữ liệu: " + fullError);
    }
    catch (Exception ex)
    {
        // Ghi log chi tiết ra Output/Trace
        System.Diagnostics.Trace.TraceError("Lỗi Create Product: " + ex.ToString());
        ModelState.AddModelError("", "Đã xảy ra sự cố khi lưu dữ liệu. Vui lòng thử lại!");
    }

    ViewBag.CategoryId = new SelectList(_db.Categories, "Id", "Name", model.CategoryId);
    return View(model);
}
```

### 7.3. Quy Chuẩn Quản Lý Cơ Sở Dữ Liệu (Linh hoạt Code First hoặc Database First)
Nhóm không bắt buộc duy nhất một phương pháp, mà có thể linh hoạt chọn phương án phù hợp:

- **Trường hợp nhóm sử dụng Code First:**
  1. Điều chỉnh file C# Model trong thư mục `Models/`.
  2. Mở Package Manager Console, chạy lệnh:
     ```powershell
     Add-Migration <TênMôTảThayĐổi>
     Update-Database
     ```
  3. Commit file Migration C# phát sinh trong thư mục `Migrations/` lên Git để các thành viên khác kéo về và chạy `Update-Database`.

- **Trường hợp nhóm sử dụng Database First / SQL Script:**
  1. Mọi câu lệnh tạo/thay đổi bảng (DDL) phải được lưu thành file script `.sql` trong thư mục `Database/Scripts/` (đặt tên rõ ràng, ví dụ: `01_Init_Database.sql`, `02_Add_Voucher_Table.sql`).
  2. Tuyệt đối không tự ý chỉnh sửa ngầm trong SSMS mà không commit file script SQL lên Git.
  3. Sau khi chạy script cập nhật CSDL trên máy cá nhân:
     - Mở file `.edmx` trong thư mục `Models/`.
     - Click chuột phải vào màn hình thiết kế &rarr; Chọn **Update Model from Database...**
     - Chọn các bảng/cột mới được thêm hoặc cập nhật &rarr; Bấm **Finish** và lưu file `.edmx`.
  4. Build lại Solution để chắc chắn các lớp Entity sinh tự động không phát sinh lỗi trước khi commit.

---
*Tuân thủ nghiêm túc các quy định trên sẽ giúp nhóm hoàn thành đồ án chất lượng cao, đúng tiến độ và không gặp rủi ro mất mát mã nguồn!*
