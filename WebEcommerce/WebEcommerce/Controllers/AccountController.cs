using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController() { }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
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

        // ═══════════════════════════════════════════════════
        //  ĐĂNG KÝ
        // ═══════════════════════════════════════════════════

        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Nghiệp vụ: Gán FullName, IsActive = true, CreatedAt = now
                var user = new ApplicationUser
                {
                    UserName   = model.Email,
                    Email      = model.Email,
                    FullName   = model.FullName.Trim(),
                    IsActive   = true,
                    CreatedAt  = DateTime.UtcNow
                };

                var result = await UserManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Nghiệp vụ: Gán mặc định Role "Customer" ngay sau khi tạo
                    await UserManager.AddToRoleAsync(user.Id, "Customer");

                    // Nghiệp vụ: KHÔNG tự đăng nhập — redirect đến Login kèm thông báo
                    TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập để tiếp tục.";
                    return RedirectToAction("Login", "Account");
                }

                AddErrors(result);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                System.Diagnostics.Debug.WriteLine($"[Register Error] {msg}");
                ModelState.AddModelError("", $"Đã xảy ra lỗi trong quá trình đăng ký: {msg}");
            }

            return View(model);
        }

        // ═══════════════════════════════════════════════════
        //  ĐĂNG NHẬP
        // ═══════════════════════════════════════════════════

        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Nghiệp vụ: Kiểm tra IsActive TRƯỚC khi SignIn
                // (tránh để Identity xử lý — cần chặn rõ ràng)
                var user = await UserManager.FindByEmailAsync(model.Email);
                if (user != null && !user.IsActive)
                {
                    ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ.");
                    return View(model);
                }

                // Đảm bảo admin@thechillshop.vn luôn có quyền Administrator
                if (user != null && user.Email.ToLower() == "admin@thechillshop.vn")
                {
                    using (var adminDb = new ApplicationDbContext())
                    using (var roleMgr = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(adminDb)))
                    {
                        if (!roleMgr.RoleExists("Administrator"))
                        {
                            roleMgr.Create(new IdentityRole("Administrator"));
                        }
                    }
                    if (!await UserManager.IsInRoleAsync(user.Id, "Administrator"))
                    {
                        await UserManager.AddToRoleAsync(user.Id, "Administrator");
                    }
                }

                // Nghiệp vụ: shouldLockout = true → đếm lần sai, khóa sau 5 lần / 10 phút
                var result = await SignInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, shouldLockout: true);

                switch (result)
                {
                    case SignInStatus.Success:
                        if (user != null)
                        {
                            bool isAdminOrManager = await UserManager.IsInRoleAsync(user.Id, "Administrator")
                                                 || await UserManager.IsInRoleAsync(user.Id, "StoreManager");

                            // Nghiệp vụ: Admin/Manager luôn vào Dashboard (bất kể returnUrl)
                            if (isAdminOrManager)
                                return RedirectToAction("Index", "AdminDashboard");

                            // Nghiệp vụ: Customer không được redirect vào trang /Admin
                            // dù returnUrl có chứa /Admin (ví dụ: ai đó bookmark trang admin cũ)
                            if (!string.IsNullOrEmpty(returnUrl) &&
                                returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                            {
                                return RedirectToAction("Index", "Home");
                            }
                        }
                        return RedirectToLocal(returnUrl);

                    case SignInStatus.LockedOut:
                        // Tính thời gian còn lại
                        var lockedUser = await UserManager.FindByEmailAsync(model.Email);
                        var lockEnd = lockedUser?.LockoutEndDateUtc ?? DateTime.UtcNow.AddMinutes(10);
                        var remaining = (int)Math.Ceiling((lockEnd - DateTime.UtcNow).TotalMinutes);
                        ModelState.AddModelError("",
                            $"Tài khoản bị tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau {remaining} phút.");
                        return View(model);

                    case SignInStatus.Failure:
                    default:
                        // Nghiệp vụ: Thông báo mơ hồ — không tiết lộ email sai hay mật khẩu sai
                        ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                        return View(model);
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                System.Diagnostics.Debug.WriteLine($"[Login Error] {msg}");
                ModelState.AddModelError("", $"Đã xảy ra lỗi trong quá trình đăng nhập: {msg}");
            }

            return View(model);
        }

        // ═══════════════════════════════════════════════════
        //  ĐĂNG XUẤT
        // ═══════════════════════════════════════════════════

        // POST: /Account/LogOff  (GET bị tắt — bảo vệ CSRF logout attack)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Login", "Account");
        }

        // ═══════════════════════════════════════════════════
        //  QUÊN MẬT KHẨU
        // ═══════════════════════════════════════════════════

        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var user = await UserManager.FindByEmailAsync(model.Email);

                // Nghiệp vụ: Luôn hiện thông báo chung dù email có tồn tại hay không
                // → chống tấn công liệt kê email (email enumeration attack)
                if (user == null || !user.IsActive)
                {
                    return RedirectToAction("ForgotPasswordConfirmation", "Account");
                }

                // Tạo token reset (hết hạn sau 24h — cấu hình trong IdentityConfig)
                var code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action(
                    "ResetPassword", "Account",
                    new { userId = user.Id, code },
                    protocol: Request.Url.Scheme);

                // Gửi email (hiện tại: log ra Debug, sau tích hợp MailKit)
                await UserManager.SendEmailAsync(user.Id,
                    "Đặt lại mật khẩu - WebEcommerce",
                    $"Nhấn vào đường link sau để đặt lại mật khẩu (hết hạn sau 24 giờ):<br/><a href='{callbackUrl}'>{callbackUrl}</a>");

                // Dev mode: Lưu link vào TempData để test mà không cần email thật
                TempData["ResetLink"] = callbackUrl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ForgotPassword Error] {ex.Message}");
            }

            return RedirectToAction("ForgotPasswordConfirmation", "Account");
        }

        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // ═══════════════════════════════════════════════════
        //  ĐẶT LẠI MẬT KHẨU
        // ═══════════════════════════════════════════════════

        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            if (code == null)
            {
                return HttpNotFound();
            }
            return View();
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var user = await UserManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Nghiệp vụ: Không tiết lộ email có tồn tại hay không
                    TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập.";
                    return RedirectToAction("Login", "Account");
                }

                var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập.";
                    return RedirectToAction("Login", "Account");
                }

                AddErrors(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ResetPassword Error] {ex.Message}");
                ModelState.AddModelError("", "Đã xảy ra lỗi. Vui lòng yêu cầu link đặt lại mật khẩu mới.");
            }

            return View(model);
        }

        // ═══════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error);
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            // Nghiệp vụ: Chỉ redirect về URL nội bộ (cùng domain) — chống Open Redirect Attack
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // Giữ lại ChallengeResult cho ManageController (external login)
        internal class ChallengeResult : HttpUnauthorizedResult
        {
            private const string XsrfKey = "XsrfId";

            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null) { }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                    properties.Dictionary[XsrfKey] = UserId;
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
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