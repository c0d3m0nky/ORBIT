using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Orbit
{
    public static class ObjectExtensions
    {
        
        // Based on Resharper's generated HashCode algorithm
        public static int ComputeHashCode(this IEnumerable<PropertyInfo> props, object obj)
        {
            unchecked
            {
                if (obj == null) return 0;

                return props.Aggregate(0, (h, p) =>
                {
                    var v = p.GetValue(obj) ?? 0;

                    return (h * 397) ^ (v is int vi ? vi : v.GetHashCode());
                });
            }
        }

    }
}