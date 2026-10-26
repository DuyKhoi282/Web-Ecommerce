using System;
using System.Collections.Generic;

namespace WebEcommerce.Helpers
{
    /// <summary>
    /// Đọc số tiền bằng chữ tiếng Việt cho dòng "Bằng chữ" trên hóa đơn.
    /// Ví dụ: 8993000 → "Tám triệu chín trăm chín mươi ba nghìn đồng".
    /// </summary>
    public static class VietnameseNumberHelper
    {
        private static readonly string[] Digits = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        private static readonly string[] Units = { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ" };

        public static string ToCurrencyText(decimal amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền không được âm.");

            var number = (long)Math.Round(amount, MidpointRounding.AwayFromZero);
            if (number == 0) return "Không đồng";

            // Tách thành các nhóm 3 chữ số, nhóm 0 là hàng đơn vị
            var groups = new List<int>();
            for (var n = number; n > 0; n /= 1000) groups.Add((int)(n % 1000));

            var words = new List<string>();
            for (var i = groups.Count - 1; i >= 0; i--)
            {
                if (groups[i] == 0) continue;
                var isLeading = i == groups.Count - 1;
                words.Add(ReadGroup(groups[i], isLeading));
                if (Units[i].Length > 0) words.Add(Units[i]);
            }

            var text = string.Join(" ", words);
            return char.ToUpper(text[0]) + text.Substring(1) + " đồng";
        }

        // Nhóm đứng sau nhóm khác phải đọc đủ "không trăm" (vd 1.005.000 → "một triệu không trăm linh năm nghìn")
        private static string ReadGroup(int group, bool isLeading)
        {
            int hundreds = group / 100, tens = group / 10 % 10, ones = group % 10;
            var parts = new List<string>();
            var readHundreds = !isLeading || hundreds > 0;

            if (readHundreds) parts.Add(Digits[hundreds] + " trăm");

            if (tens == 0)
            {
                if (ones > 0) parts.Add(readHundreds ? "linh " + Digits[ones] : Digits[ones]);
            }
            else if (tens == 1)
            {
                parts.Add("mười");
                if (ones > 0) parts.Add(ones == 5 ? "lăm" : Digits[ones]);
            }
            else
            {
                parts.Add(Digits[tens] + " mươi");
                if (ones == 1) parts.Add("mốt");
                else if (ones == 4) parts.Add("tư");
                else if (ones == 5) parts.Add("lăm");
                else if (ones > 0) parts.Add(Digits[ones]);
            }

            return string.Join(" ", parts);
        }
    }
}
