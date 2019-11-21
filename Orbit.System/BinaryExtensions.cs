using System;

namespace Orbit
{
    public static class BinaryExtensions
    {
        
        public static string Base64Encode(this byte[] bytes, bool webSafe = false)
        {
            var res = Convert.ToBase64String(bytes);

            if (webSafe) res = res.Replace('+', '-').Replace('/', '_').Replace('=', '~');

            return res;
        }

        public static byte[] Base64Decode(this string data)
        {
            return Convert.FromBase64String(data.Replace('-', '+').Replace('_', '/').Replace('~', '='));
        }

    }
}