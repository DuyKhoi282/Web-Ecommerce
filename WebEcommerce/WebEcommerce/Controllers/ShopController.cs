using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    /// <summary>
    /// Trang Sản phẩm phía Khách hàng (Storefront).
    /// Hiển thị danh sách sản phẩm, chi tiết, tìm kiếm/lọc với phân trang.
    /// Tất cả Action đều bọc try-catch.
    /// </summary>
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopController()
        {
            _context = new ApplicationDbContext();
        }

        // =========================================
        // GET: Shop (Trang danh sách sản phẩm)
        // URL: /Shop?keyword=...&categoryId=...&minPrice=...&maxPrice=...&minRating=...&sortBy=...&page=1
        // =========================================
        public async Task<ActionResult> Index(string keyword, int? categoryId, decimal? minPrice, decimal? maxPrice,
            int? minRating, string sortBy, int page = 1)
        {
            ViewBag.Title = string.IsNullOrWhiteSpace(keyword) ? "Sản phẩm" : $"Tìm kiếm: \"{keyword}\"";

            try
            {
                int pageSize = 12;
                var query = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews)
                    .Where(p => p.Status != 3) // Ẩn sản phẩm Ngừng bán
                    .AsQueryable();

                // === BỘ LỌC ===

                // 1. Tìm kiếm theo từ khóa (full-text trên Name + Description)
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.ToLower().Trim();
                    query = query.Where(p => p.Name.ToLower().Contains(kw) ||
                                             (p.Description != null && p.Description.ToLower().Contains(kw)));
                }

                // 2. Lọc theo danh mục
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(p => p.CategoryID == categoryId.Value);
                }

                // 3. Lọc theo khoảng giá
                if (minPrice.HasValue)
                {
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
                }
                if (maxPrice.HasValue)
                {
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
                }

                // 4. Lọc theo xếp hạng sao trung bình
                if (minRating.HasValue && minRating.Value > 0)
                {
                    query = query.Where(p => p.Reviews.Any() &&
                                             p.Reviews.Average(r => r.Rating) >= minRating.Value);
                }

                // === SẮP XẾP ===
                switch (sortBy)
                {
                    case "price_asc":
                        query = query.OrderBy(p => p.DiscountPrice ?? p.Price);
                        break;
                    case "price_desc":
                        query = query.OrderByDescending(p => p.DiscountPrice ?? p.Price);
                        break;
                    case "name_asc":
                        query = query.OrderBy(p => p.Name);
                        break;
                    case "name_desc":
                        query = query.OrderByDescending(p => p.Name);
                        break;
                    case "rating":
                        query = query.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0);
                        break;
                    case "popular":
                        query = query.OrderByDescending(p => p.ViewCount);
                        break;
                    case "oldest":
                        query = query.OrderBy(p => p.CreatedAt);
                        break;
                    default: // "newest"
                        query = query.OrderByDescending(p => p.CreatedAt);
                        break;
                }

                // === PHÂN TRANG ===
                int totalCount = await query.CountAsync();
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Lấy danh mục cho sidebar lọc
                var categories = await _context.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .Select(c => new CategoryItemViewModel
                    {
                        CategoryID = c.CategoryID,
                        Name = c.Name,
                        ProductCount = c.Products.Count(p => p.Status != 3),
                        ParentCategoryID = c.ParentCategoryID
                    })
                    .ToListAsync();

                // ViewModel
                ViewBag.Products = products;
                ViewBag.Categories = categories;
                ViewBag.Keyword = keyword;
                ViewBag.SelectedCategoryID = categoryId;
                ViewBag.MinPrice = minPrice;
                ViewBag.MaxPrice = maxPrice;
                ViewBag.MinRating = minRating;
                ViewBag.SortBy = sortBy ?? "newest";
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalCount = totalCount;
                ViewBag.PageSize = pageSize;

                return View(products);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi tải sản phẩm: " + ex.Message;
                ViewBag.Products = new List<Product>();
                ViewBag.Categories = new List<CategoryItemViewModel>();
                ViewBag.TotalCount = 0;
                ViewBag.TotalPages = 0;
                ViewBag.CurrentPage = 1;
                return View(new List<Product>());
            }
        }

        // =========================================
        // GET: Shop/Details/5 (Trang chi tiết sản phẩm)
        // =========================================
        public async Task<ActionResult> Details(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews.Select(r => r.User))
                    .FirstOrDefaultAsync(p => p.ProductID == id && p.Status != 3);

                if (product == null)
                {
                    return RedirectToAction("Index");
                }

                // Tăng lượt xem
                try
                {
                    var productToUpdate = await _context.Products.FindAsync(id);
                    if (productToUpdate != null)
                    {
                        productToUpdate.ViewCount++;
                        await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    // Bỏ qua lỗi tăng lượt xem
                }

                ViewBag.Title = product.Name;

                // Sản phẩm liên quan (cùng danh mục)
                List<Product> relatedProducts;
                try
                {
                    relatedProducts = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.Category)
                        .Include(p => p.ProductImages)
                        .Include(p => p.Reviews)
                        .Where(p => p.CategoryID == product.CategoryID &&
                                    p.ProductID != product.ProductID &&
                                    p.Status != 3)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(4)
                        .ToListAsync();
                }
                catch
                {
                    relatedProducts = new List<Product>();
                }

                ViewBag.RelatedProducts = relatedProducts;

                return View(product);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi tải sản phẩm: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // =========================================
        // GET: Shop/Category/5 (Sản phẩm theo danh mục)
        // =========================================
        public async Task<ActionResult> Category(int id, string sortBy, int page = 1)
        {
            try
            {
                var category = await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CategoryID == id && c.IsActive);

                if (category == null)
                {
                    return RedirectToAction("Index");
                }

                ViewBag.Title = category.Name;
                ViewBag.CategoryInfo = category;

                // Redirect sang Index với filter
                return RedirectToAction("Index", new { categoryId = id, sortBy = sortBy, page = page });
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // =========================================
        // GET: Shop/Autocomplete?term=... (API Keyword Autocomplete)
        // =========================================
        public async Task<ActionResult> Autocomplete(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                {
                    return Json(new List<object>(), JsonRequestBehavior.AllowGet);
                }

                var kw = term.ToLower().Trim();
                var suggestions = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status != 3 && p.Name.ToLower().Contains(kw))
                    .OrderBy(p => p.Name)
                    .Take(8)
                    .Select(p => new
                    {
                        id = p.ProductID,
                        name = p.Name,
                        category = p.Category != null ? p.Category.Name : "",
                        price = p.DiscountPrice ?? p.Price,
                        originalPrice = p.Price,
                        image = p.ProductImages.FirstOrDefault(i => i.IsMain) != null
                            ? p.ProductImages.FirstOrDefault(i => i.IsMain).ImageURL
                            : (p.ProductImages.FirstOrDefault() != null ? p.ProductImages.FirstOrDefault().ImageURL : null)
                    })
                    .ToListAsync();

                return Json(suggestions, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
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
