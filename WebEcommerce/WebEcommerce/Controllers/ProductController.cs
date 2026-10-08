using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebEcommerce.Filters;
using WebEcommerce.Models;

namespace WebEcommerce.Controllers
{
    [CustomAuthorize(Roles = "Administrator,StoreManager")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: Product
        public async Task<ActionResult> Index(string search, int? categoryId, int? status, string sortBy, int page = 1)
        {
            ViewBag.Title = "Quản lý Sản phẩm";

            try
            {
                int pageSize = 10;
                var query = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews)
                    .AsQueryable();

                // Bộ lọc tìm kiếm
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.ToLower();
                    query = query.Where(p => p.Name.ToLower().Contains(s) ||
                                             (p.Description != null && p.Description.ToLower().Contains(s)));
                }

                // Lọc theo danh mục
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(p => p.CategoryID == categoryId.Value);
                }

                // Lọc theo trạng thái
                if (status.HasValue)
                {
                    query = query.Where(p => p.Status == status.Value);
                }

                // Sắp xếp
                switch (sortBy)
                {
                    case "name_asc":
                        query = query.OrderBy(p => p.Name);
                        break;
                    case "name_desc":
                        query = query.OrderByDescending(p => p.Name);
                        break;
                    case "price_asc":
                        query = query.OrderBy(p => p.Price);
                        break;
                    case "price_desc":
                        query = query.OrderByDescending(p => p.Price);
                        break;
                    case "stock_asc":
                        query = query.OrderBy(p => p.StockQuantity);
                        break;
                    case "stock_desc":
                        query = query.OrderByDescending(p => p.StockQuantity);
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

                // KPI tổng thể (không lọc)
                var allProducts = _context.Products.AsNoTracking();
                int totalProducts = await allProducts.CountAsync();
                int inStock = await allProducts.CountAsync(p => p.Status == 1);
                int outOfStock = await allProducts.CountAsync(p => p.Status == 2);
                int discontinued = await allProducts.CountAsync(p => p.Status == 3);
                int lowStock = await allProducts.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity < 5 && p.Status == 1);

