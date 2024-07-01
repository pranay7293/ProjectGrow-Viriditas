using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

namespace DictionaryExtensions
{
    // Additional helpful methods for working with lists and arrays.
    public static class DictionaryExtensions
    {
        public static void RemoveWhere<TKey, TValue>(this IDictionary<TKey, TValue> dict, Func<TKey, bool> predicate)
        {
            var keysToRemove = dict.Keys.Where(predicate).ToList();
            foreach (var key in keysToRemove)
            {
                dict.Remove(key);
            }
        }
    }
}
