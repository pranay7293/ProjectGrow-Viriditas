using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

namespace ListExtensions
{
    // Additional helpful methods for working with lists and arrays.
    public static class ListExtensions
    {
        private static Random rand = new Random();

        public static T RandomElement<T>(this T[] array)
        {
            return array[rand.Next(array.Length)];
        }

        public static T RandomElement<T>(this IList<T> list)
        {
            return list[rand.Next(list.Count)];
        }

        public static T RemoveRandomElement<T>(this IList<T> list)
        {
            var index = rand.Next(list.Count);
            var element = list[index];
            list.RemoveAt(index);
            return element;
        }

        // Returns a randomized copy of a list.
        public static IList<T> Shuffled<T>(this IList<T> list)
        {
            return list.OrderBy(_ => rand.Next()).ToList();
        }

        // Returns a "Rotated" copy of a list which is shifted by the given offset.
        public static IList<T> Rotated<T>(this IList<T> list, int offset)
        {
            if (offset <= 0 || offset >= list.Count)
            {
                return list.ToList();
            }
            return list.Skip(offset).Concat(list.Take(offset)).ToList();
        }

        public static T Mode<T>(this IEnumerable<T> collection)
        {
            return
                collection
                    .GroupBy(value => value)
                    .OrderByDescending(group => group.Count())
                    .Select(group => group.Key)
                    .First();
        }
    }
}
