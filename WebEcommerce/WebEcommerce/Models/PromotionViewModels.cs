using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebEcommerce.Models;

namespace WebEcommerce.Models
{
    public class PromotionListViewModel
    {
        public List<PromotionalCode> Promotions { get; set; } = new List<PromotionalCode>();
        public string SearchTerm { get; set; }
        public string StatusFilter { get; set; } // "All", "Active", "Inactive", "Expired"
        public string TypeFilter { get; set; }   // "All", "Percentage", "Fixed"

        // Thống kê KPI
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InDateActiveCount { get; set; }
        public int ExpiredCount { get; set; }
        public int TotalUsageCount { get; set; }
    }

    public class PromotionFormViewModel
    {
        public int PromoCodeID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã giảm giá.")]
        [StringLength(50, ErrorMessage = "Mã giảm giá tối đa 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9_\-]+$", ErrorMessage = "Mã chỉ được chứa chữ cái, số, dấu gạch nối hoặc gạch dưới (viết liền không dấu).")]
        [Display(Name = "Mã voucher")]
        public string Code { get; set; }

        [Display(Name = "Mô tả ưu đãi")]
        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại giảm giá.")]
        [Display(Name = "Loại giảm giá")]
        public string DiscountType { get; set; } = "Percentage"; // "Percentage" hoặc "Fixed"

        [Required(ErrorMessage = "Vui lòng nhập giá trị giảm.")]
        [Range(0.01, 100000000, ErrorMessage = "Giá trị giảm không hợp lệ.")]
        [Display(Name = "Giá trị giảm")]
        public decimal DiscountValue { get; set; }

        [Range(0, 100000000, ErrorMessage = "Đơn hàng tối thiểu không hợp lệ.")]
        [Display(Name = "Đơn hàng tối thiểu (VNĐ)")]
        public decimal MinOrderAmount { get; set; } = 0;

        [Range(1, 1000000, ErrorMessage = "Số lượt dùng phải từ 1 trở lên.")]
        [Display(Name = "Số lượt dùng tối đa")]
        public int MaxUsage { get; set; } = 100;

        [Display(Name = "Lượt đã dùng")]
        public int CurrentUsage { get; set; } = 0;

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.DateTime)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Kích hoạt sử dụng")]
        public bool IsActive { get; set; } = true;

        // Tiện ích kiểm tra logic
        public bool IsPercentage => DiscountType == "Percentage";
        public bool IsExpired => EndDate < DateTime.UtcNow;
    }

    public class PromotionDetailsViewModel
    {
        public PromotionalCode Promotion { get; set; }
        public List<Order> RelatedOrders { get; set; } = new List<Order>();
        public decimal TotalDiscountGiven { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
    }
}
