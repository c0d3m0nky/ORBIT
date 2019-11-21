using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Orbit {
    public static class CollectionsExtensions
    {
        
        public static bool GetNext<T>(this IEnumerator<T> enumerator, out T current)
        {
            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                return true;
            }

            current = default;

            return false;
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null) yield break;

            var i = 0;

            foreach (var s in source)
            {
                yield return selector(s, i);

                i++;
            }
        }

#if NETSTANDARD2_0 
        // These will be included in NetStandard2.1
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key)
        {
            return dictionary.GetValueOrDefault<TKey, TValue>(key, default(TValue));
        }

        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            TValue obj;
            if (!dictionary.TryGetValue(key, out obj))
                return defaultValue;

            return obj;
        }
#endif

        public static bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> source, Func<KeyValuePair<TKey, TValue>, bool> selector)
        {
            var removals = source.Where(selector).ToArray();

            if (removals.Any())
            {
                removals.ForEach(r=>source.Remove(r.Key));
                
                return true;
            }

            return false;
        }

        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> merge,
            Func<(TKey key, TValue source, TValue merge), TValue> valueSelector)
        {
            if (valueSelector == null) throw new ArgumentException($"{nameof(valueSelector)} cannot be null");

            if (source?.Any() != true) return merge ?? new Dictionary<TKey, TValue>();

            if (merge?.Any() != true) return source;

            var keys = source.Keys.Union(merge.Keys).Distinct();
            var result = new Dictionary<TKey, TValue>();

            foreach (var key in keys)
            {
                TValue val;
                var inSrc = source.ContainsKey(key);
                var inMrg = merge.ContainsKey(key);

                if (inSrc && !inMrg) val = source[key];
                else if (!inSrc && inMrg) val = merge[key];
                else val = valueSelector((key, source[key], merge[key]));

                result[key] = val;
            }

            return result;
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>?> source)
            => source.Where(s => s.HasValue).ToDictionary(s => s.Value.Key, s => s.Value.Value);

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
            => source.ToDictionary(s => s.Key, s => s.Value);

        public static Dictionary<string, string[]> ToDictionary(this NameValueCollection nvc) => nvc.Cast<string>().ToDictionary(k => k, nvc.GetValues);

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey? key) where TKey : struct =>
            key != null && d.ContainsKey(key.Value) ? d[key.Value] : default;

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey? key, TValue defaultValue) where TKey : struct =>
            key != null && d.ContainsKey(key.Value) ? d[key.Value] : defaultValue;

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, int, TKey> keySelector,
            Func<TSource, int, TElement> elementSelector
        ) => source.Select((s, i) => new {s, i}).ToDictionary(p => keySelector(p.s, p.i), p => elementSelector(p.s, p.i));

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> body)
        {
            if (source == null)
            {
                var i = 0;

                foreach (var c in source)
                {
                    body.Invoke(c, i);
                    i++;
                }
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> body)
        {
            if (source != null)
            {
                foreach (var i in source)
                {
                    body.Invoke(i);
                }
            }
        }

        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body)
        {
            if (source != null)
            {
                foreach (var i in source)
                {
                    await body.Invoke(i);
                }
            }
        }

        //https://blogs.msdn.microsoft.com/pfxteam/2012/03/05/implementing-a-simple-foreachasync-part-2/
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }

        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, CancellationToken token, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }, token));
        }

        public static Task<TAccumulate> AggregateAsync<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, Task<TAccumulate>> func) =>
            source.Aggregate(Task.FromResult(seed), async (a, s) => await func(a.Result, s));

        public static Task WhenAll(this IEnumerable<Task> tasks) => Task.WhenAll(tasks);

        public static Task WhenAny(this IEnumerable<Task> tasks) => Task.WhenAny(tasks);

        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> col, Func<T, TKey> keySelector, bool takeLast = false)
        {
            var grp = col.GroupBy(keySelector);

            if (takeLast)
            {
                return grp.Select(g => g.Last());
            }

            return grp.Select(g => g.First());
        }

        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<T> initialSet, params IEnumerable<T>[] sets)
        {
            var finalSet = initialSet.Select(x => new[] {x});

            foreach (var set in sets)
            {
                var cp = finalSet.SelectMany(fs => set, (fs, s) => new {fs, s});

                finalSet = cp.Select(x =>
                {
                    var a = new T[x.fs.Length + 1];

                    x.fs.CopyTo(a, 0);
                    a[x.fs.Length] = x.s;

                    return a;
                });
            }

            return finalSet;
        }

        public static IEnumerable<T> Union<T>(this IEnumerable<T> source, T item) => source.Union(new T[] {item});

        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dic, IEnumerable<(TKey key, TValue value)> collection)
        {
            foreach (var i in collection)
            {
                dic.Add(i.key, i.value);
            }
        }

    }
}