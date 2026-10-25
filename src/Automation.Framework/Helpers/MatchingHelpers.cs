using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Helpers
{
 
        /// <summary>
        /// تطبيع أرقام الهواتف للمقارنة: إزالة كل الرموز وإبقاء الأرقام فقط.
        ///
        /// مثال:
        ///   "+218 91-123-4567" → "218911234567"
        ///   "0915751122"        → "0915751122"
        /// </summary>
        public static class PhoneNormalizer
        {
            public static string Normalize(string? phone)
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return string.Empty;

                return new string(phone.Where(char.IsDigit).ToArray());
            }

            /// <summary>
            /// يقارن رقمَي هاتف بعد التطبيع.
            /// </summary>
            public static bool AreEqual(string? first, string? second)
            {
                var a = Normalize(first);
                var b = Normalize(second);
                return !string.IsNullOrEmpty(a) && a == b;
            }
        }


        /// <summary>
        /// مطابقة الحسابات بمعرّفاتها أو أرقام هواتفها.
        ///
        /// تُستخدم عندما تعيدك الـ API قائمة حسابات، وتريد اختيار
        /// الحساب المطابق لجلستك.
        /// </summary>
        public static class AccountMatcher
        {
            /// <summary>
            /// هل المعرّفان يمثلان نفس القيمة؟ يقبل Guid أو نصًا.
            /// </summary>
            public static bool IdsMatch(string? first, string? second)
            {
                if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
                    return false;

                if (Guid.TryParse(first, out var a) && Guid.TryParse(second, out var b))
                    return a == b;

                return string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            /// <summary>
            /// هل الرقمان هما نفس الهاتف (بعد التطبيع)؟
            /// </summary>
            public static bool PhonesMatch(string? first, string? second)
                => PhoneNormalizer.AreEqual(first, second);
        }
    }
