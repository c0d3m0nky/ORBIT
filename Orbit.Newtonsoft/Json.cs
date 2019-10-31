using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Orbit
{
    public static class Json
    {
        [Flags]
        public enum MinifyOptions { None = 1, RemoveNulls = 2, RemoveEmpty = 4, RemovePrivate = 8 }

        public static JObject EmptyObject = JObject.Parse("{}");
        public static JArray EmptyArray = JArray.Parse("[]");

        public static JToken TryParse(string json) => TryParse<JToken>(json);

        public static TJ TryParse<TJ>(string str) where TJ : JToken
        {
            try
            {
                return JToken.Parse(str) as TJ;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool TryParse<TJ>(string json, out TJ j) where TJ : JToken
        {
            j = null;

            try
            {
                j = JToken.Parse(json) as TJ;
                return j != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static JValue Parse(string rawValue)
        {
            if (rawValue == null) return null;
            if (rawValue == "null" || rawValue == "undefined") return null;
            if (rawValue.StartsWith("\"") && rawValue.EndsWith("\"")) return new JValue(rawValue.Trim('"'));
            if (int.TryParse(rawValue, out var i)) return new JValue(i);
            if (double.TryParse(rawValue, out var d)) return new JValue(d);
            if (long.TryParse(rawValue, out var l)) return new JValue(l);
            if (bool.TryParse(rawValue, out var b)) return new JValue(b);
            if (DateTime.TryParse(rawValue, out var dt)) return new JValue(dt);
            if (Guid.TryParse(rawValue, out var g)) return new JValue(g);
            
            return new JValue(rawValue);
        }
        
        #region ExtensionMethods

        public static bool IsEmpty<TJ>(this TJ json) where TJ : JToken
        {
            if (json == null) return true;

            if (json is JArray ja) return !ja.Any();

            if (json is JObject jo2) return !jo2.Properties().Any();

            return json.ToString().IsNullOrWhitespace();
        }

        public static TJ Minify<TJ>(this TJ json, MinifyOptions options) where TJ : JToken
        {
            if (json == null || options.HasFlag(MinifyOptions.None)) return json;

            var removeEmpty = options.HasFlag(MinifyOptions.RemoveEmpty);
            var removeNulls = removeEmpty || options.HasFlag(MinifyOptions.RemoveNulls);
            var removePrivate = options.HasFlag(MinifyOptions.RemovePrivate);

            if (json is JArray ja)
            {
                var minified = new JArray();

                foreach (var j in ja)
                {
                    var jmin = j.Minify(options);
                    var keep = !(removeNulls && jmin == null) && !(removeEmpty && j.IsEmpty());

                    if (keep) minified.Add(j.Minify(options));
                }

                return minified as TJ;
            }

            if (json is JObject jo)
            {
                var minified = new JObject();

                foreach (var p in jo.Properties())
                {
                    var pmin = p.Value.Minify(options);
                    var keep = !(removeNulls && pmin == null) && !(removeEmpty && pmin.IsEmpty()) && !(removePrivate && p.Name.StartsWith("_"));

                    if (keep) minified[p.Name] = pmin;
                }

                return minified as TJ;
            }

            return json.DeepClone() as TJ;
        }

        public static string ToString<TJ>(this TJ json, Newtonsoft.Json.Formatting formatting, MinifyOptions options) where TJ : JToken
            => json.Minify(options).ToString(formatting);

        #endregion

    }
}