namespace WebEcommerce.Helpers
{
    /// <summary>
    /// Represents a single item stored in the cart cookie.
    /// This is a lightweight DTO — it only holds ProductID and Quantity.
    /// Product details (name, price, image…) are loaded from the DB when needed.
    /// </summary>
    public class CookieCartItem
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
    }
}
