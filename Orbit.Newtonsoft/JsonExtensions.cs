using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Orbit
{
    public static class JsonExtensions
    {
        public static void Remove(this JArray jArr, Func<JToken, bool> predicate)
            => jArr?
                .Select((t, i) => new {t, i})
                .Where(p => predicate(p.t))
                .Select(p => p.i)
                .ToArray()
                .ForEach((originalIndex, i) => jArr.RemoveAt(originalIndex - i));


        public static void Remove(this JObject jObj, Func<JProperty, bool> predicate)
            => jObj?.Properties()
                .Where(predicate)
                .Select(p => p.Name)
                .ToArray().ForEach(p => jObj.Remove(p));


        public static bool IsEmpty<TJ>(this TJ json) where TJ : JToken
        {
            if (json == null) return true;

            if (json is JArray ja) return !ja.Any();

            if (json is JObject jo2) return !jo2.Properties().Any();

            return json.ToString().IsNullOrWhitespace();
        }

        public static TJ Minify<TJ>(this TJ json, Json.MinifyOptions options) where TJ : JToken
        {
            if (json == null || options.HasFlag(Json.MinifyOptions.None)) return json;

            var removeEmpty = options.HasFlag(Json.MinifyOptions.RemoveEmpty);
            var removeNulls = removeEmpty || options.HasFlag(Json.MinifyOptions.RemoveNulls);
            var removePrivate = options.HasFlag(Json.MinifyOptions.RemovePrivate);

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

        public static string ToString<TJ>(this TJ json, Newtonsoft.Json.Formatting formatting, Json.MinifyOptions options) where TJ : JToken
            => json.Minify(options).ToString(formatting);
    }
}