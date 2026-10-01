using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using WebEcommerce.Models;
using WebEcommerce.Filters;

namespace WebEcommerce.Controllers
{
    [CustomAuthorize(Roles = "Administrator")]
    public class AdminUserController : Controller
    {
        private ApplicationUserManager _userManager;
        private ApplicationDbContext _context;

        public AdminUserController()
        {
            _context = new ApplicationDbContext();
        }

        public AdminUserController(ApplicationUserManager userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public ApplicationUserManager UserManager
        {
            get => _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            private set => _userManager = value;
        }

        // GET: AdminUser
        public async Task<ActionResult> Index(string search, string role, string status, int page = 1)
        {
            ViewBag.Title = "Quản lý tài khoản người dùng";
            int pageSize = 10;
            if (page < 1) page = 1;

            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(_context));
            var roles = await roleManager.Roles.ToListAsync();
            var roleDict = roles.ToDictionary(r => r.Id, r => r.Name);

            var now = DateTime.UtcNow;

            // KPI Metrics
            var allUsersQuery = _context.Users.AsNoTracking();
            var totalUsers = await allUsersQuery.CountAsync();
            var totalLocked = await allUsersQuery.CountAsync(u => !u.IsActive || (u.LockoutEndDateUtc != null && u.LockoutEndDateUtc > now));

            // Roles counts
            var adminRoleId = roles.FirstOrDefault(r => r.Name == "Administrator")?.Id;
            var managerRoleId = roles.FirstOrDefault(r => r.Name == "StoreManager")?.Id;
            var customerRoleId = roles.FirstOrDefault(r => r.Name == "Customer")?.Id;

            int totalCustomers = customerRoleId != null
                ? await allUsersQuery.CountAsync(u => u.Roles.Any(r => r.RoleId == customerRoleId))
                : 0;

            int totalStaff = 0;
            if (adminRoleId != null)
                totalStaff += await allUsersQuery.CountAsync(u => u.Roles.Any(r => r.RoleId == adminRoleId));
            if (managerRoleId != null)
                totalStaff += await allUsersQuery.CountAsync(u => u.Roles.Any(r => r.RoleId == managerRoleId));

            // Query with filter
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(s)) ||
                    (u.Email != null && u.Email.ToLower().Contains(s)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(s))
                );
            }

            if (!string.IsNullOrWhiteSpace(role) && role != "All")
            {
                var targetRole = roles.FirstOrDefault(r => r.Name == role);
                if (targetRole != null)
                {
                    query = query.Where(u => u.Roles.Any(r => r.RoleId == targetRole.Id));
                }
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                if (status == "Active")
                {
                    query = query.Where(u => u.IsActive && (u.LockoutEndDateUtc == null || u.LockoutEndDateUtc <= now));
                }
                else if (status == "Locked")
                {
                    query = query.Where(u => !u.IsActive || (u.LockoutEndDateUtc != null && u.LockoutEndDateUtc > now));
                }
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userItems = new List<AdminUserItemViewModel>();
            foreach (var u in users)
            {
                var userRoleId = u.Roles.FirstOrDefault()?.RoleId;
                var roleName = (userRoleId != null && roleDict.ContainsKey(userRoleId))
                    ? roleDict[userRoleId]
                    : "Customer";

                bool isLocked = !u.IsActive || (u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc.Value > now);

                userItems.Add(new AdminUserItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName ?? "Chưa đặt tên",
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber ?? "—",
                    Avatar = u.Avatar,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive,
                    IsLockedOut = isLocked,
                    RoleName = roleName
                });
            }

            var viewModel = new AdminUserListViewModel
            {
                Users = userItems,
                SearchTerm = search,
                SelectedRole = role ?? "All",
                SelectedStatus = status ?? "All",
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalCount = totalCount,
                TotalUsers = totalUsers,
                TotalCustomers = totalCustomers,
                TotalStaff = totalStaff,
                TotalLocked = totalLocked
            };

            return View(viewModel);
        }

        // GET: AdminUser/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction("Index");

            try
            {
                var user = await UserManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng trong hệ thống.";
                    return RedirectToAction("Index");
                }

                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(_context));
                var roles = await roleManager.Roles.ToListAsync();
                var roleDict = roles.ToDictionary(r => r.Id, r => r.Name);

                var userRoleId = user.Roles.FirstOrDefault()?.RoleId;
                var roleName = (userRoleId != null && roleDict.ContainsKey(userRoleId))
                    ? roleDict[userRoleId]
                    : "Customer";

                var now = DateTime.UtcNow;
                bool isLocked = !user.IsActive || (user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc.Value > now);

                // Load order statistics
                var orders = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.UserID == id)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                var recentOrders = orders.Take(5).Select(o => new AdminUserOrderItemViewModel
                {
                    OrderID       = o.OrderID,
                    OrderDate     = o.OrderDate,
                    FinalAmount   = o.FinalAmount,
                    OrderStatus   = o.Status,
                    PaymentMethod = o.PaymentMethod
                }).ToList();

                var viewModel = new AdminUserDetailViewModel
                {
                    Id              = user.Id,
                    FullName        = user.FullName ?? "Chưa đặt tên",
                    Email           = user.Email,
                    PhoneNumber     = user.PhoneNumber ?? "—",
                    Address         = user.Address ?? "Chưa cập nhật",
                    Avatar          = user.Avatar,
                    CreatedAt       = user.CreatedAt,
                    IsActive        = user.IsActive,
                    IsLockedOut     = isLocked,
                    RoleName        = roleName,
                    TotalOrders     = orders.Count,
                    TotalSpent      = orders.Where(o => o.Status == "Delivered").Sum(o => o.FinalAmount),
                    PendingOrders   = orders.Count(o => o.Status == "Pending" || o.Status == "Confirmed" || o.Status == "Processing"),
                    CompletedOrders = orders.Count(o => o.Status == "Delivered"),
                    CancelledOrders = orders.Count(o => o.Status == "Cancelled"),
                    RecentOrders    = recentOrders
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminUser/Details Error] {ex.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải thông tin người dùng.";
                return RedirectToAction("Index");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "Administrator")]
        public async Task<ActionResult> ToggleLock(string userId)
        {
            try
            {
                var currentUserId = User.Identity.GetUserId();
                if (userId == currentUserId)
                {
                    TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản của chính mình.";
                    return RedirectToAction("Index");
                }

                var user = await UserManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng trong hệ thống.";
                    return RedirectToAction("Index");
                }

                // KHÓA CỨNG: Bảo vệ tài khoản Super Admin gốc, không thể bị khóa
                if (string.Equals(user.Email, "admin@thechillshop.vn", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Tài khoản Quản trị viên tối cao (admin@thechillshop.vn) là tài khoản gốc được bảo vệ, không thể bị khóa.";
                    return RedirectToAction("Index");
                }

                var now = DateTime.UtcNow;
                bool isCurrentlyLocked = !user.IsActive || (user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc.Value > now);

                if (isCurrentlyLocked)
                {
                    // Mở khóa
                    user.IsActive = true;
                    user.LockoutEndDateUtc = null;
                    await UserManager.UpdateAsync(user); // Lưu IsActive + LockoutEndDateUtc về DB
                    await UserManager.SetLockoutEndDateAsync(userId, DateTimeOffset.MinValue);
                    await UserManager.ResetAccessFailedCountAsync(userId);
                    TempData["SuccessMessage"] = $"Đã mở khóa tài khoản cho người dùng {user.FullName ?? user.Email}.";
                }
                else
                {
                    // Khóa tài khoản
                    user.IsActive = false;
                    await UserManager.UpdateAsync(user); // Lưu IsActive về DB
                    await UserManager.SetLockoutEnabledAsync(userId, true);
                    await UserManager.SetLockoutEndDateAsync(userId, DateTimeOffset.UtcNow.AddYears(100));
                    TempData["SuccessMessage"] = $"Đã khóa tài khoản người dùng {user.FullName ?? user.Email}.";
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi thay đổi trạng thái tài khoản: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: AdminUser/ChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "Administrator")]
        public async Task<ActionResult> ChangeRole(string userId, string newRole)
        {
            try
            {
                var currentUserId = User.Identity.GetUserId();
                if (userId == currentUserId)
                {
                    TempData["ErrorMessage"] = "Bạn không thể tự thay đổi vai trò của chính mình.";
                    return RedirectToAction("Index");
                }

                var validRoles = new[] { "Customer", "StoreManager", "Administrator" };
                if (!validRoles.Contains(newRole))
                {
                    TempData["ErrorMessage"] = "Vai trò không hợp lệ.";
                    return RedirectToAction("Index");
                }

                var user = await UserManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction("Index");
                }

                // KHÓA CỨNG: Bảo vệ tài khoản Super Admin gốc, không thể bị thay đổi vai trò
                if (string.Equals(user.Email, "admin@thechillshop.vn", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Không thể thay đổi vai trò của Quản trị viên tối cao (admin@thechillshop.vn).";
                    return RedirectToAction("Index");
                }

                var currentRoles = await UserManager.GetRolesAsync(userId);
                if (currentRoles.Any())
                {
                    await UserManager.RemoveFromRolesAsync(userId, currentRoles.ToArray());
                }

                await UserManager.AddToRoleAsync(userId, newRole);
                TempData["SuccessMessage"] = $"Đã cập nhật vai trò của {user.FullName ?? user.Email} thành '{newRole}'.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi phân quyền người dùng: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
                _userManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
