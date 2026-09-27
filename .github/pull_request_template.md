## 📌 Tiêu Đề Pull Request
<!-- Định dạng mẫu: [TV1|TV2|TV3|TV4] Tên tính năng ngắn gọn -->
<!-- Ví dụ: [TV3] Hoàn thiện giỏ hàng Ajax và trừ tồn kho khi Checkout -->

---

## 📝 1. Tóm Tắt Thay Đổi
<!-- Mô tả ngắn gọn những gì bạn đã thực hiện trong PR này -->
- [ ] Chức năng 1: ...
- [ ] Chức năng 2: ...
- [ ] Xử lý ngoại lệ / Cập nhật CSDL: ...

---

## 🎯 2. Loại Thay Đổi (Type of Change)
- [ ] 🚀 **Tính năng mới** (`feat`)
- [ ] 🐛 **Sửa lỗi** (`fix`)
- [ ] 📄 **Cập nhật tài liệu / Hướng dẫn** (`docs`)
- [ ] 🎨 **Cải thiện giao diện / CSS** (`style`)
- [ ] ♻️ **Tái cấu trúc mã nguồn** (`refactor`)
- [ ] ⚡ **Tối ưu hiệu năng truy vấn** (`perf`)
- [ ] 🔧 **Cấu hình / Thư viện NuGet** (`chore`)

---

## 📸 3. Hình Ảnh Minh Chứng Chạy Thử (Screenshots / Demo)
<!-- Đính kèm ảnh chụp màn hình hoặc GIF chứng minh tính năng đã chạy đúng trên máy local -->

---

## 🧪 4. Danh Sách Kiểm Tra Chất Lượng (Quality Checklist)
- [ ] **Build:** Solution build thành công (0 Errors, 0 Warnings nghiêm trọng).
- [ ] **Try-Catch:** Mọi Action Method có CSDL/File/Thanh toán đều đã bọc trong khối `try-catch`.
- [ ] **Validation:** Đã kiểm tra `ModelState.IsValid` và có thông báo lỗi cho người dùng.
- [ ] **Bảo mật:** Toàn bộ form POST đều có `@Html.AntiForgeryToken()` và `[ValidateAntiForgeryToken]`.
- [ ] **Git Clean:** Đã kiểm tra `git status`, không commit nhầm file rác, file `.user`, `.suo`, thư mục `bin/`, `obj/`.
- [ ] **Database Sync:** (Nếu có thay đổi CSDL) Đã tạo Migration C# (Code First) HOẶC commit script SQL tương ứng trong `Database/Scripts/` (Database First).

---

## 👥 5. Phân Công & Reviewer
- **Tác giả PR:** Thành viên ...
- **Người phụ trách Review:** @...
