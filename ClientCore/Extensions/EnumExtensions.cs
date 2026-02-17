using System;
using System.Linq;
using System.Text;

namespace ClientCore.Extensions
{
    public static class EnumExtensions
    {
        public static T CycleNext<T>(this T src) where T : Enum
        {
            T[] values = EnumExtensions.GetValues<T>();
            return values[(Array.IndexOf(values, src) + 1) % values.Length];
        }

        public static T First<T>() where T : Enum 
            => EnumExtensions.GetValues<T>()[0];

        public static string GetNames<T>() where T : Enum
            => string.Join(", ", EnumExtensions.GetValues<T>().Select(e => e.ToString()));

        private static T[] GetValues<T>() where T : Enum 
            => (T[])Enum.GetValues(typeof(T));
    }
}
