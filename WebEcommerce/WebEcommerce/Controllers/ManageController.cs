using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public ManageController() { }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get { return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>(); }
            private set { _signInManager = value; }
        }

        public ApplicationUserManager UserManager
        {
            get { return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
            private set { _userManager = value; }
        }

        private IAuthenticationManager AuthenticationManager
        {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        // GET: /Manage (Mặc định chuyển hướng về trang Hồ sơ cá nhân)
        public ActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        // ═══════════════════════════════════════════════════════
        //  T05 — ĐỔI MẬT KHẨU
        // ═══════════════════════════════════════════════════════

        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var userId = User.Identity.GetUserId();

                // Nghiệp vụ: ChangePasswordAsync sẽ xác minh OldPassword trước
                // Nếu OldPassword sai → trả về lỗi identity "Incorrect password"
                var result = await UserManager.ChangePasswordAsync(userId, model.OldPassword, model.NewPassword);

                if (result.Succeeded)
                {
                    // Nghiệp vụ: Bắt buộc re-sign in sau đổi mật khẩu để cập nhật Security Stamp
                    // → Invalidate tất cả session cũ / remember-me cookies cũ
                    var user = await UserManager.FindByIdAsync(userId);
                    if (user != null)
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    TempData["SuccessMessage"] = "Mật khẩu đã được thay đổi thành công.";
                    return RedirectToAction("ChangePassword");
                }

                // Dịch lỗi Identity sang tiếng Việt
                foreach (var error in result.Errors)
                {
                    var vietnameseError = TranslateIdentityError(error);
                    ModelState.AddModelError("", vietnameseError);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[ChangePassword Error] {ex.Message}");
                ModelState.AddModelError("", "Đã xảy ra lỗi. Vui lòng thử lại.");
            }

            return View(model);
        }

        // ═══════════════════════════════════════════════════════
        //  T06 — HỒ SƠ CÁ NHÂN (Profile)
        // ═══════════════════════════════════════════════════════

        // GET: /Manage/Profile
        public new async Task<ActionResult> Profile()
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var user = await UserManager.FindByIdAsync(userId);
                if (user == null)
                    return RedirectToAction("Login", "Account");

                var model = new ProfileViewModel
                {
                    Email       = user.Email,
                    FullName    = user.FullName,
                    Address     = user.Address,
                    PhoneNumber = user.PhoneNumber,
                    Avatar      = user.Avatar
                };

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Profile GET Error] {ex.Message}");
                TempData["ErrorMessage"] = "Không thể tải thông tin hồ sơ. Vui lòng thử lại.";
                return RedirectToAction("ChangePassword");
            }
        }

        // POST: /Manage/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public new async Task<ActionResult> Profile(ProfileViewModel model)
        {
            // Xóa validation cho AvatarFile vì không bắt buộc
            ModelState.Remove("AvatarFile");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var userId = User.Identity.GetUserId();
                var user = await UserManager.FindByIdAsync(userId);
                if (user == null)
                    return RedirectToAction("Login", "Account");

                // Nghiệp vụ: Email KHÔNG được đổi
                user.FullName    = model.FullName?.Trim();
                user.Address     = model.Address?.Trim();
                user.PhoneNumber = model.PhoneNumber?.Trim();

                const string avatarFolder = "/Content/Uploads/Avatars/";
                string oldAvatar = user.Avatar;
                string newAvatarPath = null;

                // Nghiệp vụ: Upload Avatar — chỉ khi có file mới
                if (model.AvatarFile != null && model.AvatarFile.ContentLength > 0)
                {
                    var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(model.AvatarFile.FileName)?.ToLowerInvariant();

                    // Kiểm tra định dạng file
                    if (!validExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("AvatarFile", "Chỉ chấp nhận file ảnh định dạng JPG, PNG, GIF.");
                        model.Email  = user.Email;
                        model.Avatar = user.Avatar;
                        return View(model);
                    }

                    // Kiểm tra kích thước tối đa 2MB
                    if (model.AvatarFile.ContentLength > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("AvatarFile", "Kích thước ảnh không được vượt quá 2MB.");
                        model.Email  = user.Email;
                        model.Avatar = user.Avatar;
                        return View(model);
                    }

                    // Kiểm tra nội dung thật của file (magic bytes), không tin vào đuôi file:
                    // chặn file HTML/SVG/script bị đổi tên thành .png
                    var detectedExtension = DetectImageExtension(model.AvatarFile.InputStream);
                    if (detectedExtension == null)
                    {
                        ModelState.AddModelError("AvatarFile", "File tải lên không phải ảnh JPG, PNG hoặc GIF hợp lệ.");
                        model.Email  = user.Email;
                        model.Avatar = user.Avatar;
                        return View(model);
                    }

                    var uploadDir = Server.MapPath("~" + avatarFolder);
                    if (!Directory.Exists(uploadDir))
                        Directory.CreateDirectory(uploadDir);

                    // Tên file duy nhất mỗi lần upload để không ghi đè ảnh cũ trước khi lưu CSDL thành công
                    var fileName = $"{userId}_{DateTime.UtcNow.Ticks}{detectedExtension}";
                    newAvatarPath = Path.Combine(uploadDir, fileName);
                    model.AvatarFile.SaveAs(newAvatarPath);

                    user.Avatar = avatarFolder + fileName;
                }

                var result = await UserManager.UpdateAsync(user);
                if (!result.Succeeded && newAvatarPath != null)
                    DeleteFileQuietly(newAvatarPath); // CSDL không lưu được → bỏ ảnh mới, giữ ảnh cũ

                if (result.Succeeded)
                {
                    // Chỉ xóa ảnh cũ khi ảnh mới đã lưu xong vào CSDL; chỉ xóa file trong thư mục Avatars
                    if (newAvatarPath != null && oldAvatar != null &&
                        oldAvatar.StartsWith(avatarFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        DeleteFileQuietly(Server.MapPath("~" + oldAvatar));
                    }

                    // Cập nhật lại session claims (FullName trong claim)
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    TempData["SuccessMessage"] = "Hồ sơ cá nhân đã được cập nhật thành công.";
                    return RedirectToAction("Profile");
                }

                AddErrors(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Profile POST Error] {ex.Message}");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật hồ sơ. Vui lòng thử lại.");
            }

            // Nạp lại Avatar + Email (không được lưu qua form submit)
            var currentUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            model.Email  = currentUser?.Email;
            model.Avatar = currentUser?.Avatar;
            return View(model);
        }

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════

        // Nhận diện ảnh qua chữ ký đầu file; trả về đuôi file tương ứng hoặc null nếu không phải JPG/PNG/GIF
        private static string DetectImageExtension(Stream stream)
        {
            var header = new byte[8];
            stream.Position = 0;
            var read = stream.Read(header, 0, header.Length);
            stream.Position = 0;

            if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return ".jpg";
            if (read >= 8 && header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
                return ".png";
            if (read >= 6 && header[0] == 'G' && header[1] == 'I' && header[2] == 'F' && header[3] == '8' &&
                (header[4] == '7' || header[4] == '9') && header[5] == 'a')
                return ".gif";

            return null;
        }

        private static void DeleteFileQuietly(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch (Exception ex)
            {
                // Không xóa được file thừa thì chỉ ghi log, không làm hỏng thao tác chính
                System.Diagnostics.Trace.TraceError($"[Avatar Delete Error] {path}: {ex.Message}");
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", TranslateIdentityError(error));
        }

        /// <summary>Dịch các lỗi phổ biến của ASP.NET Identity sang tiếng Việt</summary>
        private static string TranslateIdentityError(string error)
        {
            if (error.Contains("Incorrect password"))
                return "Mật khẩu hiện tại không đúng.";
            if (error.Contains("Password") && error.Contains("least"))
                return "Mật khẩu phải có ít nhất 6 ký tự.";
            if (error.Contains("Email") && error.Contains("already"))
                return "Địa chỉ email này đã được sử dụng.";
            if (error.Contains("UserName") && error.Contains("already"))
                return "Tên tài khoản này đã tồn tại.";
            return error; // Giữ nguyên nếu không match
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _userManager?.Dispose();
                _signInManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}