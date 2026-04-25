using System.Collections.Generic;

namespace SurvivalGame.Core.Utilities
{
    public static class CollectionExtensions
    {
        public static T GetRandom<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                return default;
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }

        public static T AddIfNotExists<T>(this IList<T> list, T item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
            }
            return item;
        }

        public static void RemoveAllNull<T>(this IList<T> list) where T : class
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null)
                {
                    list.RemoveAt(i);
                }
            }
        }
    }
}
