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
                }
            }
            catch (Exception)
            {
                // Silently bypass if database is initializing or offline
            }
        }
    }
}
