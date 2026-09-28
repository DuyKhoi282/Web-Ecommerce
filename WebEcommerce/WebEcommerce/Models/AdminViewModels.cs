using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebEcommerce.Models
{
    public class AdminUserItemViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Avatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsLockedOut { get; set; }
        public string RoleName { get; set; }
    }

    public class AdminUserListViewModel
    {
        public List<AdminUserItemViewModel> Users { get; set; } = new List<AdminUserItemViewModel>();
        public string SearchTerm { get; set; }
        public string SelectedRole { get; set; }
        public string SelectedStatus { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        // Metrics KPI
        public int TotalUsers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalStaff { get; set; }
        public int TotalLocked { get; set; }
    }

    public class ChangeUserRoleViewModel
    {
        [Required]
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string CurrentRole { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò mới")]
        public string NewRole { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }

        public List<RecentOrderItemViewModel> RecentOrders { get; set; } = new List<RecentOrderItemViewModel>();
        public List<TopProductItemViewModel> TopProducts { get; set; } = new List<TopProductItemViewModel>();

        // Chart data
        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<decimal> ChartRevenueData { get; set; } = new List<decimal>();
    }

    public class RecentOrderItemViewModel
    {
        public int OrderID { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal FinalAmount { get; set; }
        public string OrderStatus { get; set; }
    }

    public class TopProductItemViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int SoldQuantity { get; set; }
        public string ImageUrl { get; set; }
    }
}
