using System.ComponentModel.DataAnnotations;

namespace WebEcommerce.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [StringLength(300, ErrorMessage = "Địa chỉ không được vượt quá 300 ký tự.")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại người nhận.")]
        [StringLength(50, ErrorMessage = "Số điện thoại không hợp lệ.")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại người nhận")]
        public string ReceiverPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "COD";

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string Notes { get; set; }

        [StringLength(50)]
        [Display(Name = "Mã giảm giá")]
        public string PromoCode { get; set; }
    }
}
