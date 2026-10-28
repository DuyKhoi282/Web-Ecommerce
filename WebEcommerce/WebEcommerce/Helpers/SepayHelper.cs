using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Web;

namespace WebEcommerce.Helpers
{
    public static class SepayHelper
    {
        public static string BankName => ConfigurationManager.AppSettings["Sepay:Bank"] ?? "ACB ONE";
        public static string BankCode => ConfigurationManager.AppSettings["Sepay:BankCode"] ?? "ACB";
        public static string AccountNumber => ConfigurationManager.AppSettings["Sepay:AccountNumber"] ?? "22812297";
        public static string AccountName => ConfigurationManager.AppSettings["Sepay:AccountName"] ?? "THE CHILL SHOP";
        public static string ApiKey => ConfigurationManager.AppSettings["Sepay:ApiKey"] ?? "";
        
        public static int PaymentTimeoutMinutes
        {
            get
            {
                var val = ConfigurationManager.AppSettings["Sepay:PaymentTimeoutMinutes"];
                return int.TryParse(val, out int mins) && mins > 0 ? mins : 15;
            }
        }

        /// <summary>
        /// Tạo nội dung chuyển khoản chuẩn cho đơn hàng (ví dụ: DH128)
        /// </summary>
        public static string GetTransferContent(int orderId)
        {
            return $"DH{orderId}";
        }

        /// <summary>
        /// Tạo link mã QR động SePay (tự điền số tiền và cú pháp chuyển tiền)
        /// </summary>
        public static string GetQrUrl(int orderId, decimal amount)
        {
            long roundedAmount = (long)Math.Round(amount);
            string des = GetTransferContent(orderId);
            return $"https://qr.sepay.vn/img?acc={AccountNumber}&bank={BankCode}&amount={roundedAmount}&des={des}";
        }

        /// <summary>
        /// Tạo link mã VietQR dự phòng
        /// </summary>
        public static string GetVietQrFallbackUrl(int orderId, decimal amount)
        {
            long roundedAmount = (long)Math.Round(amount);
            string des = HttpUtility.UrlEncode(GetTransferContent(orderId));
            string encodedName = HttpUtility.UrlEncode(AccountName);
            return $"https://img.vietqr.io/image/{BankCode}-{AccountNumber}-compact2.png?amount={roundedAmount}&addInfo={des}&accountName={encodedName}";
        }

        /// <summary>
        /// Bóc tách mã OrderID từ nội dung giao dịch ngân hàng do SePay Webhook gửi sang
        /// Ví dụ: "DH128", "DH 128", "SEPAY128", "DON128", "128"
        /// </summary>
        public static int? ExtractOrderId(string content, string description = null)
        {
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(description))
                return null;

            string fullText = $"{content} {description}".ToUpperInvariant();

            // 1. Ưu tiên tìm cú pháp chuẩn: DH{ID} hoặc SEPAY{ID} hoặc DON{ID}
            var match = Regex.Match(fullText, @"(?:DH|SEPAY|DON|ORDER)\s*([0-9]{1,9})", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
            {
                return id;
            }

            // 2. Tìm số nguyên độc lập nếu nội dung chỉ có mỗi số
            var pureNumberMatch = Regex.Match(fullText, @"\b([0-9]{1,9})\b");
            if (pureNumberMatch.Success && int.TryParse(pureNumberMatch.Groups[1].Value, out int pureId))
            {
                return pureId;
            }

            return null;
        }
    }
}
