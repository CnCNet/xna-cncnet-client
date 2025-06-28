#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using Rampastring.Tools;

namespace ClientCore.Extensions
{
    public static class IniFileExtensions
    {
        public static IniSection GetOrAddSection(this IniFile iniFile, string sectionName)
        {
            var section = iniFile.GetSection(sectionName);
            if (section != null)
                return section;

            section = new IniSection(sectionName);
            iniFile.AddSection(section);
            return section;
        }

        public static void RemoveAllKeys(this IniSection iniSection)
        {
            var keys = new List<KeyValuePair<string, string>>(iniSection.Keys);
            foreach (KeyValuePair<string, string> iniSectionKey in keys)
                iniSection.RemoveKey(iniSectionKey.Key);
        }

        public static string[] GetStringListValue(this IniFile iniFile, string section, string key, string defaultValue, char[]? separators = null)
        {
            separators ??= [','];
            IniSection iniSection = iniFile.GetSection(section);

            return (iniSection?.GetStringValue(key, defaultValue) ?? defaultValue)
                .Split(separators)
                .ToList()
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }
        
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
