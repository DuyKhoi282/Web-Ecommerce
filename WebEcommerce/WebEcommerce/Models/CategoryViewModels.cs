using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace WebEcommerce.Models
{
    // ViewModel hiển thị danh sách danh mục (Index)
    public class CategoryListViewModel
    {
        public List<CategoryItemViewModel> Categories { get; set; } = new List<CategoryItemViewModel>();
        public string SearchTerm { get; set; }
        public int TotalCount { get; set; }
    }

    // ViewModel hiển thị một danh mục trong danh sách
    public class CategoryItemViewModel
    {
        public int CategoryID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageURL { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public string ParentCategoryName { get; set; }
        public int? ParentCategoryID { get; set; }
        public int ProductCount { get; set; }
        public int SubCategoryCount { get; set; }

        // Cấu trúc cha-con
        public int Level { get; set; } = 0;
        public List<CategoryItemViewModel> Children { get; set; } = new List<CategoryItemViewModel>();
    }

    // ViewModel cho Tạo mới / Chỉnh sửa danh mục
    public class CategoryFormViewModel
    {
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        [StringLength(150, ErrorMessage = "Tên danh mục tối đa 150 ký tự")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [StringLength(300, ErrorMessage = "URL hình ảnh tối đa 300 ký tự")]
        [Display(Name = "URL hình ảnh")]
        public string ImageURL { get; set; }

        [Display(Name = "Danh mục cha")]
        public int? ParentCategoryID { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        [Range(0, 9999, ErrorMessage = "Thứ tự hiển thị từ 0 đến 9999")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Hiển thị")]
        public bool IsActive { get; set; } = true;

        // Dropdown danh mục cha
        public IEnumerable<SelectListItem> ParentCategoryList { get; set; }
    }
}
