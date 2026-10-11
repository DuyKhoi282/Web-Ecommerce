using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebEcommerce.Models;
using static System.Web.Razor.Parser.SyntaxConstants;

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
        // XÁC THỰC OTP (One-Time Password)
        // ═══════════════════════════════════════════════════
        // GET: /Account/VerifyOtp
        [AllowAnonymous]
        public ActionResult VerifyOtp()
        {
            var email =
                Session["PendingRegistration.Email"] as string;

            // Không có thông tin đăng ký tạm thời
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register", "Account");
            }

            var model = new VerifyOtpViewModel
            {
                Email = email
            };

            return View(model);
        }

        // POST: /Account/VerifyOtp
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // ==========================================
                // 1. Lấy thông tin đăng ký tạm thời
                // ==========================================

                var fullName =
                    Session["PendingRegistration.FullName"] as string;

                var email =
                    Session["PendingRegistration.Email"] as string;

                var passwordHash =
                    Session["PendingRegistration.PasswordHash"] as string;

                var storedOtp =
                    Session["PendingRegistration.Otp"] as string;

                var otpExpiresAt =
                    Session["PendingRegistration.OtpExpiresAt"] as DateTime?;
                var otpAttempts =
                    Session["PendingRegistration.OtpAttempts"] != null
                     ? (int)Session["PendingRegistration.OtpAttempts"] : 0;


                // ==========================================
                // 2. Kiểm tra thông tin tạm thời
                // ==========================================

                if (string.IsNullOrEmpty(fullName) ||
                    string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(passwordHash) ||
                    string.IsNullOrEmpty(storedOtp) ||
                    !otpExpiresAt.HasValue)
                {
                    ModelState.AddModelError(
                        "",
                        "Thông tin đăng ký đã hết hạn. Vui lòng đăng ký lại."
                    );

                    return View(model);
                }


                // ==========================================
                // 3. Kiểm tra OTP hết hạn
                // ==========================================

                if (DateTime.UtcNow > otpExpiresAt.Value)
                {
                    // Xóa thông tin đăng ký tạm thời
                    Session.Remove("PendingRegistration.FullName");
                    Session.Remove("PendingRegistration.Email");
                    Session.Remove("PendingRegistration.PasswordHash");
                    Session.Remove("PendingRegistration.Otp");
                    Session.Remove("PendingRegistration.OtpExpiresAt");
                    Session.Remove("PendingRegistration.OtpAttempts");

                    ModelState.AddModelError(
                        "Code",
                        "Mã xác thực đã hết hạn. Vui lòng đăng ký lại."
                    );

                    return View(model);
                }


                // ==========================================
                // 4. Kiểm tra OTP
                // ==========================================

                if (!string.Equals(
                    model.Code.Trim(),
                    storedOtp,
                    StringComparison.Ordinal))
                {
                    // Tăng số lần nhập sai
                    otpAttempts++;

                    Session["PendingRegistration.OtpAttempts"] =
                        otpAttempts;

                    // Đã sai đủ 5 lần
                    if (otpAttempts >= 5)
                    {
                        // Xóa toàn bộ thông tin đăng ký tạm thời
                        Session.Remove("PendingRegistration.FullName");
                        Session.Remove("PendingRegistration.Email");
                        Session.Remove("PendingRegistration.PasswordHash");
                        Session.Remove("PendingRegistration.Otp");
                        Session.Remove("PendingRegistration.OtpExpiresAt");
                        Session.Remove("PendingRegistration.OtpAttempts");

                        ModelState.AddModelError(
                            "Code",
                            "Bạn đã nhập sai mã xác thực quá 5 lần. Vui lòng đăng ký lại để nhận mã mới."
                        );

                        return View(model);
                    }

                    // Vẫn còn lượt thử
                    var remainingAttempts = 5 - otpAttempts;

                    ModelState.AddModelError(
                        "Code",
                        $"Mã xác thực không chính xác. Bạn còn {remainingAttempts} lần thử."
                    );

                    return View(model);
                }


                // ==========================================
                // 5. Tạo tài khoản
                // ==========================================

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,

                    FullName = fullName,

                    // Password đã được hash từ bước Register
                    PasswordHash = passwordHash,

                    // OTP đúng => Email đã được xác thực
                    EmailConfirmed = true,

                    IsActive = true,

                    CreatedAt = DateTime.UtcNow
                };

                var result = await UserManager.CreateAsync(user);


                // ==========================================
                // 6. Kiểm tra tạo tài khoản
                // ==========================================

                if (!result.Succeeded)
                {
                    AddErrors(result);
                    return View(model);
                }


                // ==========================================
                // 7. Gán Role Customer
                // ==========================================

                var roleResult =
                    await UserManager.AddToRoleAsync(
                        user.Id,
                        "Customer"
                    );

                if (!roleResult.Succeeded)
                {
                    AddErrors(roleResult);
                    return View(model);
                }


                
                // ==========================================
                // 8. Gửi mail chào mừng
                // ==========================================

