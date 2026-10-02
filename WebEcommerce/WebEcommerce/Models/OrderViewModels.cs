using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WebEcommerce.Models
{
    public class CheckoutViewModel
    {
        public Cart Cart { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [StringLength(300, ErrorMessage = "Địa chỉ không được vượt quá 300 ký tự")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(50)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string ReceiverPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [StringLength(50)]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "COD";

        [StringLength(500)]
        [Display(Name = "Ghi chú đơn hàng")]
        public string Notes { get; set; }
    }
}
