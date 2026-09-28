using System;
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
                    // Ensure missing Identity tables exist
                    context.Database.ExecuteSqlCommand(@"
                        CREATE TABLE IF NOT EXISTS ""AspNetUserClaims"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""UserId"" VARCHAR(128) NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE,
                            ""ClaimType"" TEXT,
                            ""ClaimValue"" TEXT
                        );
                        CREATE TABLE IF NOT EXISTS ""AspNetUserLogins"" (
                            ""LoginProvider"" VARCHAR(128) NOT NULL,
                            ""ProviderKey"" VARCHAR(128) NOT NULL,
                            ""UserId"" VARCHAR(128) NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE,
                            PRIMARY KEY (""LoginProvider"", ""ProviderKey"", ""UserId"")
                        );
                    ");

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

                    SeedUser(userManager, "manager@thechillshop.vn", "Manager@123456",
                        fullName: "Lê Quản Lý",            phone: "0934567894",
                        address: "TheChillShop HQ, Q3, TP.HCM", role: "StoreManager");
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
    }
}
