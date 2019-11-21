using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// ReSharper disable CheckNamespace

namespace Orbit.Experimental
{
    public static class ReflectionExtensions
    {
        public static IEnumerable<Type> ImplementedBy(this Type baseType)
            => baseType.ImplementedBy(new[] {baseType.Assembly});

        public static IEnumerable<Type> ImplementedBy(this Type baseType, Assembly assembly)
            => baseType.ImplementedBy(new[] {assembly});

        public static IEnumerable<Type> ImplementedBy(this Type baseType, IEnumerable<Assembly> inAssemblies)
            => inAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t != baseType && baseType.IsAssignableFrom(t));

    }

}