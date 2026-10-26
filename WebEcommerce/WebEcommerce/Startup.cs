using System;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Owin;
using WebEcommerce.Models;

[assembly: OwinStartupAttribute(typeof(WebEcommerce.Startup))]
namespace WebEcommerce
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            SeedRolesAndAdmin();
        }

        private void SeedRolesAndAdmin()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    // Bảng AspNet* có trong 01, bảng flash sale do DatabaseConfig chạy script 08 (Application_Start chạy trước)
                    SeedDefaultFlashSale(context);

                    var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
                    var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

                    // 1. Seed Roles
                    string[] roleNames = { "Customer", "StoreManager", "Administrator" };
                    foreach (var roleName in roleNames)
                    {
                        if (!roleManager.RoleExists(roleName))
                        {
                            roleManager.Create(new IdentityRole(roleName));
                        }
                    }

                    // 2. Seed Default Administrator if not exists
                    string adminEmail = "admin@thechillshop.vn";
                    var adminUser = userManager.FindByEmail(adminEmail);
                    if (adminUser == null)
                    {
                        var newAdmin = new ApplicationUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            FullName = "Nguyễn Văn Admin",
                            PhoneNumber = "0988888888",
                            Address = "Hệ thống TheChillShop, TP. Hồ Chí Minh",
                            IsActive = true,
                            EmailConfirmed = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        var result = userManager.Create(newAdmin, "Admin@123456");
                        if (result.Succeeded)
                        {
                            userManager.AddToRole(newAdmin.Id, "Administrator");
                        }
                    }
                    else
                    {
                        if (!userManager.IsInRole(adminUser.Id, "Administrator"))
                        {
                            userManager.AddToRole(adminUser.Id, "Administrator");
                        }
                    }

                    // 3. Seed tài khoản mẫu cho development & testing
                    SeedUser(userManager, "customer1@gmail.com", "Customer@123456",
                        fullName: "Nguyễn Thị Lan",       phone: "0901234561",
                        address: "123 Lê Lợi, Q1, TP.HCM", role: "Customer");

                    SeedUser(userManager, "customer2@gmail.com", "Customer@123456",
                        fullName: "Trần Văn Minh",         phone: "0912345672",
                        address: "456 Trần Hưng Đạo, Q5, TP.HCM", role: "Customer");

                    SeedUser(userManager, "customer3@gmail.com", "Customer@123456",
                        fullName: "Phạm Thị Hoa",          phone: "0923456783",
                        address: "789 Nguyễn Trãi, Q7, TP.HCM", role: "Customer");

                    SeedUser(userManager, "customer4@gmail.com", "Customer@123456",
                        fullName: "Hoàng Văn Nam",         phone: "0945678125",
                        address: "12 Võ Văn Tần, Q3, TP.HCM", role: "Customer");

                    SeedUser(userManager, "customer5@gmail.com", "Customer@123456",
                        fullName: "Đỗ Thị Mai",            phone: "0956781236",
                        address: "34 Phan Xích Long, Phú Nhuận, TP.HCM", role: "Customer");

                    SeedUser(userManager, "customer6@gmail.com", "Customer@123456",
                        fullName: "Vũ Đức Anh",            phone: "0967812347",
                        address: "56 Cầu Giấy, Cầu Giấy, Hà Nội", role: "Customer");

                    SeedUser(userManager, "customer7@gmail.com", "Customer@123456",
                        fullName: "Bùi Thị Thu",           phone: "0978123458",
                        address: "78 Nguyễn Văn Linh, Hải Châu, Đà Nẵng", role: "Customer");

                    SeedUser(userManager, "customer8@gmail.com", "Customer@123456",
                        fullName: "Ngô Quốc Bảo",          phone: "0989234569",
                        address: "90 Quang Trung, Gò Vấp, TP.HCM", role: "Customer");

                    SeedUser(userManager, "customer9@gmail.com", "Customer@123456",
                        fullName: "Lý Thị Ngọc",           phone: "0390345670",
                        address: "21 Trần Phú, Ninh Kiều, Cần Thơ", role: "Customer");

                    SeedUser(userManager, "customer10@gmail.com", "Customer@123456",
                        fullName: "Đặng Minh Khoa",        phone: "0381456781",
                        address: "43 Lê Duẩn, Q1, TP.HCM", role: "Customer");

                    SeedUser(userManager, "manager@thechillshop.vn", "Manager@123456",
                        fullName: "Lê Quản Lý",            phone: "0934567894",
                        address: "TheChillShop HQ, Q3, TP.HCM", role: "StoreManager");

                    SeedUser(userManager, "manager2@thechillshop.vn", "Manager@123456",
                        fullName: "Trương Thị Kho",        phone: "0935678905",
                        address: "TheChillShop Kho Thủ Đức, TP.HCM", role: "StoreManager");
                }
            }
            catch (Exception)
            {
                // Silently bypass if database is initializing or offline
            }
        }

        /// <summary>
        /// Tạo tài khoản mẫu nếu chưa tồn tại (idempotent).
        /// </summary>
        private void SeedUser(
            UserManager<ApplicationUser> userManager,
            string email, string password,
            string fullName, string phone, string address, string role)
        {
            try
            {
                if (userManager.FindByEmail(email) != null) return;

                var user = new ApplicationUser
                {
                    UserName       = email,
                    Email          = email,
                    FullName       = fullName,
                    PhoneNumber    = phone,
                    Address        = address,
                    IsActive       = true,
                    EmailConfirmed = true,
                    CreatedAt      = DateTime.UtcNow
                };

                var result = userManager.Create(user, password);
                if (result.Succeeded)
                {
                    userManager.AddToRole(user.Id, role);
                }
            }
            catch (Exception)
            {
                // Bỏ qua nếu user đã tồn tại hoặc DB chưa sẵn sàng
            }
        }

        /// <summary>
        /// Khởi tạo đợt Flash Sale mẫu khi CSDL chưa có đợt nào.
        /// Không tự bật/gia hạn lại đợt đã có để tôn trọng thao tác tắt/kết thúc của admin.
        /// </summary>
        private void SeedDefaultFlashSale(ApplicationDbContext context)
        {
            try
            {
                if (context.FlashSales.Any()) return;

                var sampleProducts = context.Products
                    .Where(p => p.Status != 3)
                    .OrderBy(p => p.ProductID)
                    .Take(5)
                    .ToList();
                if (!sampleProducts.Any()) return;

                var now = DateTime.UtcNow;
                var fs = new FlashSale
                {
                    Title = "FLASH SALE TỰU TRƯỜNG - GIẢM SỐC TỚI 50%",
                    Description = "Ưu đãi độc quyền chớp nhoáng! Số lượng có hạn, tự động kết thúc khi hết thời gian!",
                    StartTime = now.AddHours(-1),
                    EndTime = now.AddHours(28),
                    IsActive = true,
                    CreatedAt = now
                };
                context.FlashSales.Add(fs);
                context.SaveChanges();

                foreach (var p in sampleProducts)
                {
                    decimal fsPrice = p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0
                        ? Math.Round(p.DiscountPrice.Value * 0.85m)
                        : Math.Round(p.Price * 0.7m);

                    context.FlashSaleItems.Add(new FlashSaleItem
                    {
                        FlashSaleID = fs.FlashSaleID,
                        ProductID = p.ProductID,
                        FlashSalePrice = fsPrice,
                        MaxQuantity = 30,
                        SoldQuantity = 0 // chưa có đơn thật nào, tránh làm sai thống kê doanh thu flash sale
                    });
                }
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[Startup/SeedDefaultFlashSale] {ex}");
            }
        }
    }
}
