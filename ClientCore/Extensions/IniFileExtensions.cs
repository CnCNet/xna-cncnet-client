using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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

        public static string[] GetStringListValue(this IniFile iniFile, string section, string key, string defaultValue, char[] separators = null)
        {
            separators ??= new char[] { ',' };
            IniSection iniSection = iniFile.GetSection(section);

            return (iniSection == null ? defaultValue : iniSection.GetStringValue(key, string.Empty))
                .Split(separators)
                .ToList()
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }
    }
}
