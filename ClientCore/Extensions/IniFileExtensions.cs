using System.Collections.Generic;
using System.Linq;

using Rampastring.Tools;

namespace ClientCore.Extensions
{
    public static class IniFileExtensions
    {
        extension(IniFile iniFile)
        {
            // Clone() method is not officially available now. https://github.com/Rampastring/Rampastring.Tools/issues/12
            public IniFile Clone()
            {
                var newIni = new IniFile();
                foreach (string sectionName in iniFile.GetSections())
                {
                    IniSection oldSection = iniFile.GetSection(sectionName);
                    newIni.AddSection(oldSection.Clone());
                }

                return newIni;
            }

            public IniSection GetOrAddSection(string sectionName)
            {
                var section = iniFile.GetSection(sectionName);
                if (section != null)
                    return section;

                section = new IniSection(sectionName);
                iniFile.AddSection(section);
                return section;
            }

            public string[] GetStringListValue(string section, string key, string defaultValue, char[] separators = null)
                => (iniFile.GetSection(section)?.GetStringValue(key, defaultValue) ?? defaultValue)
                    .SplitWithCleanup(separators);

        }

        extension(IniSection iniSection)
        {
            public IniSection Clone()
            {
                IniSection newSection = new(iniSection.SectionName);

                foreach ((var key, var value) in iniSection.Keys)
                {
                    newSection.AddKey(key, value);
                }

                return newSection;
            }

            public void RemoveAllKeys()
            {
                var keys = new List<KeyValuePair<string, string>>(iniSection.Keys);
                foreach (KeyValuePair<string, string> iniSectionKey in keys)
                    iniSection.RemoveKey(iniSectionKey.Key);
            }
        }
    }
}
