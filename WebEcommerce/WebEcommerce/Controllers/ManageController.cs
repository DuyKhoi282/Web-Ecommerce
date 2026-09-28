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
                System.Diagnostics.Debug.WriteLine($"[ChangePassword Error] {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[Profile GET Error] {ex.Message}");
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

                    // Lưu file với tên = {userId}{ext} để tránh trùng lặp
                    var uploadDir = Server.MapPath("~/Content/Uploads/Avatars/");
                    if (!Directory.Exists(uploadDir))
                        Directory.CreateDirectory(uploadDir);

                    // Xóa ảnh cũ nếu tồn tại
                    if (!string.IsNullOrEmpty(user.Avatar))
                    {
                        var oldPath = Server.MapPath("~" + user.Avatar);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    var fileName  = userId + extension;
                    var savePath  = Path.Combine(uploadDir, fileName);
                    model.AvatarFile.SaveAs(savePath);

                    user.Avatar = $"/Content/Uploads/Avatars/{fileName}";
                }

                var result = await UserManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    // Cập nhật lại session claims (FullName trong claim)
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    TempData["SuccessMessage"] = "Hồ sơ cá nhân đã được cập nhật thành công.";
                    return RedirectToAction("Profile");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Profile POST Error] {ex.Message}");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật hồ sơ. Vui lòng thử lại.");
            }

            // Nạp lại Avatar + Email (không được lưu qua form submit)
            var currentUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            model.Email  = currentUser?.Email;
            model.Avatar = currentUser?.Avatar;
            return View(model);
        }

        // ═══════════════════════════════════════════════════════
        //  Giữ lại các actions mặc định của Identity (LinkLogin...)
        // ═══════════════════════════════════════════════════════

        // POST: /Manage/LinkLogin (dùng bởi external login)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            return new AccountController.ChallengeResult(
                provider,
                Url.Action("LinkLoginCallback", "Manage"),
                User.Identity.GetUserId());
        }

        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });

            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded
                ? RedirectToAction("ManageLogins")
                : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════

        private const string XsrfKey = "XsrfId";

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

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            return user?.PasswordHash != null;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
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