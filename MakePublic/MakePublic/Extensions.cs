using System;
using System.Collections.Generic;
using System.Linq;

namespace SetAccessibility
{
    internal static class Extensions
    {
        internal static IEnumerable<TSource> Exclude<TSource>(
            this IEnumerable<TSource> source, 
            IEnumerable<TSource> toExclude, 
            Func<TSource, TSource, bool> comparer)
        {
            if (toExclude == null || comparer == null)
            {
                return source;
            }

            return source.Where(m => !toExclude.Any(e => comparer(e, m)));
        }
    }
}
