using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebEcommerce.Filters
{
    /// <summary>
    /// Custom Authorize Attribute hỗ trợ phân quyền RBAC 3 cấp (Customer / StoreManager / Administrator).
    /// Khi người dùng chưa đăng nhập: Chuyển hướng về trang Đăng nhập (Account/Login).
    /// Khi người dùng đã đăng nhập nhưng không đủ quyền (ví dụ Customer vào trang Admin): Chuyển hướng về trang 403 Forbidden (Errors/Forbidden).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                // Đã đăng nhập nhưng không có vai trò phù hợp -> 403 Forbidden
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        controller = "Errors",
                        action = "Forbidden"
                    }));
            }
            else
            {
                // Chưa đăng nhập -> redirect đến trang Login mặc định
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}
