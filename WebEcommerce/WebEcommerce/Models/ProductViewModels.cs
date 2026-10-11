using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace WebEcommerce.Models
{
    // ==========================================
    // ViewModel danh sách sản phẩm (Index)
    // ==========================================
    public class ProductListViewModel
    {
        public List<ProductItemViewModel> Products { get; set; } = new List<ProductItemViewModel>();
        public string SearchTerm { get; set; }
        public int? SelectedCategoryID { get; set; }
        public int? SelectedStatus { get; set; }
        public string SortBy { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        // Dropdown lọc
        public IEnumerable<SelectListItem> CategoryList { get; set; }
        public IEnumerable<SelectListItem> StatusList { get; set; }

        // KPI
        public int TotalProducts { get; set; }
        public int InStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int DiscontinuedCount { get; set; }
        public int LowStockCount { get; set; }
    }

    // ViewModel hiển thị một sản phẩm trong danh sách
    public class ProductItemViewModel
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public int CategoryID { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
        public int ViewCount { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string MainImageUrl { get; set; }
        public int ImageCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    // ==========================================
    // ViewModel Tạo / Sửa sản phẩm
    // ==========================================
    public class ProductFormViewModel
    {
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tên sản phẩm tối đa 255 ký tự")]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; }

        [Display(Name = "Mô tả")]
        [AllowHtml]
        public string Description { get; set; }

        [Required(ErrorMessage = "Danh mục là bắt buộc")]
        [Display(Name = "Danh mục")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Giá gốc là bắt buộc")]
        [Range(0, 999999999, ErrorMessage = "Giá không hợp lệ")]
        [Display(Name = "Giá gốc")]
        public decimal Price { get; set; }

        [Display(Name = "Giá khuyến mãi")]
        [Range(0, 999999999, ErrorMessage = "Giá khuyến mãi không hợp lệ")]
        public decimal? DiscountPrice { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc")]
        [Range(0, 999999, ErrorMessage = "Số lượng tồn kho không hợp lệ")]
        [Display(Name = "Số lượng tồn kho")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [Display(Name = "Trạng thái")]
        public int Status { get; set; } = 1;

        // Hình ảnh upload
        [Display(Name = "Tải lên hình ảnh")]
        public HttpPostedFileBase[] UploadImages { get; set; }

        // Ảnh hiện tại (khi chỉnh sửa)
        public List<ProductImageItem> ExistingImages { get; set; } = new List<ProductImageItem>();

        // ID ảnh đại diện (IsMain)
        [Display(Name = "Ảnh đại diện")]
        public int? MainImageID { get; set; }

        // Danh sách ID ảnh cần xóa
        public List<int> DeleteImageIDs { get; set; } = new List<int>();

        // Dropdowns
        public IEnumerable<SelectListItem> CategoryList { get; set; }
        public IEnumerable<SelectListItem> StatusList { get; set; }
    }

    // Hiển thị hình ảnh sản phẩm
    public class ProductImageItem
    {
        public int ImageID { get; set; }
        public string ImageURL { get; set; }
        public bool IsMain { get; set; }
        public int DisplayOrder { get; set; }
    }

    // ==========================================
    // ViewModel Chi tiết sản phẩm
    // ==========================================
    public class ProductDetailViewModel
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public int CategoryID { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
        public int ViewCount { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public List<ProductImageItem> Images { get; set; } = new List<ProductImageItem>();
        public List<ProductReviewItem> Reviews { get; set; } = new List<ProductReviewItem>();
    }

    public class ProductReviewItem
    {
        public int ReviewID { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }

    // ==========================================
    // ViewModel Tìm kiếm & Lọc sản phẩm
    // ==========================================
    public class ProductSearchViewModel
    {
        public List<ProductItemViewModel> Products { get; set; } = new List<ProductItemViewModel>();
        public string Keyword { get; set; }
        public int? CategoryID { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinRating { get; set; }
        public string SortBy { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        // Dropdown lọc
        public IEnumerable<SelectListItem> CategoryList { get; set; }
    }

    // ==========================================
    // ViewModel Quản lý Kho & Tồn kho
    // ==========================================
    public class InventoryListViewModel
    {
        public List<InventoryItemViewModel> Items { get; set; } = new List<InventoryItemViewModel>();
        public string SearchTerm { get; set; }
        public string StockFilter { get; set; } // "all", "low", "out", "ok"
        public int? SelectedCategoryID { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        // KPI
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int NormalStockCount { get; set; }

        // Dropdown
        public IEnumerable<SelectListItem> CategoryList { get; set; }
    }

    public class InventoryItemViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string MainImageUrl { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
        public string StockLevel { get; set; } // "danger", "warning", "success"
    }

    // ViewModel cập nhật tồn kho
    public class UpdateStockViewModel
    {
        [Required]
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int CurrentStock { get; set; }

        [Required(ErrorMessage = "Số lượng thay đổi là bắt buộc")]
        [Display(Name = "Số lượng thay đổi")]
        public int QuantityChange { get; set; }

        [Required(ErrorMessage = "Loại thao tác là bắt buộc")]
        [Display(Name = "Loại thao tác")]
        public string OperationType { get; set; } // "import" or "export"

        [Display(Name = "Ghi chú")]
        [StringLength(500)]
        public string Note { get; set; }
    }

    // ViewModel autocomplete search
    public class SearchSuggestionItem
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
    }
}
