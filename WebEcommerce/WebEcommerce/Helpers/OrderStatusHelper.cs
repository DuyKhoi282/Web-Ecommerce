namespace WebEcommerce.Helpers
{
    /// <summary>
    /// Luật chuyển trạng thái đơn hàng phía Admin (dùng chung cho controller và view).
    /// Luồng thẳng: Pending -> Confirmed -> Processing -> Shipped -> Delivered.
    /// Hủy chỉ khi hàng chưa rời kho (Pending/Confirmed/Processing).
    /// </summary>
    public static class OrderStatusHelper
    {
        public static string GetNextStatus(string status)
        {
            switch (status)
            {
                case "Pending": return "Confirmed";
                case "Confirmed": return "Processing";
                case "Processing": return "Shipped";
                case "Shipped": return "Delivered";
                default: return null;
            }
        }

        public static bool CanCancel(string status)
        {
            return status == "Pending" || status == "Confirmed" || status == "Processing";
        }

        // Đơn chuyển khoản phải nhận tiền trước khi được đi tiếp bất kỳ bước nào
        public static bool NeedsPayment(string paymentMethod, string paymentStatus)
        {
            return paymentMethod == "BankTransfer" && paymentStatus != "Paid";
        }
    }
}
