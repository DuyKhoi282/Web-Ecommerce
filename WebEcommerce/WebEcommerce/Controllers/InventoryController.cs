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
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: Inventory
        public async Task<ActionResult> Index(string search, string stockFilter, int? categoryId, int page = 1)
        {
            ViewBag.Title = "Quản lý Kho & Tồn kho";

            try
            {
                int pageSize = 20;
                var query = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status != 3) // Bỏ qua sản phẩm Ngừng bán
                    .AsQueryable();

                // Tìm kiếm
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.ToLower();
                    query = query.Where(p => p.Name.ToLower().Contains(s));
                }

                // Lọc theo danh mục
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(p => p.CategoryID == categoryId.Value);
                }

                // Lọc theo mức tồn kho
                switch (stockFilter)
                {
                    case "low":
                        query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity < 5);
                        break;
                    case "out":
                        query = query.Where(p => p.StockQuantity == 0);
                        break;
                    case "ok":
                        query = query.Where(p => p.StockQuantity >= 5);
                        break;
                }

                // Sắp xếp: Tồn kho thấp nhất lên đầu
                query = query.OrderBy(p => p.StockQuantity).ThenBy(p => p.Name);

                int totalCount = await query.CountAsync();
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // KPI tổng thể
                var allProducts = _context.Products.AsNoTracking().Where(p => p.Status != 3);
                int totalProducts = await allProducts.CountAsync();
                int lowStockCount = await allProducts.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity < 5);
                int outOfStockCount = await allProducts.CountAsync(p => p.StockQuantity == 0);
                int normalStockCount = await allProducts.CountAsync(p => p.StockQuantity >= 5);

                var model = new InventoryListViewModel
                {
                    Items = products.Select(p => new InventoryItemViewModel
                    {
                        ProductID = p.ProductID,
                        ProductName = p.Name,
                        CategoryName = p.Category?.Name,
                        MainImageUrl = p.ProductImages.FirstOrDefault(i => i.IsMain) != null
                            ? p.ProductImages.FirstOrDefault(i => i.IsMain).ImageURL
                            : (p.ProductImages.FirstOrDefault() != null ? p.ProductImages.FirstOrDefault().ImageURL : null),
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        Status = p.Status,
                        StatusText = p.Status == 1 ? "Còn hàng" : "Hết hàng",
                        StockLevel = p.StockQuantity == 0 ? "danger" : (p.StockQuantity < 5 ? "warning" : "success")
                    }).ToList(),
                    SearchTerm = search,
                    StockFilter = stockFilter ?? "all",
                    SelectedCategoryID = categoryId,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount,
                    TotalProducts = totalProducts,
                    LowStockCount = lowStockCount,
                    OutOfStockCount = outOfStockCount,
                    NormalStockCount = normalStockCount,
                    CategoryList = await GetCategorySelectListAsync()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi tải dữ liệu kho: " + ex.Message;
                return View(new InventoryListViewModel
                {
                    CategoryList = new List<SelectListItem>()
                });
            }
        }

        // GET: Inventory/UpdateStock/5
        public async Task<ActionResult> UpdateStock(int id)
        {
            ViewBag.Title = "Cập nhật Tồn kho";

            try
            {
                var product = await _context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction("Index");
                }

                var model = new UpdateStockViewModel
                {
                    ProductID = product.ProductID,
                    ProductName = product.Name,
                    CurrentStock = product.StockQuantity,
                    OperationType = "import"
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Inventory/UpdateStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateStock(UpdateStockViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var product = await _context.Products.FindAsync(model.ProductID);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction("Index");
                }

                if (model.QuantityChange <= 0)
                {
                    ModelState.AddModelError("QuantityChange", "Số lượng phải lớn hơn 0.");
                    model.ProductName = product.Name;
                    model.CurrentStock = product.StockQuantity;
                    return View(model);
                }

                if (model.OperationType == "import")
                {
                    product.StockQuantity += model.QuantityChange;
                }
                else if (model.OperationType == "export")
                {
                    if (product.StockQuantity < model.QuantityChange)
                    {
                        ModelState.AddModelError("QuantityChange", $"Số lượng xuất ({model.QuantityChange}) vượt quá tồn kho hiện tại ({product.StockQuantity}).");
                        model.ProductName = product.Name;
                        model.CurrentStock = product.StockQuantity;
                        return View(model);
                    }
                    product.StockQuantity -= model.QuantityChange;
                }

                // Cập nhật trạng thái tự động
                if (product.StockQuantity == 0 && product.Status == 1)
                {
                    product.Status = 2; // Chuyển sang Hết hàng
                }
                else if (product.StockQuantity > 0 && product.Status == 2)
                {
                    product.Status = 1; // Chuyển sang Còn hàng
                }

                await _context.SaveChangesAsync();

                string action = model.OperationType == "import" ? "Nhập" : "Xuất";
                TempData["SuccessMessage"] = $"{action} kho thành công! Sản phẩm \"{product.Name}\": Tồn kho mới = {product.StockQuantity}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi cập nhật tồn kho: " + ex.Message);
                return View(model);
            }
        }

        // GET: Inventory/LowStockAlerts (API trả JSON cho cảnh báo)
        public async Task<ActionResult> LowStockAlerts()
        {
            try
            {
                var lowStockProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Where(p => p.Status != 3 && p.StockQuantity < 5)
                    .OrderBy(p => p.StockQuantity)
                    .Select(p => new
                    {
                        id = p.ProductID,
                        name = p.Name,
                        category = p.Category != null ? p.Category.Name : "",
                        stock = p.StockQuantity,
                        level = p.StockQuantity == 0 ? "danger" : "warning"
                    })
                    .ToListAsync();

                return Json(lowStockProducts, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        private async Task<IEnumerable<SelectListItem>> GetCategorySelectListAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Tất cả danh mục --" }
            };

            items.AddRange(categories.Select(c => new SelectListItem
            {
                Value = c.CategoryID.ToString(),
                Text = c.Name
            }));

            return items;
        }

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