                var model = new ProductListViewModel
                {
                    Products = products.Select(p => MapToProductItem(p)).ToList(),
                    SearchTerm = search,
                    SelectedCategoryID = categoryId,
                    SelectedStatus = status,
                    SortBy = sortBy,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount,
                    TotalProducts = totalProducts,
                    InStockCount = inStock,
                    OutOfStockCount = outOfStock,
                    DiscontinuedCount = discontinued,
                    LowStockCount = lowStock,
                    CategoryList = await GetCategorySelectListAsync(),
                    StatusList = GetStatusSelectList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi tải sản phẩm: " + ex.Message;
                return View(new ProductListViewModel
                {
                    CategoryList = new List<SelectListItem>(),
                    StatusList = new List<SelectListItem>()
                });
            }
        }

        // GET: Product/Create
        public async Task<ActionResult> Create()
        {
            ViewBag.Title = "Thêm Sản phẩm mới";

            var model = new ProductFormViewModel
            {
                Status = 1,
                CategoryList = await GetCategorySelectListAsync(),
                StatusList = GetStatusSelectList()
            };

            return View(model);
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ProductFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CategoryList = await GetCategorySelectListAsync();
                model.StatusList = GetStatusSelectList();
                return View(model);
            }

            try
            {
                // Kiểm tra giá khuyến mãi < giá gốc
                if (model.DiscountPrice.HasValue && model.DiscountPrice.Value >= model.Price)
                {
                    ModelState.AddModelError("DiscountPrice", "Giá khuyến mãi phải nhỏ hơn giá gốc.");
                    model.CategoryList = await GetCategorySelectListAsync();
                    model.StatusList = GetStatusSelectList();
                    return View(model);
                }

                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    CategoryID = model.CategoryID,
                    Price = model.Price,
                    DiscountPrice = model.DiscountPrice,
                    StockQuantity = model.StockQuantity,
                    Status = model.Status,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Upload hình ảnh
                if (model.UploadImages != null && model.UploadImages.Any(f => f != null && f.ContentLength > 0))
                {
                    await SaveProductImagesAsync(product.ProductID, model.UploadImages, model.MainImageID);
                }

                TempData["SuccessMessage"] = $"Đã tạo sản phẩm \"{product.Name}\" thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi tạo sản phẩm: " + ex.Message);
                model.CategoryList = await GetCategorySelectListAsync();
                model.StatusList = GetStatusSelectList();
                return View(model);
            }
        }

        // GET: Product/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            ViewBag.Title = "Chỉnh sửa Sản phẩm";

            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction("Index");
                }

                var model = new ProductFormViewModel
                {
                    ProductID = product.ProductID,
                    Name = product.Name,
                    Description = product.Description,
                    CategoryID = product.CategoryID,
                    Price = product.Price,
                    DiscountPrice = product.DiscountPrice,
                    StockQuantity = product.StockQuantity,
                    Status = product.Status,
                    ExistingImages = product.ProductImages
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => new ProductImageItem
                        {
                            ImageID = i.ImageID,
                            ImageURL = i.ImageURL,
                            IsMain = i.IsMain,
                            DisplayOrder = i.DisplayOrder
                        }).ToList(),
                    MainImageID = product.ProductImages.FirstOrDefault(i => i.IsMain)?.ImageID,
                    CategoryList = await GetCategorySelectListAsync(),
                    StatusList = GetStatusSelectList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ProductFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CategoryList = await GetCategorySelectListAsync();
                model.StatusList = GetStatusSelectList();
                // Tải lại ảnh hiện tại
                var existingImgs = await _context.ProductImages
                    .Where(i => i.ProductID == model.ProductID)
                    .OrderBy(i => i.DisplayOrder)
                    .ToListAsync();
                model.ExistingImages = existingImgs.Select(i => new ProductImageItem
                {
                    ImageID = i.ImageID, ImageURL = i.ImageURL, IsMain = i.IsMain, DisplayOrder = i.DisplayOrder
                }).ToList();
                return View(model);
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.ProductID == model.ProductID);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction("Index");
                }

                // Validate giá khuyến mãi
                if (model.DiscountPrice.HasValue && model.DiscountPrice.Value >= model.Price)
                {
                    ModelState.AddModelError("DiscountPrice", "Giá khuyến mãi phải nhỏ hơn giá gốc.");
                    model.CategoryList = await GetCategorySelectListAsync();
                    model.StatusList = GetStatusSelectList();
                    model.ExistingImages = product.ProductImages
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => new ProductImageItem { ImageID = i.ImageID, ImageURL = i.ImageURL, IsMain = i.IsMain, DisplayOrder = i.DisplayOrder }).ToList();
                    return View(model);
                }

                product.Name = model.Name;
                product.Description = model.Description;
                product.CategoryID = model.CategoryID;
                product.Price = model.Price;
                product.DiscountPrice = model.DiscountPrice;
                product.StockQuantity = model.StockQuantity;
                product.Status = model.Status;

                // Xóa ảnh đã đánh dấu
                if (model.DeleteImageIDs != null && model.DeleteImageIDs.Any())
                {
                    var imagesToDelete = product.ProductImages
                        .Where(i => model.DeleteImageIDs.Contains(i.ImageID))
                        .ToList();

                    foreach (var img in imagesToDelete)
                    {
                        DeletePhysicalFile(img.ImageURL);
                        _context.ProductImages.Remove(img);
                    }
                }

                // Upload ảnh mới
                if (model.UploadImages != null && model.UploadImages.Any(f => f != null && f.ContentLength > 0))
                {
                    await SaveProductImagesAsync(product.ProductID, model.UploadImages, null);
                }

                // Cập nhật ảnh đại diện (IsMain)
                if (model.MainImageID.HasValue)
                {
                    foreach (var img in product.ProductImages)
                    {
                        img.IsMain = (img.ImageID == model.MainImageID.Value);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã cập nhật sản phẩm \"{product.Name}\" thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật: " + ex.Message);
                model.CategoryList = await GetCategorySelectListAsync();
                model.StatusList = GetStatusSelectList();
                return View(model);
            }
        }

        // GET: Product/Details/5
        public async Task<ActionResult> Details(int id)
        {
            ViewBag.Title = "Chi tiết Sản phẩm";

            try
            {
                var product = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews.Select(r => r.User))
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction("Index");
                }

                var model = new ProductDetailViewModel
                {
                    ProductID = product.ProductID,
                    Name = product.Name,
                    Description = product.Description,
                    CategoryName = product.Category?.Name,
                    CategoryID = product.CategoryID,
                    Price = product.Price,
                    DiscountPrice = product.DiscountPrice,
                    StockQuantity = product.StockQuantity,
                    Status = product.Status,
                    StatusText = GetStatusText(product.Status),
                    ViewCount = product.ViewCount,
                    CreatedAt = product.CreatedAt,
                    AverageRating = product.Reviews.Any() ? product.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = product.Reviews.Count,
                    Images = product.ProductImages.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageItem
                    {
                        ImageID = i.ImageID,
                        ImageURL = i.ImageURL,
                        IsMain = i.IsMain,
                        DisplayOrder = i.DisplayOrder
                    }).ToList(),
                    Reviews = product.Reviews.OrderByDescending(r => r.CreatedAt).Take(10).Select(r => new ProductReviewItem
                    {
                        ReviewID = r.ReviewID,
                        UserName = r.User?.FullName ?? r.User?.Email ?? "Ẩn danh",
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Product/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.OrderDetails)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                if (product.OrderDetails.Any())
                {
                    return Json(new { success = false, message = $"Không thể xóa sản phẩm \"{product.Name}\" vì đã có đơn hàng liên quan. Hãy chuyển trạng thái sang \"Ngừng bán\"." });
                }

                // Xóa ảnh vật lý
                foreach (var img in product.ProductImages.ToList())
                {
                    DeletePhysicalFile(img.ImageURL);
                    _context.ProductImages.Remove(img);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Đã xóa sản phẩm \"{product.Name}\" thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        #region Helper Methods

        private ProductItemViewModel MapToProductItem(Product p)
        {
            return new ProductItemViewModel
            {
                ProductID = p.ProductID,
                Name = p.Name,
                CategoryName = p.Category?.Name,
                CategoryID = p.CategoryID,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                StockQuantity = p.StockQuantity,
                Status = p.Status,
                StatusText = GetStatusText(p.Status),
                ViewCount = p.ViewCount,
                CreatedAt = p.CreatedAt,
                MainImageUrl = p.ProductImages?.FirstOrDefault(i => i.IsMain)?.ImageURL
                               ?? p.ProductImages?.FirstOrDefault()?.ImageURL,
                ImageCount = p.ProductImages?.Count ?? 0,
                AverageRating = p.Reviews != null && p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = p.Reviews?.Count ?? 0
            };
        }

        private string GetStatusText(int status)
        {
            switch (status)
            {
                case 1: return "Còn hàng";
                case 2: return "Hết hàng";
                case 3: return "Ngừng bán";
                default: return "Không rõ";
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

        private IEnumerable<SelectListItem> GetStatusSelectList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Tất cả trạng thái --" },
                new SelectListItem { Value = "1", Text = "Còn hàng" },
                new SelectListItem { Value = "2", Text = "Hết hàng" },
                new SelectListItem { Value = "3", Text = "Ngừng bán" }
            };
        }

        private async Task SaveProductImagesAsync(int productId, HttpPostedFileBase[] files, int? mainImageIndex)
        {
            var uploadDir = Server.MapPath("~/Content/Uploads/Products/");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            int order = 0;
            bool isFirstImage = !await _context.ProductImages.AnyAsync(i => i.ProductID == productId);

            foreach (var file in files)
            {
                if (file == null || file.ContentLength == 0) continue;

                // Validate file type
                var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(file.FileName)?.ToLower();
                if (!allowedTypes.Contains(ext)) continue;

                // Validate file size (max 5MB)
                if (file.ContentLength > 5 * 1024 * 1024) continue;

                var fileName = $"{productId}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadDir, fileName);
                file.SaveAs(filePath);

                var image = new ProductImage
                {
                    ProductID = productId,
                    ImageURL = "/Content/Uploads/Products/" + fileName,
                    IsMain = isFirstImage && order == 0,
                    DisplayOrder = order++
                };

                _context.ProductImages.Add(image);
            }

            await _context.SaveChangesAsync();
        }

        private void DeletePhysicalFile(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                var physicalPath = Server.MapPath("~" + imageUrl);
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }
            catch
            {
                // Bỏ qua lỗi xóa file
            }
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
