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
    public class PromotionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PromotionController()
        {
            _context = new ApplicationDbContext();
        }

        // ==============================================================
        // GET: /Promotion
        // Danh sách mã giảm giá kèm bộ lọc và chỉ số KPI
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult> Index(string search = "", string status = "All", string type = "All")
        {
            ViewBag.Title = "Quản lý Mã giảm giá (Voucher)";

            try
            {
                var query = _context.PromotionalCodes
                    .AsNoTracking()
                    .Include(p => p.Orders)
                    .AsQueryable();

                // 1. Thống kê tổng quan trước khi áp dụng bộ lọc
                var allList = await _context.PromotionalCodes.AsNoTracking().ToListAsync();
                var now = DateTime.UtcNow;

                var totalCount = allList.Count;
                var activeCount = allList.Count(p => p.IsActive);
                var inDateActiveCount = allList.Count(p => p.IsActive && p.StartDate <= now && p.EndDate >= now);
                var expiredCount = allList.Count(p => p.EndDate < now);
                var totalUsageCount = allList.Sum(p => p.CurrentUsage);

                // 2. Lọc theo từ khóa tìm kiếm
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim().ToLower();
                    query = query.Where(p => p.Code.ToLower().Contains(search) || 
                                             (p.Description != null && p.Description.ToLower().Contains(search)));
                }

                // 3. Lọc theo trạng thái
                if (!string.IsNullOrWhiteSpace(status) && status != "All")
                {
                    switch (status)
                    {
                        case "Active":
                            query = query.Where(p => p.IsActive);
                            break;
                        case "Inactive":
                            query = query.Where(p => !p.IsActive);
                            break;
                        case "Expired":
                            query = query.Where(p => p.EndDate < now);
                            break;
                        case "Upcoming":
                            query = query.Where(p => p.StartDate > now && p.IsActive);
                            break;
                    }
                }

                // 4. Lọc theo loại giảm giá
                if (!string.IsNullOrWhiteSpace(type) && type != "All")
                {
                    query = query.Where(p => p.DiscountType == type);
                }

                var promotions = await query
                    .OrderByDescending(p => p.PromoCodeID)
                    .ToListAsync();

                var viewModel = new PromotionListViewModel
                {
                    Promotions = promotions,
                    SearchTerm = search,
                    StatusFilter = status,
                    TypeFilter = type,
                    TotalCount = totalCount,
                    ActiveCount = activeCount,
                    InDateActiveCount = inDateActiveCount,
                    ExpiredCount = expiredCount,
                    TotalUsageCount = totalUsageCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Promotion/Index] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách mã giảm giá.";
                return View(new PromotionListViewModel());
            }
        }

        // ==============================================================
        // GET: /Promotion/Create
        // Form tạo mới mã giảm giá
        // ==============================================================
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.Title = "Thêm Mã giảm giá mới";

            var model = new PromotionFormViewModel
            {
                StartDate = DateTime.Now.AddMinutes(-5),
                EndDate = DateTime.Now.AddDays(30),
                DiscountType = "Percentage",
                DiscountValue = 10,
                MinOrderAmount = 0,
                MaxUsage = 100,
                IsActive = true
            };

            return View(model);
        }

        // ==============================================================
        // POST: /Promotion/Create
        // Xử lý tạo mới mã giảm giá vào CSDL
        // ==============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(PromotionFormViewModel model)
        {
            ViewBag.Title = "Thêm Mã giảm giá mới";

            try
            {
                if (model == null)
                {
                    TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                    return RedirectToAction("Index");
                }

                // Chuẩn hóa mã Code: Viết hoa, xóa khoảng trắng
                model.Code = model.Code?.Trim().ToUpper();

                // Kiểm tra trùng lặp mã Code
                if (!string.IsNullOrWhiteSpace(model.Code))
                {
                    var isCodeExists = await _context.PromotionalCodes
                        .AnyAsync(p => p.Code.ToUpper() == model.Code);

                    if (isCodeExists)
                    {
                        ModelState.AddModelError("Code", $"Mã giảm giá \"{model.Code}\" đã tồn tại trong hệ thống. Vui lòng chọn mã khác.");
                    }
                }

                // Validate thời gian hiệu lực
                if (model.EndDate < model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
                }

                // Validate theo loại giảm giá
                if (model.DiscountType == "Percentage")
                {
                    if (model.DiscountValue <= 0 || model.DiscountValue > 100)
                    {
                        ModelState.AddModelError("DiscountValue", "Giảm theo phần trăm phải nằm trong khoảng từ 1% đến 100%.");
                    }
                }
                else if (model.DiscountType == "Fixed")
                {
                    if (model.DiscountValue <= 0)
                    {
                        ModelState.AddModelError("DiscountValue", "Số tiền giảm cố định phải lớn hơn 0đ.");
                    }
                }
                else
                {
                    ModelState.AddModelError("DiscountType", "Loại giảm giá không hợp lệ.");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var entity = new PromotionalCode
                {
                    Code = model.Code,
                    Description = model.Description?.Trim(),
                    DiscountType = model.DiscountType,
                    DiscountValue = model.DiscountValue,
                    MinOrderAmount = model.MinOrderAmount,
                    MaxUsage = model.MaxUsage,
                    CurrentUsage = 0,
                    StartDate = model.StartDate.ToUniversalTime(),
                    EndDate = model.EndDate.ToUniversalTime(),
                    IsActive = model.IsActive
                };

                _context.PromotionalCodes.Add(entity);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã tạo mã giảm giá \"{entity.Code}\" thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Promotion/Create POST] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi lưu mã giảm giá: " + ex.Message;
                return View(model);
            }
        }

        // ==============================================================
        // GET: /Promotion/Edit/5
        // Form chỉnh sửa mã giảm giá
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult> Edit(int id)
        {
            ViewBag.Title = "Chỉnh sửa Mã giảm giá";

            try
            {
                var promo = await _context.PromotionalCodes.FindAsync(id);
                if (promo == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá cần chỉnh sửa.";
                    return RedirectToAction("Index");
                }

                var model = new PromotionFormViewModel
                {
                    PromoCodeID = promo.PromoCodeID,
                    Code = promo.Code,
                    Description = promo.Description,
                    DiscountType = promo.DiscountType,
                    DiscountValue = promo.DiscountValue,
                    MinOrderAmount = promo.MinOrderAmount,
                    MaxUsage = promo.MaxUsage,
                    CurrentUsage = promo.CurrentUsage,
                    StartDate = promo.StartDate.ToLocalTime(),
                    EndDate = promo.EndDate.ToLocalTime(),
                    IsActive = promo.IsActive
                };

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Promotion/Edit GET] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi.";
                return RedirectToAction("Index");
            }
        }

        // ==============================================================
        // POST: /Promotion/Edit/5
        // Cập nhật mã giảm giá
        // ==============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(PromotionFormViewModel model)
        {
            ViewBag.Title = "Chỉnh sửa Mã giảm giá";

            try
            {
                if (model == null)
                {
                    TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                    return RedirectToAction("Index");
                }

                var entity = await _context.PromotionalCodes.FindAsync(model.PromoCodeID);
                if (entity == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá trong cơ sở dữ liệu.";
                    return RedirectToAction("Index");
                }

                // Số lượt đã dùng lấy từ DB, không tin hidden field của form
                model.CurrentUsage = entity.CurrentUsage;
                model.Code = model.Code?.Trim().ToUpper();

                // Kiểm tra trùng lặp mã (loại trừ chính id đang sửa)
                var isCodeExists = await _context.PromotionalCodes
                    .AnyAsync(p => p.Code.ToUpper() == model.Code && p.PromoCodeID != model.PromoCodeID);

                if (isCodeExists)
                {
                    ModelState.AddModelError("Code", $"Mã giảm giá \"{model.Code}\" đã được sử dụng ở một voucher khác.");
                }

                if (model.EndDate < model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
                }

                if (model.DiscountType == "Percentage")
                {
                    if (model.DiscountValue <= 0 || model.DiscountValue > 100)
                    {
                        ModelState.AddModelError("DiscountValue", "Giảm theo phần trăm phải nằm trong khoảng từ 1% đến 100%.");
                    }
                }
                else if (model.DiscountType == "Fixed")
                {
                    if (model.DiscountValue <= 0)
                    {
                        ModelState.AddModelError("DiscountValue", "Số tiền giảm cố định phải lớn hơn 0đ.");
                    }
                }
                else
                {
                    ModelState.AddModelError("DiscountType", "Loại giảm giá không hợp lệ.");
                }

                if (model.MaxUsage < model.CurrentUsage)
                {
                    ModelState.AddModelError("MaxUsage", $"Số lượt dùng tối đa không thể nhỏ hơn số lượt đã sử dụng ({model.CurrentUsage} lượt).");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                entity.Code = model.Code;
                entity.Description = model.Description?.Trim();
                entity.DiscountType = model.DiscountType;
                entity.DiscountValue = model.DiscountValue;
                entity.MinOrderAmount = model.MinOrderAmount;
                entity.MaxUsage = model.MaxUsage;
                entity.StartDate = model.StartDate.ToUniversalTime();
                entity.EndDate = model.EndDate.ToUniversalTime();
                entity.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã cập nhật mã giảm giá \"{entity.Code}\" thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Promotion/Edit POST] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật: " + ex.Message;
                return View(model);
            }
        }

        // ==============================================================
        // POST: /Promotion/ToggleStatus/5
        // Bật / Tắt nhanh trạng thái hoạt động của Voucher
        // ==============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ToggleStatus(int id)
        {
            try
            {
                var promo = await _context.PromotionalCodes.FindAsync(id);
                if (promo == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá.";
                    return RedirectToAction("Index");
                }

                promo.IsActive = !promo.IsActive;
                await _context.SaveChangesAsync();

                var statusText = promo.IsActive ? "kích hoạt" : "vô hiệu hóa";
                TempData["SuccessMessage"] = $"Đã {statusText} mã giảm giá \"{promo.Code}\".";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Promotion/ToggleStatus] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi đổi trạng thái.";
                return RedirectToAction("Index");
            }
        }

        // ==============================================================
        // POST: /Promotion/Delete/5
        // Xóa an toàn mã giảm giá
        // ==============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var promo = await _context.PromotionalCodes
                    .Include(p => p.Orders)
                    .FirstOrDefaultAsync(p => p.PromoCodeID == id);

                if (promo == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá để xóa.";
                    return RedirectToAction("Index");
                }

                // Bảo toàn dữ liệu: Nếu voucher đã có đơn hàng sử dụng, không được xóa cứng (hard-delete)
                if (promo.Orders != null && promo.Orders.Any())
                {
                    promo.IsActive = false;
                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] = $"Mã giảm giá \"{promo.Code}\" đã được áp dụng trong {promo.Orders.Count} đơn hàng lịch sử nên không thể xóa hoàn toàn. Hệ thống đã tự động chuyển sang trạng thái VÔ HIỆU HÓA.";
                    return RedirectToAction("Index");
                }

                // Nếu chưa có đơn nào dùng, cho phép xóa hoàn toàn
                _context.PromotionalCodes.Remove(promo);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn mã giảm giá \"{promo.Code}\"!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Promotion/Delete] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa mã giảm giá.";
                return RedirectToAction("Index");
            }
        }

        // ==============================================================
        // GET: /Promotion/Details/5
        // Xem chi tiết voucher và danh sách các đơn hàng đã áp dụng mã
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult> Details(int id)
        {
            ViewBag.Title = "Chi tiết Mã giảm giá";

            try
            {
                var promo = await _context.PromotionalCodes
                    .Include(p => p.Orders.Select(o => o.User))
                    .FirstOrDefaultAsync(p => p.PromoCodeID == id);

                if (promo == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá.";
                    return RedirectToAction("Index");
                }

                var relatedOrders = promo.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                var viewModel = new PromotionDetailsViewModel
                {
                    Promotion = promo,
                    RelatedOrders = relatedOrders,
                    TotalDiscountGiven = relatedOrders.Sum(o => o.DiscountAmount),
                    TotalRevenueGenerated = relatedOrders.Sum(o => o.FinalAmount)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Promotion/Details] {ex}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải chi tiết mã giảm giá.";
                return RedirectToAction("Index");
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
