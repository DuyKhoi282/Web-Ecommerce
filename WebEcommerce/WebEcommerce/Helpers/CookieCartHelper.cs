using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebEcommerce.Helpers
{
    /// <summary>
    /// Helper class for reading and writing cart data to/from an HTTP cookie.
    /// The cookie stores a JSON array of <see cref="CookieCartItem"/> objects.
    /// Cookie name: "ShoppingCart", expires in 30 days, HttpOnly for security.
    /// </summary>
    public static class CookieCartHelper
    {
        private const string CookieName = "ShoppingCart";
        private const int CookieExpiryDays = 30;

        // ==================================================
        // READ: Get the cart items list from the cookie
        // ==================================================
        public static List<CookieCartItem> GetCartItems(HttpRequestBase request)
        {
            var cookie = request.Cookies[CookieName];

            if (cookie == null || string.IsNullOrWhiteSpace(cookie.Value))
            {
                return new List<CookieCartItem>();
            }

            try
            {
                var decoded = HttpUtility.UrlDecode(cookie.Value);
                var items = JsonConvert.DeserializeObject<List<CookieCartItem>>(decoded);
                return items ?? new List<CookieCartItem>();
            }
            catch
            {
                // If the cookie is corrupted, return empty list
                return new List<CookieCartItem>();
            }
        }

        // ==================================================
        // WRITE: Save the cart items list into the cookie
        // ==================================================
        public static void SaveCartItems(
            HttpResponseBase response,
            List<CookieCartItem> items)
        {
            var json = JsonConvert.SerializeObject(items);
            var encoded = HttpUtility.UrlEncode(json);

            var cookie = new HttpCookie(CookieName, encoded)
            {
                Expires = System.DateTime.Now.AddDays(CookieExpiryDays),
                HttpOnly = true,
                Path = "/"
            };

            response.Cookies.Set(cookie);
        }

        // ==================================================
        // ADD: Add a product or increase its quantity
        // ==================================================
        public static void AddItem(
            HttpRequestBase request,
            HttpResponseBase response,
            int productId,
            int quantity)
        {
            var items = GetCartItems(request);
            var existing = items.FirstOrDefault(i => i.ProductID == productId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                items.Add(new CookieCartItem
                {
                    ProductID = productId,
                    Quantity = quantity
                });
            }

            SaveCartItems(response, items);
        }

        // ==================================================
        // UPDATE: Set the exact quantity for a product
        // ==================================================
        public static void UpdateQuantity(
            HttpRequestBase request,
            HttpResponseBase response,
            int productId,
            int newQuantity)
        {
            var items = GetCartItems(request);
            var existing = items.FirstOrDefault(i => i.ProductID == productId);

            if (existing != null)
            {
                existing.Quantity = newQuantity;
                SaveCartItems(response, items);
            }
        }

        // ==================================================
        // REMOVE: Remove a product from the cart
        // ==================================================
        public static void RemoveItem(
            HttpRequestBase request,
            HttpResponseBase response,
            int productId)
        {
            var items = GetCartItems(request);
            items.RemoveAll(i => i.ProductID == productId);
            SaveCartItems(response, items);
        }

        // ==================================================
        // CLEAR: Remove the entire cart cookie
        // ==================================================
        public static void ClearCart(HttpResponseBase response)
        {
            var cookie = new HttpCookie(CookieName, "")
            {
                Expires = System.DateTime.Now.AddDays(-1),
                HttpOnly = true,
                Path = "/"
            };

            response.Cookies.Set(cookie);
        }

        // ==================================================
        // COUNT: Get total quantity of all items
        // ==================================================
        public static int GetTotalQuantity(HttpRequestBase request)
        {
            var items = GetCartItems(request);
            return items.Sum(i => i.Quantity);
        }
    }
}
