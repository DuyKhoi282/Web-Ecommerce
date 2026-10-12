using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace WebEcommerce
{
    public class DecimalModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueResult == null || string.IsNullOrWhiteSpace(valueResult.AttemptedValue))
            {
                return null;
            }

            var val = valueResult.AttemptedValue.Trim().Replace("₫", "").Replace("VND", "").Trim();
            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal actualValue))
            {
                return actualValue;
            }
            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.CurrentCulture, out actualValue))
            {
                return actualValue;
            }
            val = val.Replace(",", ".");
            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out actualValue))
            {
                return actualValue;
            }

            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Giá trị không hợp lệ.");
            return 0m;
        }
    }

    public class NullableDecimalModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueResult == null || string.IsNullOrWhiteSpace(valueResult.AttemptedValue))
            {
                return null;
            }

            var val = valueResult.AttemptedValue.Trim().Replace("₫", "").Replace("VND", "").Trim();
            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal actualValue))
            {
                return actualValue;
            }
            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.CurrentCulture, out actualValue))
            {
                return actualValue;
            }
            val = val.Replace(",", ".");
            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out actualValue))
            {
                return actualValue;
            }

            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Giá trị không hợp lệ.");
            return null;
        }
    }

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            System.Web.Helpers.AntiForgeryConfig.UniqueClaimTypeIdentifier = System.Security.Claims.ClaimTypes.NameIdentifier;
            ModelBinders.Binders.Add(typeof(decimal), new DecimalModelBinder());
            ModelBinders.Binders.Add(typeof(decimal?), new NullableDecimalModelBinder());
            DatabaseConfig.Initialize();
        }
    }
}

