using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Rampastring.Tools;

namespace ClientCore.Extensions
{
    public static class IniFileExtensions
    {
        // Clone() method is not officially available now. https://github.com/Rampastring/Rampastring.Tools/issues/12
        public static IniFile Clone(this IniFile oldIniFile)
        {
            var newIni = new IniFile();
            foreach (string sectionName in oldIniFile.GetSections())
            {
                IniSection oldSection = oldIniFile.GetSection(sectionName);
                newIni.AddSection(oldSection.Clone());
            }

            return newIni;
        }

        public static IniSection Clone(this IniSection oldSection)
        {
            IniSection newSection = new(oldSection.SectionName);

            foreach ((var key, var value) in oldSection.Keys)
            {
                newSection.AddKey(key, value);
            }

            return newSection;
        }

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
            => (iniFile.GetSection(section)?.GetStringValue(key, defaultValue) ?? defaultValue)
                .SplitWithCleanup();
    }
}
