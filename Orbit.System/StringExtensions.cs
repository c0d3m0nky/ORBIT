using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Orbit
{
    public static class StringExtensions
    {
        // Split string into strings of a max size
        public static IEnumerable<string> Split(this string str, int size)
        {
            var skip = 0;
            var res = new List<string>();
            var cstr = str.Select(c => c.ToString()).ToArray();

            while (skip < str.Length)
            {
                res.Add(cstr.Skip(skip).Take(size).Join(""));
                skip += size;
            }

            return res;
        }

        public static string Join(this IEnumerable<string> coll, string separator) => string.Join(separator, coll.ToArray());

        public static bool IsNullOrWhitespace(this string str) => string.IsNullOrWhiteSpace(str?.Trim());

        public static string IfNullOrWhitespace(this string source, string ifEmpty) => source.IsNullOrWhitespace() ? ifEmpty : source;

        public static bool Contains(this string source, string value, StringComparison comparisonType) => source.IndexOf(value, comparisonType) > -1;

        public static DateTime? ParseDate(this string str) => DateTime.TryParse(str, out var i) ? i : (DateTime?) null;

        public static DateTime? ParseDate(this string str, string format, IFormatProvider provider = null, DateTimeStyles style = DateTimeStyles.AssumeLocal) =>
            DateTime.TryParseExact(str, format, provider ?? CultureInfo.CurrentCulture, style, out var i) ? i : (DateTime?) null;

        public static long? ParseLong(this string str) => long.TryParse(str, out var i) ? i : (long?) null;

        public static int? ParseInt(this string str) => int.TryParse(str, out var i) ? i : (int?) null;

        public static Guid? ParseGuid(this string str) => Guid.TryParse(str, out var i) ? i : (Guid?) null;

        public static uint? ParseUInt(this string str) => uint.TryParse(str, out var i) ? i : (uint?) null;

        public static bool? ParseBool(this string str)
        {
            if (str.IsNullOrWhitespace()) return null;

            if (!bool.TryParse(str, out var i))
            {
                str = str.ToLower();

                if (str == "1" || str == "true" || str == "t" || str == "y" || str == "yes")
                {
                    return true;
                }

                if (str == "0" || str == "false" || str == "f" || str == "n" || str == "no")
                {
                    return false;
                }

                return null;
            }

            return i;
        }

        public static Uri ParseWebUrl(this string str, UriKind kind = UriKind.Absolute)
        {
            if (str.IsNullOrWhitespace())
            {
                return null;
            }

            str = str.Trim();

            if (kind == UriKind.Absolute && !str.StartsWith("http"))
            {
                str = "http://" + str;
            }

            Uri.TryCreate(str, UriKind.Absolute, out var u);

            return u;
        }

        public static object ParseBestGuess(this string rawValue)
        {
            if (rawValue == null) return null;
            if (rawValue == "null" || rawValue == "undefined") return null;
            if (rawValue.StartsWith("\"") && rawValue.EndsWith("\"")) return rawValue.Trim('"');
            if (int.TryParse(rawValue, out var i)) return i;
            if (double.TryParse(rawValue, out var d)) return d;
            if (long.TryParse(rawValue, out var l)) return l;
            if (bool.TryParse(rawValue, out var b)) return b;
            if (DateTime.TryParse(rawValue, out var dt)) return dt;
            if (Guid.TryParse(rawValue, out var g)) return g;

            return rawValue;
        }

        public static bool IsMatch(this string str, Regex rx)
        {
            if (str.IsNullOrWhitespace())
            {
                return false;
            }

            var m = rx.Match(str);

            return m.Success;
        }

        public static string Match(this string str, Regex rx)
        {
            var m = rx.Match(str);

            return m.Success ? m.Value : null;
        }

        public static string Match(this string str, Regex rx, string groupName)
        {
            var m = rx.Match(str);

            return m.Success ? m.Groups[groupName]?.Value : null;
        }

        public static IEnumerable<string> Matches(this string str, Regex rx) => rx.Matches(str).Cast<Match>().Select(m => m.Value);

    }
}