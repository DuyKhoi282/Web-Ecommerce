using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using MimeKit;
using System;
using System.Configuration;
using System.Threading.Tasks;
using WebEcommerce.Models;

namespace WebEcommerce
{
    // ──────────────────────────────────────────────
    //  Email Service (mock – log ra console)
    // ──────────────────────────────────────────────
    public class EmailService : IIdentityMessageService
    {
        public async Task SendAsync(IdentityMessage message)
        {
            // TODO: Tích hợp MailKit khi đến Task của Thành viên 4
            // Hiện tại: ghi log ra Debug để test
            //System.Diagnostics.Debug.WriteLine(
            //$"[EMAIL] To: {message.Destination} | Subject: {message.Subject} | Body: {message.Body}");
            //return Task.FromResult(0);

            // Ở trên là của Khôi, dưới đây là Phúc làm 
            var host = ConfigurationManager.AppSettings["SmtpHost"];
            var port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
            var username = ConfigurationManager.AppSettings["SmtpUsername"];
            var password = ConfigurationManager.AppSettings["SmtpPassword"];
            var fromEmail = ConfigurationManager.AppSettings["SmtpFromEmail"];
            var fromName = ConfigurationManager.AppSettings["SmtpFromName"];

            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(fromName, fromEmail));
            email.To.Add(new MailboxAddress("", message.Destination));
            email.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = message.Body
            };

            email.Body = bodyBuilder.ToMessageBody();

            using (var smtp = new SmtpClient())
            {
                await smtp.ConnectAsync(
                    host,
                    port,
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    username,
                    password);

                await smtp.SendAsync(email);

                await smtp.DisconnectAsync(true);
            }
        }
    }

    public class SmsService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            return Task.FromResult(0);
        }
    }

    // ──────────────────────────────────────────────
    //  ApplicationUserManager
    // ──────────────────────────────────────────────
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(
            IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
        {
            var manager = new ApplicationUserManager(
                new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));

            // ── Quy tắc UserName ──────────────────────────────
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true          // Email phải duy nhất
            };

            // ── Quy tắc mật khẩu ─────────────────────────────
            // Nghiệp vụ: Tối thiểu 6 ký tự, có chữ hoa, chữ thường, số, ký tự đặc biệt
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = false,   // Giảm bớt để UX không khó quá
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = false,
            };

            // ── Chống Brute-Force: Lockout ────────────────────
            // Nghiệp vụ: Sai 5 lần → khóa 10 phút
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(10);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // ── Token Provider cho Forgot Password ────────────
            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();

            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider =
                    new DataProtectorTokenProvider<ApplicationUser>(
                        dataProtectionProvider.Create("ASP.NET Identity"))
                    {
                        TokenLifespan = TimeSpan.FromHours(24) // Link reset mật khẩu hết hạn sau 24h
                    };
            }
            return manager;
        }
    }

    // ──────────────────────────────────────────────
    //  ApplicationSignInManager
    // ──────────────────────────────────────────────
    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(
            ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<System.Security.Claims.ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(
            IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(
                context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }
}
