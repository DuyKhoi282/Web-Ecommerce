using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebEcommerce.Models;

namespace WebEcommerce.Models
{
    public class FlashSaleListViewModel
    {
        public List<FlashSale> FlashSales { get; set; } = new List<FlashSale>();
        public string SearchTerm { get; set; }
        public string StatusFilter { get; set; } = "All"; // "All", "Live", "Upcoming", "Ended", "Inactive"

        // Thống kê KPI
        public int TotalCount { get; set; }
        public int LiveCount { get; set; }
        public int UpcomingCount { get; set; }
        public int EndedCount { get; set; }
        public int TotalSoldQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class FlashSaleItemInputModel
    {
        public int FlashSaleItemID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public string CategoryName { get; set; }
        public decimal OriginalPrice { get; set; }
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá Flash Sale.")]
        [Range(1000, 1000000000, ErrorMessage = "Giá Flash Sale tối thiểu 1.000đ.")]
        public decimal FlashSalePrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng mở bán.")]
        [Range(1, 10000, ErrorMessage = "Số lượng mở bán tối thiểu là 1.")]
        public int MaxQuantity { get; set; } = 10;

        public int SoldQuantity { get; set; } = 0;

        public int DiscountPercent => OriginalPrice > 0 && FlashSalePrice < OriginalPrice
            ? (int)Math.Round((OriginalPrice - FlashSalePrice) * 100 / OriginalPrice)
            : 0;
    }

    public class FlashSaleFormViewModel
    {
        public int FlashSaleID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề đợt Flash Sale.")]
        [StringLength(200, ErrorMessage = "Tiêu đề Flash Sale tối đa 200 ký tự.")]
        [Display(Name = "Tiêu đề đợt Flash Sale")]
        public string Title { get; set; }

        [Display(Name = "Mô tả / Thông điệp khuyến mãi")]
        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1.000 ký tự.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
        [Display(Name = "Thời gian bắt đầu")]
        public DateTime StartTime { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
        [Display(Name = "Thời gian kết thúc")]
        public DateTime EndTime { get; set; } = DateTime.Now.AddHours(24);

        [Display(Name = "Kích hoạt sự kiện")]
        public bool IsActive { get; set; } = true;

        // Danh sách sản phẩm tham gia đợt sale
        public List<FlashSaleItemInputModel> Items { get; set; } = new List<FlashSaleItemInputModel>();

        // Danh sách sản phẩm khả dụng cho modal lựa chọn
        public List<Product> AvailableProducts { get; set; } = new List<Product>();
    }

    public class FlashSaleDetailsViewModel
    {
        public FlashSale FlashSale { get; set; }
        public int TotalItems { get; set; }
        public int TotalMaxQuantity { get; set; }
        public int TotalSoldQuantity { get; set; }
        public decimal TotalEstimatedRevenue { get; set; }

        public decimal SoldPercentage => TotalMaxQuantity > 0 
            ? Math.Round((decimal)TotalSoldQuantity * 100 / TotalMaxQuantity, 1) 
            : 0;

        public bool IsLive => FlashSale != null && FlashSale.IsActive && FlashSale.StartTime <= DateTime.UtcNow && FlashSale.EndTime > DateTime.UtcNow;
        public bool IsUpcoming => FlashSale != null && FlashSale.IsActive && FlashSale.StartTime > DateTime.UtcNow;
        public bool IsEnded => FlashSale != null && FlashSale.EndTime <= DateTime.UtcNow;
    }
}
