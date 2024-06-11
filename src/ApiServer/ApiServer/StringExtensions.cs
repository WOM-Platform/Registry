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

        public static string ConcealEmail(this string s) {
            Span<char> concealed = stackalloc char[s.Length];

            int posAt = s.LastIndexOf('@');
            int posDot = s.LastIndexOf('.');
            bool userPartiallyConcealed = posAt >= 3;
            bool domainPartiallyConcealed = (posDot - posAt - 1) > 2;

            for(int i = 0; i < s.Length; ++i) {
                if(i < posAt) {
                    if(userPartiallyConcealed && (i == 0 || i >= posAt - 1)) {
                        concealed[i] = s[i];
                    }
                    else {
                        concealed[i] = '*';
                    }
                }
                else if(i > posAt && i < posDot) {
                    if(domainPartiallyConcealed && i == posAt + 1) {
                        concealed[i] = s[i];
                    }
                    else {
                        concealed[i] = '*';
                    }
                }
                else {
                    concealed[i] = s[i];
                }
            }

            return new string(concealed);
        }

        public static string Conceal(this string s) {
            return new string('*', s.Length);
        }

        /// <summary>
        /// Optionally concatenates two strings, if both are not null.
        /// </summary>
        public static string OptConcat(this string left, string right, string separator = " ") {
            if(string.IsNullOrEmpty(left)) {
                return null;
            }

            if(string.IsNullOrEmpty(right)) {
                return left;
            }

            return string.Concat(left, separator, right);
        }

        /// <summary>
        /// Checks whether a string equals another string with lax comparison for user-supplied codes.
        /// </summary>
        public static bool EqualsToCode(this string s, string code) {
            return (s ?? string.Empty).Trim().Equals((code ?? string.Empty).Trim(), StringComparison.InvariantCultureIgnoreCase);
        }

        private static Regex RegexCodeCleaner = new("[^\\da-zA-Z]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Normalizes a user-supplied code.
        /// </summary>
        public static string NormalizeCode(this string s) {
            if(string.IsNullOrWhiteSpace(s)) {
                return null;
            }

            return RegexCodeCleaner.Replace(s ?? string.Empty, string.Empty).ToUpperInvariant();
        }

    }

}
