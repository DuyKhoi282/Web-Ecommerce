using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

namespace WebEcommerce.Models
{
    // ──────────────────────────────────────────────────────────────────────
    //  T05 — ĐỔI MẬT KHẨU
    // ──────────────────────────────────────────────────────────────────────
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  T06 — HỒ SƠ CÁ NHÂN (Profile)
    // ──────────────────────────────────────────────────────────────────────
    public class ProfileViewModel
    {
        public string Email { get; set; }           // Chỉ đọc — không được đổi
        public string Avatar { get; set; }          // URL ảnh hiện tại

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2 đến 150 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [StringLength(300, ErrorMessage = "Địa chỉ không được vượt quá 300 ký tự.")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        // File upload — không required (không bắt buộc đổi ảnh mỗi lần update)
        [Display(Name = "Ảnh đại diện mới")]
        public HttpPostedFileBase AvatarFile { get; set; }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Các ViewModels phụ (giữ lại cho các tính năng sẵn có)
    // ──────────────────────────────────────────────────────────────────────
    public class IndexViewModel
    {
        public bool HasPassword { get; set; }
        public IList<UserLoginInfo> Logins { get; set; }
        public string PhoneNumber { get; set; }
        public bool TwoFactor { get; set; }
        public bool BrowserRemembered { get; set; }
    }

    public class ManageLoginsViewModel
    {
        public IList<UserLoginInfo> CurrentLogins { get; set; }
        public IList<AuthenticationDescription> OtherLogins { get; set; }
    }

    public class FactorViewModel
    {
        public string Purpose { get; set; }
    }

    public class SetPasswordViewModel
    {
        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }

    public class AddPhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string Number { get; set; }
    }

    public class VerifyPhoneNumberViewModel
    {
        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }

    public class ConfigureTwoFactorViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
    }
}