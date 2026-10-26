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
    public class FlashSaleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FlashSaleController()
        {
            _context = new ApplicationDbContext();
        }

        // ==============================================================
        // GET: /FlashSale
        // Danh sách các đợt Flash Sale kèm chỉ số KPI và bộ lọc
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult> Index(string search = "", string status = "All")
        {
            ViewBag.Title = "Quản lý Sự kiện Flash Sale";

            try
            {
                var query = _context.FlashSales
                    .AsNoTracking()
                    .Include(f => f.FlashSaleItems.Select(i => i.Product.ProductImages))
                    .AsQueryable();

                var now = DateTime.UtcNow;

                // 1. Thống kê KPI tổng thể trước khi lọc
                var allSales = await _context.FlashSales
                    .AsNoTracking()
                    .Include(f => f.FlashSaleItems)
                    .ToListAsync();

                var totalCount = allSales.Count;
                var liveCount = allSales.Count(f => f.IsActive && f.StartTime <= now && f.EndTime > now);
                var upcomingCount = allSales.Count(f => f.IsActive && f.StartTime > now);
                var endedCount = allSales.Count(f => f.EndTime <= now);
                var totalSold = allSales.SelectMany(f => f.FlashSaleItems).Sum(i => i.SoldQuantity);
                var totalRev = allSales.SelectMany(f => f.FlashSaleItems).Sum(i => i.SoldQuantity * i.FlashSalePrice);

                // 2. Tìm kiếm theo tên hoặc mô tả
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim().ToLower();
                    query = query.Where(f => f.Title.ToLower().Contains(search) ||
                                             (f.Description != null && f.Description.ToLower().Contains(search)));
                }

                // 3. Lọc theo trạng thái
                if (!string.IsNullOrWhiteSpace(status) && status != "All")
                {
                    switch (status)
                    {
                        case "Live":
                            query = query.Where(f => f.IsActive && f.StartTime <= now && f.EndTime > now);
                            break;
                        case "Upcoming":
                            query = query.Where(f => f.IsActive && f.StartTime > now);
                            break;
                        case "Ended":
                            query = query.Where(f => f.EndTime <= now);
                            break;
                        case "Inactive":
                            query = query.Where(f => !f.IsActive);
                            break;
                    }
                }

                var list = await query
                    .OrderByDescending(f => f.FlashSaleID)
                    .ToListAsync();

                var viewModel = new FlashSaleListViewModel
                {
                    FlashSales = list,
                    SearchTerm = search,
                    StatusFilter = status,
                    TotalCount = totalCount,
                    LiveCount = liveCount,
                    UpcomingCount = upcomingCount,
                    EndedCount = endedCount,
                    TotalSoldQuantity = totalSold,
                    TotalRevenue = totalRev
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FlashSale/Index] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách Flash Sale.";
                return View(new FlashSaleListViewModel());
            }
        }

        // ==============================================================
        // GET: /FlashSale/Create
        // Form tạo mới đợt Flash Sale
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult> Create()
        {
            ViewBag.Title = "Tạo Đợt Flash Sale Mới";

            try
            {
                var availableProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status == 1 && p.StockQuantity > 0)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var model = new FlashSaleFormViewModel
                {
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now.AddHours(24),
                    IsActive = true,
                    AvailableProducts = availableProducts
                };

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FlashSale/Create GET] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi chuẩn bị form tạo Flash Sale.";
                return RedirectToAction("Index");
            }
        }

        // ==============================================================
        // POST: /FlashSale/Create
        // Xử lý tạo mới đợt Flash Sale vào CSDL
        // ==============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(FlashSaleFormViewModel model)
        {
            ViewBag.Title = "Tạo Đợt Flash Sale Mới";

            try
            {
                // Tải lại danh sách sản phẩm để render lại form nếu có lỗi validation
                var availableProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status == 1 && p.StockQuantity > 0)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                model.AvailableProducts = availableProducts;

                // 1. Kiểm tra thời gian
                if (model.EndTime <= model.StartTime)
                {
                    ModelState.AddModelError("EndTime", "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
                }

                // 2. Kiểm tra danh sách sản phẩm
                if (model.Items == null || !model.Items.Any())
                {
                    ModelState.AddModelError("", "Vui lòng chọn ít nhất 1 sản phẩm tham gia đợt Flash Sale.");
                }
                else
                {
                    // Kiểm tra trùng lặp sản phẩm
                    var productIds = model.Items.Select(i => i.ProductID).ToList();
                    if (productIds.Count != productIds.Distinct().Count())
                    {
                        ModelState.AddModelError("", "Không được chọn trùng sản phẩm trong cùng một đợt Flash Sale.");
                    }

                    // Kiểm tra từng sản phẩm
                    for (int i = 0; i < model.Items.Count; i++)
                    {
                        var item = model.Items[i];
                        var prod = availableProducts.FirstOrDefault(p => p.ProductID == item.ProductID);
                        if (prod == null)
                        {
                            ModelState.AddModelError("", $"Sản phẩm #{item.ProductID} không tồn tại hoặc đã ngừng bán.");
                            continue;
                        }

                        if (item.FlashSalePrice <= 0)
                        {
                            ModelState.AddModelError($"Items[{i}].FlashSalePrice", $"Giá Flash Sale của sản phẩm \"{prod.Name}\" phải lớn hơn 0đ.");
                        }
                        else if (item.FlashSalePrice >= prod.Price)
                        {
                            ModelState.AddModelError($"Items[{i}].FlashSalePrice", $"Giá Flash Sale của \"{prod.Name}\" ({item.FlashSalePrice:N0}đ) phải nhỏ hơn giá gốc ({prod.Price:N0}đ).");
                        }

                        if (item.MaxQuantity <= 0)
                        {
                            ModelState.AddModelError($"Items[{i}].MaxQuantity", $"Số lượng mở bán của \"{prod.Name}\" phải từ 1 trở lên.");
                        }
                        else if (item.MaxQuantity > prod.StockQuantity)
                        {
                            ModelState.AddModelError($"Items[{i}].MaxQuantity", $"Số lượng mở bán của \"{prod.Name}\" ({item.MaxQuantity}) không được vượt quá tồn kho thực tế ({prod.StockQuantity}).");
                        }
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // 3. Lưu vào CSDL trong transaction
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var flashSale = new FlashSale
                        {
                            Title = model.Title.Trim(),
                            Description = model.Description?.Trim(),
                            StartTime = model.StartTime.ToUniversalTime(),
                            EndTime = model.EndTime.ToUniversalTime(),
                            IsActive = model.IsActive,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.FlashSales.Add(flashSale);
                        await _context.SaveChangesAsync();

                        foreach (var it in model.Items)
                        {
                            var fsItem = new FlashSaleItem
                            {
                                FlashSaleID = flashSale.FlashSaleID,
                                ProductID = it.ProductID,
                                FlashSalePrice = it.FlashSalePrice,
                                MaxQuantity = it.MaxQuantity,
                                SoldQuantity = 0
                            };
                            _context.FlashSaleItems.Add(fsItem);
                        }

                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        TempData["SuccessMessage"] = $"Đã tạo đợt Flash Sale \"{flashSale.Title}\" với {model.Items.Count} sản phẩm thành công!";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FlashSale/Create POST] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tạo Flash Sale: " + ex.Message;
                return View(model);
            }
        }

        // ==============================================================
        // GET: /FlashSale/Edit/5
        // Form chỉnh sửa đợt Flash Sale
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult> Edit(int id)
        {
            ViewBag.Title = "Chỉnh Sửa Đợt Flash Sale";

            try
            {
                var flashSale = await _context.FlashSales
                    .Include(f => f.FlashSaleItems.Select(i => i.Product.ProductImages))
                    .Include(f => f.FlashSaleItems.Select(i => i.Product.Category))
                    .FirstOrDefaultAsync(f => f.FlashSaleID == id);

                if (flashSale == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đợt Flash Sale cần chỉnh sửa.";
                    return RedirectToAction("Index");
                }

                var availableProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status == 1 && p.StockQuantity > 0)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var model = new FlashSaleFormViewModel
                {
                    FlashSaleID = flashSale.FlashSaleID,
                    Title = flashSale.Title,
                    Description = flashSale.Description,
                    StartTime = flashSale.StartTime.ToLocalTime(),
                    EndTime = flashSale.EndTime.ToLocalTime(),
                    IsActive = flashSale.IsActive,
                    AvailableProducts = availableProducts,
                    Items = flashSale.FlashSaleItems.Select(i => new FlashSaleItemInputModel
                    {
                        FlashSaleItemID = i.FlashSaleItemID,
                        ProductID = i.ProductID,
                        ProductName = i.Product?.Name ?? ("Sản phẩm #" + i.ProductID),
                        ProductImage = i.Product?.ProductImages?.FirstOrDefault(img => img.IsMain)?.ImageURL 
                                       ?? i.Product?.ProductImages?.FirstOrDefault()?.ImageURL,
                        CategoryName = i.Product?.Category?.Name,
                        OriginalPrice = i.Product?.Price ?? 0,
                        StockQuantity = i.Product?.StockQuantity ?? 0,
                        FlashSalePrice = i.FlashSalePrice,
                        MaxQuantity = i.MaxQuantity,
                        SoldQuantity = i.SoldQuantity
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FlashSale/Edit GET] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi.";
                return RedirectToAction("Index");
            }
        }

        // ==============================================================
        // POST: /FlashSale/Edit/5
        // Cập nhật thông tin và sản phẩm đợt Flash Sale
        // ==============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(FlashSaleFormViewModel model)
        {
            ViewBag.Title = "Chỉnh Sửa Đợt Flash Sale";

            try
            {
                var availableProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Status == 1 && p.StockQuantity > 0)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                model.AvailableProducts = availableProducts;

                if (model.EndTime <= model.StartTime)
                {
                    ModelState.AddModelError("EndTime", "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
                }

                if (model.Items == null || !model.Items.Any())
                {
                    ModelState.AddModelError("", "Vui lòng chọn ít nhất 1 sản phẩm tham gia đợt Flash Sale.");
                }
                else
                {
                    // Sản phẩm và số đã bán của item cũ lấy từ DB, không tin hidden field client gửi lên
                    var dbItems = await _context.FlashSaleItems
                        .AsNoTracking()
                        .Include(i => i.Product)
                        .Where(i => i.FlashSaleID == model.FlashSaleID)
                        .ToListAsync();

                    foreach (var item in model.Items)
                    {
                        var dbItem = dbItems.FirstOrDefault(d => d.FlashSaleItemID == item.FlashSaleItemID);
                        item.ProductID = dbItem?.ProductID ?? item.ProductID;
                        item.SoldQuantity = dbItem?.SoldQuantity ?? 0;
                    }

                    foreach (var removed in dbItems.Where(d => d.SoldQuantity > 0 && !model.Items.Any(i => i.FlashSaleItemID == d.FlashSaleItemID)))
                    {
                        ModelState.AddModelError("", $"Sản phẩm \"{removed.Product?.Name}\" đã bán {removed.SoldQuantity} suất nên không thể xóa khỏi đợt Flash Sale.");
                    }

                    var productIds = model.Items.Select(i => i.ProductID).ToList();
                    if (productIds.Count != productIds.Distinct().Count())
                    {
                        ModelState.AddModelError("", "Không được chọn trùng sản phẩm trong cùng một đợt Flash Sale.");
                    }

                    for (int i = 0; i < model.Items.Count; i++)
                    {
                        var item = model.Items[i];
                        var prod = availableProducts.FirstOrDefault(p => p.ProductID == item.ProductID);
                        var originalPrice = prod?.Price ?? item.OriginalPrice;
                        var stock = prod?.StockQuantity ?? item.StockQuantity;

                        if (item.FlashSalePrice <= 0)
                        {
                            ModelState.AddModelError($"Items[{i}].FlashSalePrice", $"Giá Flash Sale của sản phẩm \"{item.ProductName}\" phải lớn hơn 0đ.");
                        }
                        else if (originalPrice > 0 && item.FlashSalePrice >= originalPrice)
                        {
                            ModelState.AddModelError($"Items[{i}].FlashSalePrice", $"Giá Flash Sale của \"{item.ProductName}\" ({item.FlashSalePrice:N0}đ) phải nhỏ hơn giá gốc ({originalPrice:N0}đ).");
                        }

                        if (item.MaxQuantity <= 0)
                        {
                            ModelState.AddModelError($"Items[{i}].MaxQuantity", $"Số lượng mở bán của \"{item.ProductName}\" phải từ 1 trở lên.");
                        }
                        else if (item.MaxQuantity < item.SoldQuantity)
                        {
                            ModelState.AddModelError($"Items[{i}].MaxQuantity", $"Số lượng mở bán của \"{item.ProductName}\" ({item.MaxQuantity}) không thể nhỏ hơn số lượng đã bán ({item.SoldQuantity}).");
                        }
                        else if (stock > 0 && item.MaxQuantity > stock)
                        {
                            ModelState.AddModelError($"Items[{i}].MaxQuantity", $"Số lượng mở bán của \"{item.ProductName}\" ({item.MaxQuantity}) vượt quá tồn kho thực tế ({stock}).");
                        }
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var flashSale = await _context.FlashSales
                            .Include(f => f.FlashSaleItems)
                            .FirstOrDefaultAsync(f => f.FlashSaleID == model.FlashSaleID);

                        if (flashSale == null)
                        {
                            TempData["ErrorMessage"] = "Không tìm thấy đợt Flash Sale trong cơ sở dữ liệu.";
                            return RedirectToAction("Index");
                        }

                        // Cập nhật thông tin Flash Sale
                        flashSale.Title = model.Title.Trim();
                        flashSale.Description = model.Description?.Trim();
                        flashSale.StartTime = model.StartTime.ToUniversalTime();
                        flashSale.EndTime = model.EndTime.ToUniversalTime();
                        flashSale.IsActive = model.IsActive;

                        // Đồng bộ danh sách FlashSaleItems:
                        // 1. Xóa những item không còn trong danh sách gửi lên (nếu chưa bán suất nào)
                        var submittedItemIds = model.Items.Where(i => i.FlashSaleItemID > 0).Select(i => i.FlashSaleItemID).ToList();
                        var itemsToRemove = flashSale.FlashSaleItems.Where(i => !submittedItemIds.Contains(i.FlashSaleItemID) && i.SoldQuantity == 0).ToList();

                        foreach (var rem in itemsToRemove)
                        {
                            _context.FlashSaleItems.Remove(rem);
                        }

                        // 2. Cập nhật hoặc thêm mới các item
                        foreach (var inputItem in model.Items)
                        {
                            if (inputItem.FlashSaleItemID > 0)
                            {
                                var existingItem = flashSale.FlashSaleItems.FirstOrDefault(i => i.FlashSaleItemID == inputItem.FlashSaleItemID);
                                if (existingItem != null)
                                {
                                    existingItem.ProductID = inputItem.ProductID;
                                    existingItem.FlashSalePrice = inputItem.FlashSalePrice;
                                    existingItem.MaxQuantity = inputItem.MaxQuantity;
                                }
                            }
                            else
                            {
                                var newItem = new FlashSaleItem
                                {
                                    FlashSaleID = flashSale.FlashSaleID,
                                    ProductID = inputItem.ProductID,
                                    FlashSalePrice = inputItem.FlashSalePrice,
                                    MaxQuantity = inputItem.MaxQuantity,
                                    SoldQuantity = 0
                                };
                                _context.FlashSaleItems.Add(newItem);
                            }
                        }

                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        TempData["SuccessMessage"] = $"Đã cập nhật đợt Flash Sale \"{flashSale.Title}\" thành công!";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FlashSale/Edit POST] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật: " + ex.Message;
                return View(model);
            }
        }

        // ==============================================================
        // GET: /FlashSale/Details/5
        // Chi tiết đợt Flash Sale, tỷ lệ bán và thống kê doanh thu
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult> Details(int id)
        {
            ViewBag.Title = "Chi Tiết Đợt Flash Sale";

            try
            {
                var flashSale = await _context.FlashSales
                    .Include(f => f.FlashSaleItems.Select(i => i.Product.ProductImages))
                    .Include(f => f.FlashSaleItems.Select(i => i.Product.Category))
                    .FirstOrDefaultAsync(f => f.FlashSaleID == id);

                if (flashSale == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đợt Flash Sale.";
                    return RedirectToAction("Index");
                }

                var items = flashSale.FlashSaleItems.ToList();
                var totalMax = items.Sum(i => i.MaxQuantity);
                var totalSold = items.Sum(i => i.SoldQuantity);
                var totalRevenue = items.Sum(i => i.SoldQuantity * i.FlashSalePrice);

                var viewModel = new FlashSaleDetailsViewModel
                {
                    FlashSale = flashSale,
                    TotalItems = items.Count,
                    TotalMaxQuantity = totalMax,
                    TotalSoldQuantity = totalSold,
                    TotalEstimatedRevenue = totalRevenue
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FlashSale/Details] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải chi tiết Flash Sale.";
                return RedirectToAction("Index");
            }
        }

        // ==============================================================
        // POST: /FlashSale/ToggleStatus/5
        // Bật / Tắt nhanh trạng thái hoạt động của Flash Sale
        // ==============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ToggleStatus(int id)
        {
            try
            {
                var flashSale = await _context.FlashSales.FindAsync(id);
                if (flashSale == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đợt Flash Sale.";
                    return RedirectToAction("Index");
                }

                flashSale.IsActive = !flashSale.IsActive;
                await _context.SaveChangesAsync();

                var statusText = flashSale.IsActive ? "kích hoạt" : "tạm ngưng";
                TempData["SuccessMessage"] = $"Đã {statusText} đợt Flash Sale \"{flashSale.Title}\".";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FlashSale/ToggleStatus] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi đổi trạng thái.";
                return RedirectToAction("Index");
            }
        }

        // ==============================================================
        // POST: /FlashSale/Delete/5
        // Xóa an toàn đợt Flash Sale
        // ==============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var flashSale = await _context.FlashSales
                    .Include(f => f.FlashSaleItems)
                    .FirstOrDefaultAsync(f => f.FlashSaleID == id);

                if (flashSale == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đợt Flash Sale để xóa.";
                    return RedirectToAction("Index");
                }

                var hasSoldItems = flashSale.FlashSaleItems.Any(i => i.SoldQuantity > 0);
                if (hasSoldItems)
                {
                    // Bảo toàn dữ liệu: Nếu đã bán được sản phẩm, không xóa cứng để phục vụ đối soát lịch sử
                    flashSale.IsActive = false;
                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] = $"Đợt Flash Sale \"{flashSale.Title}\" đã có sản phẩm được bán ra trong các đơn hàng nên không thể xóa hoàn toàn. Hệ thống đã tự động chuyển sang trạng thái TẠM NGƯNG.";
                    return RedirectToAction("Index");
                }

                _context.FlashSales.Remove(flashSale);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn đợt Flash Sale \"{flashSale.Title}\"!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FlashSale/Delete] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa Flash Sale.";
                return RedirectToAction("Index");
            }
        }

        // ==============================================================
        // GET: /FlashSale/Live
        // Chuyên trang Flash Sale công khai dành cho khách hàng & khách vãng lai
        // ==============================================================
        [HttpGet]
        [AllowAnonymous]
        [OverrideAuthorization]
        public async Task<ActionResult> Live(int? id, int? categoryId)
        {
            ViewBag.Title = "⚡ Giờ Vàng Flash Sale - Săn Deal Giá Sốc";

            try
            {
                var now = DateTime.UtcNow;

                // 1. Lấy tất cả các đợt Flash Sale đang active và chưa kết thúc (EndTime > now)
                var allActiveSales = await _context.FlashSales
                    .AsNoTracking()
                    .Include(f => f.FlashSaleItems)
                    .Include(f => f.FlashSaleItems.Select(i => i.Product))
                    .Include(f => f.FlashSaleItems.Select(i => i.Product.ProductImages))
                    .Include(f => f.FlashSaleItems.Select(i => i.Product.Category))
                    .Where(f => f.IsActive && f.EndTime > now)
                    .OrderBy(f => f.StartTime)
                    .ToListAsync();

                // Lọc bỏ sản phẩm xóa / không hoạt động
                foreach (var sale in allActiveSales)
                {
                    if (sale.FlashSaleItems != null)
                    {
                        sale.FlashSaleItems = sale.FlashSaleItems
                            .Where(i => i.Product != null && i.Product.Status != 3)
                            .ToList();
                    }
                }

                // 2. Xác định đợt Flash Sale hiện tại được chọn
                FlashSale currentSale = null;
                if (id.HasValue)
                {
                    currentSale = allActiveSales.FirstOrDefault(f => f.FlashSaleID == id.Value);
                }

                if (currentSale == null)
                {
                    // Ưu tiên đợt đang diễn ra (StartTime <= now < EndTime), nếu không thì đợt sắp diễn ra sớm nhất
                    currentSale = allActiveSales.FirstOrDefault(f => f.StartTime <= now && f.EndTime > now)
                                  ?? allActiveSales.FirstOrDefault();
                }

                // Nếu không có đợt nào đang/sắp diễn ra, lấy đợt kết thúc gần nhất để hiển thị giao diện fallback lịch sự
                if (currentSale == null)
                {
                    currentSale = await _context.FlashSales
                        .AsNoTracking()
                        .Include(f => f.FlashSaleItems.Select(i => i.Product.ProductImages))
                        .Include(f => f.FlashSaleItems.Select(i => i.Product.Category))
                        .OrderByDescending(f => f.EndTime)
                        .FirstOrDefaultAsync();

                    if (currentSale != null && currentSale.FlashSaleItems != null)
                    {
                        currentSale.FlashSaleItems = currentSale.FlashSaleItems
                            .Where(i => i.Product != null && i.Product.Status != 3)
                            .ToList();
                    }
                }

                // 3. Danh sách các danh mục có trong đợt sale hiện tại
                var currentItems = currentSale?.FlashSaleItems?.ToList() ?? new List<FlashSaleItem>();
                var saleCategories = currentItems
                    .Where(i => i.Product?.Category != null)
                    .Select(i => i.Product.Category)
                    .GroupBy(c => c.CategoryID)
                    .Select(g => g.First())
                    .OrderBy(c => c.Name)
                    .ToList();

                // 4. Lọc theo danh mục nếu người dùng chọn
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    currentItems = currentItems
                        .Where(i => i.Product != null && i.Product.CategoryID == categoryId.Value)
                        .ToList();
                }

                ViewBag.AllSales = allActiveSales;
                ViewBag.CurrentSale = currentSale;
                ViewBag.SaleCategories = saleCategories;
                ViewBag.SelectedCategoryId = categoryId;

                return View(currentItems);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FlashSale/Live] {ex}");
                ViewBag.AllSales = new List<FlashSale>();
                ViewBag.CurrentSale = null;
                ViewBag.SaleCategories = new List<Category>();
                ViewBag.SelectedCategoryId = null;
                return View(new List<FlashSaleItem>());
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
