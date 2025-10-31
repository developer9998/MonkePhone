using System;
using System.Collections.Generic;
using System.Linq;

namespace MonkePhone.Extensions;

public static class ListEx
{
    public static string ListElements<T>(this IEnumerable<T> collection)
    {
        if (collection == null)
            throw new ArgumentNullException("collection");

        if (collection.Count() == 1)
            return collection.First().ToString();

        if (collection.Count() == 2)
            return string.Join(" and ", collection);

        string[] strings = collection.Select(element => element.ToString()).ToArray();
        strings[^1] = string.Concat("and ", strings[^1]);

        return string.Join(", ", strings);
    }
}