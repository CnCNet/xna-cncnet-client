using System;
using System.Linq;
using System.Text;

namespace ClientCore.Extensions
{
    public static class EnumExtensions
    {
        public static T Next<T>(this T src) where T : Enum
        {
            T[] Arr = GetValues(src);
            int nextIndex = Array.IndexOf(Arr, src) + 1;
            return Arr.Length == nextIndex ? Arr[0] : Arr[nextIndex];
        }

        public static T First<T>(this T src) where T : Enum
        {
            return GetValues(src)[0];
        }

        public static string GetNames<T>(this T src) where T : Enum
        {
            StringBuilder sb = new StringBuilder();

            GetValues(src).ToList().ForEach(elem => sb.Append(elem.ToString()).Append(", "));

            return sb.Remove(sb.Length - 2, 2).ToString();
        }

        private static T[] GetValues<T>(T src) where T : Enum
        {
            return (T[])Enum.GetValues(src.GetType());
        }
    }
}
