using System;
using System.Linq;
using System.Web;

// ReSharper disable once CheckNamespace
namespace Orbit.Experimental
{
    public static class Extensions
    {
        #region Web

        public static Uri Combine(this Uri baseUri, string path)
        {
            if (baseUri.Scheme.StartsWith("http"))
            {
                var pathUri = new Uri(baseUri, path);
                var q = HttpUtility.ParseQueryString(baseUri.Query);

                q.Add(HttpUtility.ParseQueryString(pathUri.Query));
                var query = q.HasKeys()
                    ? $"?{q.ToDictionary().Select(p => $"{p.Key}={p.Value.LastOrDefault()}").ToArray().Join("&")}"
                    : "";

                return new UriBuilder(baseUri.Scheme, baseUri.Host, baseUri.Port, baseUri.LocalPath + pathUri.LocalPath, query).Uri;
            }

            throw new NotImplementedException();
        }

        #endregion
    }
}