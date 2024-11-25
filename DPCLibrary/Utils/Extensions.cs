using System;
using System.Collections.Generic;

namespace DPCLibrary.Utils
{
    //Taken from https://www.techiedelight.com/split-string-into-chunks-csharp/
    public static class Extensions
    {
        //Split string into a number of strings based on a fixed length specified as stringLength
        public static IEnumerable<string> SplitByLength(this string str, int stringLength)
        {
            //By Default return anything left if not matching an exact split
            return SplitByLength(str, stringLength, false);
        }

        public static IEnumerable<string> SplitByLength(this string str, int stringLength, bool exactSize)
        {
            if (string.IsNullOrEmpty(str) || stringLength < 1)
            {
                throw new ArgumentException();
            }

            for (int i = 0; i < str.Length; i += stringLength)
            {
                if (exactSize)
                {
                    yield return str.Substring(i, stringLength);
                }
                else
                {
                    yield return str.Substring(i, Math.Min(stringLength, str.Length - i));
                }
            }
        }

        public static bool EqualsArray<T>(this IList<T> item, IList<T> other)
        {
            if (item == null && other == null)
            {
                return true;
            }
            else if (item == null && other != null)
            {
                //Treat count = 0 and null as equal
                return other.Count == 0;
            }
            else if (item != null && other == null)
            {
                //Treat count = 0 and null as equal
                return item.Count == 0;
            }
            else
            {
                //Both arrays are not null so sequential checking can be done
                //return Enumerable.SequenceEqual(item, other);
                return item.Count == other.Count && new HashSet<T>(item).SetEquals(other);
            }
        }

        public static bool EqualsString(this string item, string other)
        {
            //if both strings are kind of empty
            if (string.IsNullOrWhiteSpace(item) && string.IsNullOrWhiteSpace(other))
            {
                return true;
            }
            //Do full case insensitive check
            return string.Equals(item, other, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
