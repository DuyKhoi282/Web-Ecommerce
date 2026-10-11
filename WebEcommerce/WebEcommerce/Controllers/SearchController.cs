using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: Search?keyword=...&categoryId=...&minPrice=...&maxPrice=...&minRating=...&sortBy=...&page=1
        public async Task<ActionResult> Index(string keyword, int? categoryId, decimal? minPrice, decimal? maxPrice,
            int? minRating, string sortBy, int page = 1)
        {
            ViewBag.Title = string.IsNullOrWhiteSpace(keyword)
                ? "Tìm kiếm & Lọc Sản phẩm"
                : $"Kết quả tìm kiếm: \"{keyword}\"";

            try
            {
                int pageSize = 12;
                var query = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews)
                    .Where(p => p.Status != 3) // Không hiển thị sản phẩm Ngừng bán
                    .AsQueryable();

                // Tìm kiếm từ khóa (full-text search trên Name và Description)
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.ToLower().Trim();
                    query = query.Where(p => p.Name.ToLower().Contains(kw) ||
                                             (p.Description != null && p.Description.ToLower().Contains(kw)) ||
                                             (p.Category != null && p.Category.Name.ToLower().Contains(kw)));
                }

                // Lọc theo danh mục
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(p => p.CategoryID == categoryId.Value);
                }

                // Lọc theo khoảng giá
                if (minPrice.HasValue)
                {
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
                }
                if (maxPrice.HasValue)
                {
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
                }

                // Lọc theo xếp hạng sao trung bình
                if (minRating.HasValue && minRating.Value > 0)
                {
                    query = query.Where(p => p.Reviews.Any() &&
                                             p.Reviews.Average(r => r.Rating) >= minRating.Value);
                }

                // Sắp xếp
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
                    case "rating_desc":
                        query = query.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0);
                        break;
                    case "newest":
                        query = query.OrderByDescending(p => p.CreatedAt);
                        break;
                    case "popular":
                        query = query.OrderByDescending(p => p.ViewCount);
                        break;
                    default:
                        query = query.OrderByDescending(p => p.CreatedAt);
                        break;
                }

                int totalCount = await query.CountAsync();
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var model = new ProductSearchViewModel
                {
                    Products = products.Select(p => new ProductItemViewModel
                    {
                        ProductID = p.ProductID,
                        Name = p.Name,
                        CategoryName = p.Category?.Name,
                        CategoryID = p.CategoryID,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        StockQuantity = p.StockQuantity,
                        Status = p.Status,
                        StatusText = p.Status == 1 ? "Còn hàng" : (p.Status == 2 ? "Hết hàng" : "Ngừng bán"),
                        ViewCount = p.ViewCount,
                        CreatedAt = p.CreatedAt,
                        MainImageUrl = p.ProductImages.FirstOrDefault(i => i.IsMain) != null
                            ? p.ProductImages.FirstOrDefault(i => i.IsMain).ImageURL
                            : (p.ProductImages.FirstOrDefault() != null ? p.ProductImages.FirstOrDefault().ImageURL : null),
                        ImageCount = p.ProductImages.Count,
                        AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                        ReviewCount = p.Reviews.Count
                    }).ToList(),
                    Keyword = keyword,
                    CategoryID = categoryId,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    MinRating = minRating,
                    SortBy = sortBy,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount,
                    CategoryList = await GetCategorySelectListAsync()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi tìm kiếm: " + ex.Message;
                return View(new ProductSearchViewModel
                {
                    CategoryList = new List<SelectListItem>()
                });
            }
        }

        // GET: Search/Autocomplete?term=...
        // API trả về JSON cho tính năng Keyword autocomplete
        public async Task<ActionResult> Autocomplete(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            try
            {
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
