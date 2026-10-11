using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Filters;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    [CustomAuthorize(Roles = "Administrator,StoreManager")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: Category
        public async Task<ActionResult> Index(string search)
        {
            ViewBag.Title = "Quản lý Danh mục Sản phẩm";

            try
            {
                var allCategories = await _context.Categories
                    .AsNoTracking()
                    .Include(c => c.Products)
                    .Include(c => c.SubCategories)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                // Áp dụng bộ lọc tìm kiếm
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    allCategories = allCategories
                        .Where(c => c.Name.ToLower().Contains(searchLower) ||
                                    (c.Description != null && c.Description.ToLower().Contains(searchLower)))
                        .ToList();
                }

                // Xây dựng cấu trúc cây cha-con
                var tree = BuildCategoryTree(allCategories, null, 0);

                var model = new CategoryListViewModel
                {
                    Categories = tree,
                    SearchTerm = search,
                    TotalCount = allCategories.Count
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi tải danh mục: " + ex.Message;
                return View(new CategoryListViewModel());
            }
        }

        // GET: Category/Create
        public async Task<ActionResult> Create()
        {
            ViewBag.Title = "Thêm Danh mục mới";

            var model = new CategoryFormViewModel
            {
                IsActive = true,
                ParentCategoryList = await GetParentCategoryListAsync(null)
            };

            return View(model);
        }

        // POST: Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ParentCategoryList = await GetParentCategoryListAsync(null);
                return View(model);
            }

            try
            {
                var category = new Category
                {
                    Name = model.Name,
                    Description = model.Description,
                    ImageURL = model.ImageURL,
                    ParentCategoryID = model.ParentCategoryID,
                    DisplayOrder = model.DisplayOrder,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã tạo danh mục \"{category.Name}\" thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi tạo danh mục: " + ex.Message);
                model.ParentCategoryList = await GetParentCategoryListAsync(null);
                return View(model);
            }
        }

        // GET: Category/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            ViewBag.Title = "Chỉnh sửa Danh mục";

            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục.";
                    return RedirectToAction("Index");
                }

                var model = new CategoryFormViewModel
                {
                    CategoryID = category.CategoryID,
                    Name = category.Name,
                    Description = category.Description,
                    ImageURL = category.ImageURL,
                    ParentCategoryID = category.ParentCategoryID,
                    DisplayOrder = category.DisplayOrder,
                    IsActive = category.IsActive,
                    ParentCategoryList = await GetParentCategoryListAsync(category.CategoryID)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(CategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ParentCategoryList = await GetParentCategoryListAsync(model.CategoryID);
                return View(model);
            }

            try
            {
                var category = await _context.Categories.FindAsync(model.CategoryID);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục.";
                    return RedirectToAction("Index");
                }

                // Kiểm tra vòng lặp cha-con
                if (model.ParentCategoryID.HasValue && model.ParentCategoryID.Value == model.CategoryID)
                {
                    ModelState.AddModelError("ParentCategoryID", "Danh mục không thể làm cha của chính nó.");
                    model.ParentCategoryList = await GetParentCategoryListAsync(model.CategoryID);
                    return View(model);
                }

                category.Name = model.Name;
                category.Description = model.Description;
                category.ImageURL = model.ImageURL;
                category.ParentCategoryID = model.ParentCategoryID;
                category.DisplayOrder = model.DisplayOrder;
                category.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã cập nhật danh mục \"{category.Name}\" thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật: " + ex.Message);
                model.ParentCategoryList = await GetParentCategoryListAsync(model.CategoryID);
                return View(model);
            }
        }

        // POST: Category/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ToggleActive(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục." });
                }

                category.IsActive = !category.IsActive;
                await _context.SaveChangesAsync();

                return Json(new { success = true, isActive = category.IsActive, message = category.IsActive ? "Đã hiển thị danh mục." : "Đã ẩn danh mục." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Category/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);

                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục." });
                }

                if (category.Products.Any())
                {
                    return Json(new { success = false, message = $"Không thể xóa danh mục \"{category.Name}\" vì đang có {category.Products.Count} sản phẩm." });
                }

                if (category.SubCategories.Any())
                {
                    return Json(new { success = false, message = $"Không thể xóa danh mục \"{category.Name}\" vì đang có {category.SubCategories.Count} danh mục con." });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Đã xóa danh mục \"{category.Name}\" thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        #region Helper Methods

        private List<CategoryItemViewModel> BuildCategoryTree(List<Category> allCategories, int? parentId, int level)
        {
            return allCategories
                .Where(c => c.ParentCategoryID == parentId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryItemViewModel
                {
                    CategoryID = c.CategoryID,
                    Name = c.Name,
                    Description = c.Description,
                    ImageURL = c.ImageURL,
                    DisplayOrder = c.DisplayOrder,
                    IsActive = c.IsActive,
                    ParentCategoryID = c.ParentCategoryID,
                    ParentCategoryName = c.ParentCategoryID.HasValue
                        ? allCategories.FirstOrDefault(p => p.CategoryID == c.ParentCategoryID.Value)?.Name
                        : null,
                    ProductCount = c.Products?.Count ?? 0,
                    SubCategoryCount = c.SubCategories?.Count ?? 0,
                    Level = level,
                    Children = BuildCategoryTree(allCategories, c.CategoryID, level + 1)
                })
                .ToList();
        }

        private async Task<IEnumerable<SelectListItem>> GetParentCategoryListAsync(int? excludeId)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => !excludeId.HasValue || c.CategoryID != excludeId.Value)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Không có (Danh mục gốc) --" }
            };

            items.AddRange(categories.Select(c => new SelectListItem
            {
                Value = c.CategoryID.ToString(),
                Text = c.ParentCategoryID.HasValue ? "   └─ " + c.Name : c.Name
            }));

            return items;
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