try
                {
                    await UserManager.SendEmailAsync(
                        user.Id,
                        "Đăng ký tài khoản thành công - The Chill Shop",
                        $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Chào mừng đến với The Chill Shop</title>
</head>

<body style='margin:0; padding:0; background-color:#f4f5f7;
             font-family:Arial, Helvetica, sans-serif; color:#333333;'>

    <table role='presentation' width='100%' cellpadding='0'
           cellspacing='0' border='0'
           style='background-color:#f4f5f7; padding:30px 12px;'>
        <tr>
            <td align='center'>

                <table role='presentation' width='600' cellpadding='0'
                       cellspacing='0' border='0'
                       style='width:100%; max-width:600px;
                              background-color:#ffffff;
                              border-radius:10px; overflow:hidden;'>

                    <!-- HEADER -->
                    <tr>
                        <td align='center'
                            style='background-color:#d70018;
                                   padding:28px 20px;'>

                            <div style='font-size:26px; font-weight:bold;
                                        color:#ffffff; letter-spacing:0.3px;'>
                                The Chill Shop
                            </div>

                            <div style='font-size:13px; color:#ffffff;
                                        margin-top:8px;'>
                                Công nghệ trong tầm tay
                            </div>
                        </td>
                    </tr>

                    <!-- WELCOME BANNER -->
                    <tr>
                        <td align='center'
                            style='padding:32px 24px 16px;'>

                            <div style='display:inline-block;
                                        background-color:#fff0f1;
                                        color:#d70018;
                                        padding:8px 16px;
                                        border-radius:20px;
                                        font-size:12px;
                                        font-weight:bold;'>
                                ĐĂNG KÝ THÀNH CÔNG
                            </div>

                            <h1 style='margin:20px 0 10px;
                                       font-size:25px;
                                       line-height:1.4;
                                       color:#222222;'>
                                Chào mừng bạn đến với
                                <span style='color:#d70018;'>
                                    The Chill Shop!
                                </span>
                            </h1>

                            <p style='margin:0; font-size:14px;
                                      line-height:1.8; color:#666666;'>
                                Cảm ơn bạn đã lựa chọn đồng hành cùng chúng tôi.
                            </p>
                        </td>
                    </tr>

                    <!-- MAIN CONTENT -->
                    <tr>
                        <td style='padding:12px 30px 30px;
                                   font-size:14px; line-height:1.9;
                                   color:#444444;'>

                            <p>
                                Xin chào
                                <strong style='color:#222222;'>
                                    {HttpUtility.HtmlEncode(user.FullName)}
                                </strong>,
                            </p>

                            <p>
                                Tài khoản của bạn đã được đăng ký thành công
                                và email đã được xác thực.
                                Bạn đã sẵn sàng khám phá thế giới công nghệ
                                tại The Chill Shop!
                            </p>

                            <!-- ACCOUNT INFORMATION -->
                            <table role='presentation' width='100%'
                                   cellpadding='0' cellspacing='0' border='0'
                                   style='background-color:#fff5f5;
                                          border:1px solid #ffe0e3;
                                          border-radius:8px;
                                          margin:22px 0;'>
                                <tr>
                                    <td style='padding:18px 20px;'>

                                        <p style='margin:0 0 12px;
                                                  font-size:15px;
                                                  font-weight:bold;
                                                  color:#d70018;'>
                                            THÔNG TIN TÀI KHOẢN
                                        </p>

                                        <p style='margin:0 0 6px;'>
                                            <strong>Email đăng nhập:</strong>
                                        </p>

                                        <p style='margin:0;
                                                  color:#333333;
                                                  word-break:break-word;'>
                                            {HttpUtility.HtmlEncode(user.Email)}
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <p>
                                Bạn có thể đăng nhập để khám phá các sản phẩm,
                                cập nhật ưu đãi và trải nghiệm mua sắm
                                thuận tiện tại cửa hàng.
                            </p>

                            <!-- CTA BUTTON -->
                            <table role='presentation' cellpadding='0'
                                   cellspacing='0' border='0'
                                   align='center'
                                   style='margin:28px auto;'>
                                <tr>
                                    <td align='center'
                                        bgcolor='#d70018'
                                        style='border-radius:6px;'>
                                        <a href='https://YOUR-WEBSITE-URL'
                                           style='display:inline-block;
                                                  padding:14px 30px;
                                                  font-size:14px;
                                                  font-weight:bold;
                                                  color:#ffffff;
                                                  text-decoration:none;
                                                  border-radius:6px;'>
                                            KHÁM PHÁ CỬA HÀNG
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <p style='margin-top:24px;'>
                                Nếu bạn không thực hiện đăng ký tài khoản này,
                                vui lòng liên hệ với bộ phận hỗ trợ của chúng tôi.
                            </p>

                            <p style='margin-bottom:0;'>
                                Trân trọng,<br>
                                <strong style='color:#d70018;'>
                                    Đội ngũ The Chill Shop
                                </strong>
                            </p>
                        </td>
                    </tr>

                    <!-- FOOTER -->
                    <tr>
                        <td align='center'
                            style='background-color:#f8f8f8;
                                   border-top:1px solid #eeeeee;
                                   padding:22px 20px;'>

                            <p style='margin:0 0 8px;
                                      font-size:13px;
                                      font-weight:bold;
                                      color:#d70018;'>
                                THE CHILL SHOP
                            </p>

                            <p style='margin:0;
                                      font-size:12px;
                                      line-height:1.8;
                                      color:#888888;'>
                                Cảm ơn bạn đã tin tưởng và lựa chọn chúng tôi.
                                <br>
                                Email này được gửi tự động.
                                Vui lòng không trả lời trực tiếp email này.
                            </p>

                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>"
                    );
                }
                catch (Exception emailEx)
                {
                    // Tài khoản vẫn được tạo thành công
                    // dù mail chào mừng không gửi được.

                    System.Diagnostics.Debug.WriteLine(
                        $"[Welcome Email Error] {emailEx.Message}"
                    );
                }
            


                // ==========================================
                // 9. Xóa dữ liệu đăng ký tạm thời
                // ==========================================

                Session.Remove("PendingRegistration.FullName");
                Session.Remove("PendingRegistration.Email");
                Session.Remove("PendingRegistration.PasswordHash");
                Session.Remove("PendingRegistration.Otp");
                Session.Remove("PendingRegistration.OtpExpiresAt");
                Session.Remove("PendingRegistration.OtpAttempts");

                // ==========================================
                // 10. Chuyển về Login
                // ==========================================

                TempData["SuccessMessage"] =
                    "Xác thực email thành công! Tài khoản đã được tạo. Vui lòng đăng nhập.";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null
                    ? $"{ex.Message} -> {ex.InnerException.Message}"
                    : ex.Message;

                System.Diagnostics.Debug.WriteLine(
                    $"[VerifyOtp Error] {msg}"
                );

                ModelState.AddModelError(
                    "",
                    $"Đã xảy ra lỗi trong quá trình xác thực: {msg}"
                );
            }

            return View(model);
        }

        //======================================
        //  GỬI LẠI OTP
        //======================================
        // POST: /Account/ResendOtp
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResendOtp()
        {
            try
            {
                // ==========================================
                // 1. Kiểm tra đăng ký tạm thời
                // ==========================================

                var fullName =
                    Session["PendingRegistration.FullName"] as string;

                var email =
                    Session["PendingRegistration.Email"] as string;

                var passwordHash =
                    Session["PendingRegistration.PasswordHash"] as string;

                if (string.IsNullOrEmpty(fullName) ||
                    string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(passwordHash))
                {
                    TempData["OtpError"] =
                        "Phiên đăng ký đã hết hạn. Vui lòng đăng ký lại.";

                    return RedirectToAction("Register", "Account");
                }


                // ==========================================
                // 2. Kiểm tra cooldown 60 giây
                // ==========================================

                var lastSentAt =
                    Session["PendingRegistration.OtpLastSentAt"] as DateTime?;

                if (lastSentAt.HasValue)
                {
                    var elapsed =
                        DateTime.UtcNow - lastSentAt.Value;

                    if (elapsed.TotalSeconds < 60)
                    {
                        var remainingSeconds =
                            60 - (int)elapsed.TotalSeconds;

                        TempData["OtpError"] =
                            $"Vui lòng chờ {remainingSeconds} giây trước khi gửi lại mã.";

                        return RedirectToAction("VerifyOtp", "Account");
                    }
                }


                // ==========================================
                // 3. Tạo OTP mới
                // ==========================================

                var newOtp = GenerateOtp();


                // ==========================================
                // 4. Cập nhật OTP mới
                // ==========================================

                Session["PendingRegistration.Otp"] =
                    newOtp;

                Session["PendingRegistration.OtpExpiresAt"] =
                    DateTime.UtcNow.AddMinutes(10);

                // Reset số lần nhập sai
                Session["PendingRegistration.OtpAttempts"] = 0;

                // Cập nhật thời điểm gửi
                Session["PendingRegistration.OtpLastSentAt"] =
                    DateTime.UtcNow;


                // ==========================================
                // 5. Gửi OTP mới
                // ==========================================

                try
                {
                    var emailService =
                        new EmailService();

                    await emailService.SendAsync(
                        new IdentityMessage
                        {
                            Destination = email,

                            Subject =
                                "Mã xác thực mới - The Chill Shop",
                            Body = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Mã xác thực mới - The Chill Shop</title>
</head>

<body style='margin:0; padding:0; background-color:#f4f5f7;
             font-family:Arial, Helvetica, sans-serif; color:#333333;'>

    <table role='presentation' width='100%' cellpadding='0'
           cellspacing='0' border='0'
           style='background-color:#f4f5f7; padding:30px 12px;'>
        <tr>
            <td align='center'>

                <table role='presentation' width='600' cellpadding='0'
                       cellspacing='0' border='0'
                       style='width:100%; max-width:600px;
                              background-color:#ffffff;
                              border-radius:10px; overflow:hidden;'>

                    <!-- HEADER -->
                    <tr>
                        <td align='center'
                            style='background-color:#d70018;
                                   padding:26px 20px;'>

                            <div style='font-size:26px; font-weight:bold;
                                        color:#ffffff;'>
                                The Chill Shop
                            </div>

                            <div style='font-size:13px; color:#ffffff;
                                        margin-top:8px;'>
                                Công nghệ trong tầm tay
                            </div>
                        </td>
                    </tr>

                    <!-- TITLE -->
                    <tr>
                        <td align='center'
                            style='padding:32px 24px 12px;'>

                            <div style='display:inline-block;
                                        background-color:#fff0f1;
                                        color:#d70018;
                                        padding:8px 16px;
                                        border-radius:20px;
                                        font-size:12px;
                                        font-weight:bold;'>
                                GỬI LẠI MÃ XÁC THỰC
                            </div>

                            <h1 style='margin:20px 0 10px;
                                       font-size:24px;
                                       line-height:1.4;
                                       color:#222222;'>
                                Mã OTP mới của bạn
                            </h1>

                            <p style='margin:0; font-size:14px;
                                      line-height:1.8; color:#666666;'>
                                Sử dụng mã mới bên dưới để tiếp tục đăng ký.
                            </p>
                        </td>
                    </tr>

                    <!-- CONTENT -->
                    <tr>
                        <td style='padding:12px 30px 30px;
                                   font-size:14px; line-height:1.9;
                                   color:#444444;'>

                            <p>
                                Xin chào
                                <strong style='color:#222222;'>
                                    {HttpUtility.HtmlEncode(fullName)}
                                </strong>,
                            </p>

                            <p>
                                Chúng tôi đã nhận được yêu cầu gửi lại mã
                                xác thực cho tài khoản The Chill Shop của bạn.
                                Vui lòng sử dụng mã OTP mới dưới đây.
                            </p>

                            <!-- OTP BOX -->
                            <table role='presentation' width='100%'
                                   cellpadding='0' cellspacing='0' border='0'
                                   style='margin:24px 0;
                                          background-color:#fff5f5;
                                          border:1px solid #ffd9de;
                                          border-radius:8px;'>
                                <tr>
                                    <td align='center'
                                        style='padding:24px 16px;'>

                                        <p style='margin:0 0 12px;
                                                  font-size:13px;
                                                  color:#666666;'>
                                            MÃ XÁC THỰC MỚI CỦA BẠN
                                        </p>

                                        <div style='font-size:32px;
                                                    font-weight:bold;
                                                    letter-spacing:8px;
                                                    color:#d70018;
                                                    line-height:1.5;
                                                    overflow-wrap:anywhere;'>
                                            {newOtp}
                                        </div>

                                        <p style='margin:12px 0 0;
                                                  font-size:13px;
                                                  color:#666666;'>
                                            Có hiệu lực trong
                                            <strong style='color:#d70018;'>
                                                10 phút
                                            </strong>
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <!-- OLD OTP NOTICE -->
                            <table role='presentation' width='100%'
                                   cellpadding='0' cellspacing='0' border='0'
                                   style='background-color:#f8f8f8;
                                          border-left:4px solid #d70018;
                                          margin:20px 0;'>
                                <tr>
                                    <td style='padding:14px 16px;
                                               font-size:13px;
                                               line-height:1.8;
                                               color:#555555;'>

                                        <strong style='color:#333333;'>
                                            Lưu ý quan trọng
                                        </strong>
                                        <br>
                                        Mã OTP trước đó đã hết hiệu lực.
                                        Bạn chỉ nên sử dụng mã mới nhất
                                        được gửi đến email này.
                                    </td>
                                </tr>
                            </table>

                            <p>
                                Vì lý do bảo mật, vui lòng không chia sẻ mã OTP
                                với bất kỳ ai. Nếu bạn không yêu cầu gửi lại mã,
                                hãy bỏ qua email này.
                            </p>

                            <p style='margin-bottom:0;'>
                                Trân trọng,<br>
                                <strong style='color:#d70018;'>
                                    Đội ngũ The Chill Shop
                                </strong>
                            </p>
                        </td>
                    </tr>

                    <!-- FOOTER -->
                    <tr>
                        <td align='center'
                            style='background-color:#f8f8f8;
                                   border-top:1px solid #eeeeee;
                                   padding:22px 20px;'>

                            <p style='margin:0 0 8px;
                                      font-size:13px;
                                      font-weight:bold;
                                      color:#d70018;'>
                                THE CHILL SHOP
                            </p>

                            <p style='margin:0;
                                      font-size:12px;
                                      line-height:1.8;
                                      color:#888888;'>
                                Email được gửi tự động để xác thực tài khoản.
                                <br>
                                Vui lòng không trả lời trực tiếp email này.
                            </p>
                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>"
                        }
                    );
                }
                catch (Exception emailEx)
                {
                    // Nếu gửi email thất bại,
                    // xóa OTP mới để không sử dụng mã chưa được gửi thành công.

                    Session.Remove("PendingRegistration.Otp");
                    Session.Remove("PendingRegistration.OtpExpiresAt");
                    Session.Remove("PendingRegistration.OtpLastSentAt");

                    System.Diagnostics.Debug.WriteLine(
                        $"[Resend OTP Email Error] {emailEx.Message}"
                    );

                    TempData["OtpError"] =
                        "Không thể gửi mã xác thực. Vui lòng thử lại.";

                    return RedirectToAction(
                        "VerifyOtp",
                        "Account"
                    );
                }


                // ==========================================
                // 6. Thông báo thành công
                // ==========================================

                TempData["OtpSuccess"] =
                    "Mã xác thực mới đã được gửi đến email của bạn.";

                return RedirectToAction(
                    "VerifyOtp",
                    "Account"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ResendOtp Error] {ex.Message}"
                );

                TempData["OtpError"] =
                    "Đã xảy ra lỗi. Vui lòng thử lại.";

                return RedirectToAction(
                    "VerifyOtp",
                    "Account"
                );
            }
        }

        // ═══════════════════════════════════════════════════
        //  ĐĂNG KÝ
        // ═══════════════════════════════════════════════════

        // Tạo mã OTP 6 chữ số ngẫu nhiên
        private string GenerateOtp()
        {
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];

                rng.GetBytes(bytes);

                var number = BitConverter.ToUInt32(bytes, 0) % 1000000;

                return number.ToString("D6");
            }
        }

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
                // 1. Kiểm tra email đã tồn tại chưa
                var existingUser = await UserManager.FindByEmailAsync(model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError(
                        "Email",
                        "Email này đã được sử dụng."
                    );

                    return View(model);
                }

                // 2. Kiểm tra password theo cấu hình Identity
                var passwordValidation =
                    await UserManager.PasswordValidator.ValidateAsync(model.Password);

                if (!passwordValidation.Succeeded)
                {
                    AddErrors(passwordValidation);
                    return View(model);
                }

                // 3. Tạo mã OTP 6 chữ số
                var otp = GenerateOtp();

                // 4. Hash password trước khi lưu tạm
                // Không lưu password gốc vào Session
                var passwordHash =
                    UserManager.PasswordHasher.HashPassword(model.Password);

                // 5. Lưu thông tin đăng ký tạm thời vào Session
                Session["PendingRegistration.FullName"] =
                    model.FullName.Trim();

                Session["PendingRegistration.Email"] =
                    model.Email.Trim();

                Session["PendingRegistration.PasswordHash"] =
                    passwordHash;

                Session["PendingRegistration.Otp"] =
                    otp;

                Session["PendingRegistration.OtpExpiresAt"] =
                    DateTime.UtcNow.AddMinutes(10);

                Session["PendingRegistration.OtpAttempts"] = 0;

                Session["PendingRegistration.OtpLastSentAt"] =
                    DateTime.UtcNow;

                // 6. Gửi OTP đến email
                try
                {
                    // Email chưa có UserId vì tài khoản chưa được tạo.
                    // Vì vậy gửi trực tiếp thông qua EmailService.
                    var emailService = new EmailService();

                    await emailService.SendAsync(
                        new IdentityMessage
                        {
                            Destination = model.Email,
                            Subject = "Mã xác thực đăng ký - The Chill Shop",
                            Body = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Xác thực email - The Chill Shop</title>
</head>

<body style='margin:0; padding:0; background-color:#f4f5f7;
             font-family:Arial, Helvetica, sans-serif; color:#333333;'>

    <table role='presentation' width='100%' cellpadding='0'
           cellspacing='0' border='0'
           style='background-color:#f4f5f7; padding:30px 12px;'>
        <tr>
            <td align='center'>

                <table role='presentation' width='600' cellpadding='0'
                       cellspacing='0' border='0'
                       style='width:100%; max-width:600px;
                              background-color:#ffffff;
                              border-radius:10px; overflow:hidden;'>

                    <!-- HEADER -->
                    <tr>
                        <td align='center'
                            style='background-color:#d70018; padding:26px 20px;'>

                            <div style='font-size:26px; font-weight:bold;
                                        color:#ffffff;'>
                                The Chill Shop
                            </div>

                            <div style='font-size:13px; color:#ffffff;
                                        margin-top:8px;'>
                                Công nghệ trong tầm tay
                            </div>
                        </td>
                    </tr>

                    <!-- TITLE -->
                    <tr>
                        <td align='center'
                            style='padding:32px 24px 12px;'>

                            <div style='display:inline-block;
                                        background-color:#fff0f1;
                                        color:#d70018;
                                        padding:8px 16px;
                                        border-radius:20px;
                                        font-size:12px;
                                        font-weight:bold;'>
                                XÁC THỰC TÀI KHOẢN
                            </div>

                            <h1 style='margin:20px 0 10px;
                                       font-size:24px;
                                       line-height:1.4;
                                       color:#222222;'>
                                Xác thực email của bạn
                            </h1>

                            <p style='margin:0; font-size:14px;
                                      line-height:1.8; color:#666666;'>
                                Chỉ còn một bước nữa để hoàn tất đăng ký!
                            </p>
                        </td>
                    </tr>

                    <!-- CONTENT -->
                    <tr>
                        <td style='padding:12px 30px 30px;
                                   font-size:14px; line-height:1.9;
                                   color:#444444;'>

                            <p>
                                Xin chào
                                <strong style='color:#222222;'>
                                    {HttpUtility.HtmlEncode(model.FullName)}
                                </strong>,
                            </p>

                            <p>
                                Cảm ơn bạn đã đăng ký tài khoản tại
                                <strong>The Chill Shop</strong>.
                                Vui lòng sử dụng mã xác thực bên dưới
                                để xác minh địa chỉ email của bạn.
                            </p>

                            <!-- OTP BOX -->
                            <table role='presentation' width='100%'
                                   cellpadding='0' cellspacing='0' border='0'
                                   style='margin:24px 0;
                                          background-color:#fff5f5;
                                          border:1px solid #ffd9de;
                                          border-radius:8px;'>
                                <tr>
                                    <td align='center'
                                        style='padding:24px 16px;'>

                                        <p style='margin:0 0 12px;
                                                  font-size:13px;
                                                  color:#666666;'>
                                            MÃ XÁC THỰC CỦA BẠN
                                        </p>

                                        <div style='font-size:32px;
                                                    font-weight:bold;
                                                    letter-spacing:8px;
                                                    color:#d70018;
                                                    line-height:1.5;
                                                    overflow-wrap:anywhere;'>
                                            {otp}
                                        </div>

                                        <p style='margin:12px 0 0;
                                                  font-size:13px;
                                                  color:#666666;'>
                                            Mã có hiệu lực trong
                                            <strong style='color:#d70018;'>
                                                10 phút
                                            </strong>
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <!-- SECURITY NOTICE -->
                            <table role='presentation' width='100%'
                                   cellpadding='0' cellspacing='0' border='0'
                                   style='background-color:#f8f8f8;
                                          border-left:4px solid #d70018;
                                          margin:20px 0;'>
                                <tr>
                                    <td style='padding:14px 16px;
                                               font-size:13px;
                                               line-height:1.8;
                                               color:#555555;'>
                                        <strong style='color:#333333;'>
                                            Lưu ý bảo mật
                                        </strong>
                                        <br>
                                        Không chia sẻ mã OTP này với bất kỳ ai.
                                        The Chill Shop sẽ không yêu cầu bạn
                                        cung cấp mã xác thực qua điện thoại.
                                    </td>
                                </tr>
                            </table>

                            <p>
                                Nếu bạn không thực hiện đăng ký tài khoản,
                                vui lòng bỏ qua email này và không chia sẻ mã OTP.
                            </p>

                            <p style='margin-bottom:0;'>
                                Trân trọng,<br>
                                <strong style='color:#d70018;'>
                                    Đội ngũ The Chill Shop
                                </strong>
                            </p>
                        </td>
                    </tr>

                    <!-- FOOTER -->
                    <tr>
                        <td align='center'
                            style='background-color:#f8f8f8;
                                   border-top:1px solid #eeeeee;
                                   padding:22px 20px;'>

                            <p style='margin:0 0 8px;
                                      font-size:13px;
                                      font-weight:bold;
                                      color:#d70018;'>
                                THE CHILL SHOP
                            </p>

                            <p style='margin:0;
                                      font-size:12px;
                                      line-height:1.8;
                                      color:#888888;'>
                                Email được gửi tự động để xác thực tài khoản.
                                <br>
                                Vui lòng không trả lời trực tiếp email này.
                            </p>
                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>"
                        }
                    );
                }
                catch (Exception emailEx)
                {
                    // Nếu gửi mail thất bại thì xóa dữ liệu đăng ký tạm
                    Session.Remove("PendingRegistration.FullName");
                    Session.Remove("PendingRegistration.Email");
                    Session.Remove("PendingRegistration.PasswordHash");
                    Session.Remove("PendingRegistration.Otp");
                    Session.Remove("PendingRegistration.OtpExpiresAt");

                    System.Diagnostics.Debug.WriteLine(
                        $"[Register OTP Email Error] {emailEx.Message}"
                    );

                    ModelState.AddModelError(
                        "",
                        "Không thể gửi mã xác thực đến email. Vui lòng thử lại."
                    );

                    return View(model);
                }

                // 7. Chuyển sang trang nhập OTP
                return RedirectToAction("VerifyOtp", "Account");
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null
                    ? $"{ex.Message} -> {ex.InnerException.Message}"
                    : ex.Message;

                System.Diagnostics.Debug.WriteLine(
                    $"[Register OTP Error] {msg}"
                );

                ModelState.AddModelError(
                    "",
                    $"Đã xảy ra lỗi trong quá trình đăng ký: {msg}"
                );
            }

            return View(model);
        }

        //=======================
        //  XÁC THỰC EMAIL (Email Confirmation)
        //=======================
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return HttpNotFound();
            }

            try
            {
                var result = await UserManager.ConfirmEmailAsync(userId, code);

                if (result.Succeeded)
                {
                    ViewBag.Message = "Email của bạn đã được xác thực thành công.";
                    return View();
                }

                ViewBag.Message = "Liên kết xác thực không hợp lệ hoặc đã hết hạn.";
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ConfirmEmail Error] {ex.Message}");

                ViewBag.Message =
                    "Đã xảy ra lỗi trong quá trình xác thực email.";

                return View();
            }
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
                /*await UserManager.SendEmailAsync(user.Id,
                    "Đặt lại mật khẩu - WebEcommerce",
                    $"Nhấn vào đường link sau để đặt lại mật khẩu (hết hạn sau 24 giờ):<br/><a href='{callbackUrl}'>{callbackUrl}</a>");*/

                // Dev mode: Lưu link vào TempData để test mà không cần email thật
                // TempData["ResetLink"] = callbackUrl; ( Phúc ẩn để phát triển mail )

                // Phúc làm lại template email đẹp hơn, gửi HTML email
                await UserManager.SendEmailAsync(
    user.Id,
"Đặt lại mật khẩu - The Chill Shop",
$@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Đặt lại mật khẩu - The Chill Shop</title>
</head>

<body style='margin:0; padding:0; background-color:#f4f5f7;
             font-family:Arial, Helvetica, sans-serif; color:#333333;'>

    <table role='presentation' width='100%' cellpadding='0'
           cellspacing='0' border='0'
           style='background-color:#f4f5f7; padding:30px 12px;'>
        <tr>
            <td align='center'>

                <table role='presentation' width='600' cellpadding='0'
                       cellspacing='0' border='0'
                       style='width:100%; max-width:600px;
                              background-color:#ffffff;
                              border-radius:10px; overflow:hidden;'>

                    <!-- HEADER -->
                    <tr>
                        <td align='center'
                            style='background-color:#d70018;
                                   padding:26px 20px;'>

                            <div style='font-size:26px; font-weight:bold;
                                        color:#ffffff;'>
                                The Chill Shop
                            </div>

                            <div style='font-size:13px; color:#ffffff;
                                        margin-top:8px;'>
                                Công nghệ trong tầm tay
                            </div>
                        </td>
                    </tr>

                    <!-- ICON -->
                    <tr>
                        <td align='center'
                            style='padding:28px 20px 0;'>

                            <div style='width:64px; height:64px;
                                        background-color:#fff0f1;
                                        border-radius:16px;
                                        line-height:64px;
                                        font-size:30px;
                                        color:#d70018;'>
                                &#128274;
                            </div>
                        </td>
                    </tr>

                    <!-- TITLE -->
                    <tr>
                        <td align='center'
                            style='padding:24px 30px 10px;'>

                            <div style='display:inline-block;
                                        background-color:#fff0f1;
                                        color:#d70018;
                                        padding:7px 14px;
                                        border-radius:20px;
                                        font-size:12px;
                                        font-weight:bold;'>
                                BẢO MẬT TÀI KHOẢN
                            </div>

                            <h1 style='margin:18px 0 12px;
                                       font-size:24px;
                                       line-height:1.4;
                                       color:#222222;'>
                                Đặt lại mật khẩu
                            </h1>

                            <p style='margin:0;
                                      font-size:14px;
                                      line-height:1.8;
                                      color:#666666;'>
                                Khôi phục quyền truy cập tài khoản của bạn
                                một cách an toàn.
                            </p>
                        </td>
                    </tr>

                    <!-- CONTENT -->
                    <tr>
                        <td style='padding:18px 30px 10px;
                                   font-size:14px;
                                   line-height:1.9;
                                   color:#444444;'>

                            <p style='margin:0 0 15px;'>
                                Xin chào,
                            </p>

                            <p style='margin:0 0 15px;'>
                                Chúng tôi nhận được yêu cầu đặt lại mật khẩu
                                cho tài khoản
                                <strong style='color:#d70018;'>
                                    The Chill Shop
                                </strong>
                                của bạn.
                            </p>

                            <p style='margin:0 0 15px;'>
                                Nếu bạn đã thực hiện yêu cầu này,
                                hãy nhấn vào nút bên dưới để tạo mật khẩu mới.
                            </p>

                        </td>
                    </tr>

                    <!-- BUTTON -->
                    <tr>
                        <td align='center'
                            style='padding:15px 24px 28px;'>

                            <table role='presentation' cellpadding='0'
                                   cellspacing='0' border='0'>
                                <tr>
                                    <td align='center'
                                        bgcolor='#d70018'
                                        style='border-radius:6px;'>

                                        <a href='{callbackUrl}'
                                           style='display:inline-block;
                                                  padding:14px 30px;
                                                  font-size:14px;
                                                  font-weight:bold;
                                                  line-height:1.4;
                                                  color:#ffffff;
                                                  text-decoration:none;
                                                  border-radius:6px;'>
                                            ĐẶT LẠI MẬT KHẨU
                                        </a>

                                    </td>
                                </tr>
                            </table>

                        </td>
                    </tr>

                    <!-- EXPIRY NOTICE -->
                    <tr>
                        <td style='padding:0 30px 24px;'>

                            <table role='presentation' width='100%'
                                   cellpadding='0' cellspacing='0' border='0'
                                   style='background-color:#fff5f5;
                                          border:1px solid #ffd9de;
                                          border-radius:8px;'>
                                <tr>
                                    <td align='center'
                                        style='padding:16px;'>

                                        <p style='margin:0;
                                                  font-size:13px;
                                                  line-height:1.8;
                                                  color:#555555;'>
                                            <strong style='color:#d70018;'>
                                                &#9200; Lưu ý về thời hạn
                                            </strong>
                                            <br>
                                            Liên kết đặt lại mật khẩu có hiệu lực
                                            trong
                                            <strong style='color:#d70018;'>
                                                24 giờ
                                            </strong>.
                                        </p>

                                    </td>
                                </tr>
                            </table>

                        </td>
                    </tr>

                    <!-- SECURITY NOTICE -->
                    <tr>
                        <td style='padding:0 30px 28px;'>

                            <table role='presentation' width='100%'
                                   cellpadding='0' cellspacing='0' border='0'
                                   style='background-color:#f8f8f8;
                                          border-left:4px solid #d70018;'>
                                <tr>
                                    <td style='padding:14px 16px;
                                               font-size:13px;
                                               line-height:1.8;
                                               color:#555555;'>

                                        <strong style='color:#333333;'>
                                            Thông báo bảo mật
                                        </strong>
                                        <br>
                                        Nếu bạn không yêu cầu đặt lại mật khẩu,
                                        vui lòng bỏ qua email này.
                                        Mật khẩu hiện tại của bạn sẽ không
                                        thay đổi chỉ vì email này được gửi đi.
                                    </td>
                                </tr>
                            </table>

                            <p style='margin:22px 0 0;
                                      font-size:13px;
                                      line-height:1.8;
                                      color:#666666;'>
                                Trân trọng,<br>
                                <strong style='color:#d70018;'>
                                    Đội ngũ The Chill Shop
                                </strong>
                            </p>

                        </td>
                    </tr>

                    <!-- FOOTER -->
                    <tr>
                        <td align='center'
                            style='background-color:#f8f8f8;
                                   border-top:1px solid #eeeeee;
                                   padding:22px 20px;'>

                            <p style='margin:0 0 8px;
                                      font-size:13px;
                                      font-weight:bold;
                                      color:#d70018;'>
                                THE CHILL SHOP
                            </p>

                            <p style='margin:0;
                                      font-size:12px;
                                      line-height:1.8;
                                      color:#888888;'>
                                Email được gửi tự động liên quan đến
                                bảo mật tài khoản.
                                <br>
                                Vui lòng không trả lời trực tiếp email này.
                            </p>

                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>"
);
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

        // GET: /Account/ResetPassword ( của Khôi )
        /*[AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            if (code == null)
            {
                return HttpNotFound();
            }
            return View();
        }*/

        // GET: /Account/ResetPassword ( Của Phúc )
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            if (code == null)
            {
                return HttpNotFound();
            }

            var model = new ResetPasswordViewModel
            {
                Code = code
            };

            return View(model);
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
        //  AJAX LOGIN / REGISTER (cho Modal trên trang Index)
        // ═══════════════════════════════════════════════════

        // POST: /Account/AjaxLogin
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> AjaxLogin(string email, string password, bool rememberMe = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ email và mật khẩu." });

                var user = await UserManager.FindByEmailAsync(email);
                if (user != null && !user.IsActive)
                    return Json(new { success = false, message = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên." });

                var result = await SignInManager.PasswordSignInAsync(email, password, rememberMe, shouldLockout: true);

                switch (result)
                {
                    case SignInStatus.Success:
                        var isAdmin = user != null && (await UserManager.IsInRoleAsync(user.Id, "Administrator") || await UserManager.IsInRoleAsync(user.Id, "StoreManager"));
                        return Json(new { success = true, message = "Đăng nhập thành công!", isAdmin = isAdmin, fullName = user?.FullName ?? "" });

                    case SignInStatus.LockedOut:
                        return Json(new { success = false, message = "Tài khoản bị tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau." });

                    default:
                        return Json(new { success = false, message = "Email hoặc mật khẩu không chính xác." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AjaxLogin Error] {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }

        // POST: /Account/AjaxRegister
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> AjaxRegister(string fullName, string email, string password, string confirmPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin." });

                if (password != confirmPassword)
                    return Json(new { success = false, message = "Mật khẩu xác nhận không khớp." });

                if (password.Length < 6)
                    return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự." });

                var user = new ApplicationUser
                {
                    UserName  = email,
                    Email     = email,
                    FullName  = fullName.Trim(),
                    IsActive  = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await UserManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await UserManager.AddToRoleAsync(user.Id, "Customer");
                    return Json(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập." });
                }

                var errors = string.Join(" ", result.Errors);
                return Json(new { success = false, message = errors });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AjaxRegister Error] {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
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