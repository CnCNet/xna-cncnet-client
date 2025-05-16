#nullable enable
using System;
using System.Collections.Generic;

using Rampastring.Tools;

namespace ClientCore.Extensions
{
    public static class RampastringToolsExtensions
    {
        public static string? GetStringValueOrNull(this IniSection section, string key) =>
            section.KeyExists(key) ? section.GetStringValue(key, string.Empty) : null;

        public static int? GetIntValueOrNull(this IniSection section, string key) =>
            section.KeyExists(key) ? section.GetIntValue(key, 0) : null;

        public static bool? GetBooleanValueOrNull(this IniSection section, string key) =>
            section.KeyExists(key) ? section.GetBooleanValue(key, false) : null;

        public static List<T>? GetListValueOrNull<T>(this IniSection section, string key, char separator, Func<string, T> converter) =>
            section.KeyExists(key) ? section.GetListValue<T>(key, separator, converter) : null;


    }
}
