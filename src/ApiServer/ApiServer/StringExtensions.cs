using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WomPlatform.Web.Api {

    public static class StringExtensions {

        public static string ToBase64(this byte[] b) {
            return Convert.ToBase64String(b, Base64FormattingOptions.None);
        }

        public static byte[] FromBase64(this string s) {
            return Convert.FromBase64String(s);
        }

        private static readonly uint[] _hexLookup = Enumerable.Range(0, 256).Select(i => {
            string s = i.ToString("X2");
            return s[0] + ((uint)s[1] << 16);
        }).ToArray();

        public static string ToHexString(this byte[] bytes) {
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++) {
                var val = _hexLookup[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        public static byte[] ToBytes(this string s) {
            return Encoding.UTF8.GetBytes(s);
        }

        public static string AsUtf8String(this byte[] b) {
            return Encoding.UTF8.GetString(b);
        }

        public static bool LengthIsBetween(this string s, int min, int max) {
            if (s == null)
                return false;

            return s.Length >= min && s.Length <= max;
        }

        private static readonly Regex _regexDuplicateDash = new("-+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string ToCleanUrl(this string s) {
            if(string.IsNullOrEmpty(s)) {
                return s;
            }

            static char ConvertChar(char c) {
                return CharUnicodeInfo.GetUnicodeCategory(c) switch {
                    // These are kept as-is
                    UnicodeCategory.DecimalDigitNumber => c,
                    UnicodeCategory.LowercaseLetter => c,
                    UnicodeCategory.UppercaseLetter => c,
                    UnicodeCategory.SpaceSeparator => '-',

                    // Punctuation
                    UnicodeCategory.ClosePunctuation => '-',
                    UnicodeCategory.ConnectorPunctuation => '-',
                    UnicodeCategory.DashPunctuation => '-',
                    UnicodeCategory.FinalQuotePunctuation => '-',
                    UnicodeCategory.InitialQuotePunctuation => '-',
                    UnicodeCategory.OpenPunctuation => '-',
                    UnicodeCategory.OtherPunctuation => '-',

                    // Modifiers and others to remove
                    UnicodeCategory.ModifierLetter => '?',
                    UnicodeCategory.ModifierSymbol => '?',
                    UnicodeCategory.NonSpacingMark => '?',
                    _ => '?',
                };
            }

            var output = new StringBuilder(s.Length);
            foreach(var c in s.Normalize(NormalizationForm.FormD)) {
                output.Append(ConvertChar(c));
            }

            var modifiersRemoved = output.Replace("?", null).ToString();
            var spacesFixed = _regexDuplicateDash.Replace(modifiersRemoved, "-");

            return spacesFixed.ToLowerInvariant().Trim('-');
        }

    }

}
