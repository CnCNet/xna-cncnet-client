using System.Collections.Generic;
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
    }
}
