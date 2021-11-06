using Rampastring.Tools;
using System.IO;

namespace ClientCore
{
    public class CCIniFile : IniFile
    {
        public CCIniFile(string path) : base(path)
        {
            foreach (IniSection section in Sections)
            {
                string baseSectionName = section.GetStringValue("$BaseSection", null);

                if (string.IsNullOrWhiteSpace(baseSectionName))
                    continue;

                var baseSection = Sections.Find(s => s.SectionName == baseSectionName);
                if (baseSection == null)
                {
                    Logger.Log($"Base section not found in INI file {path}, section {section.SectionName}, base section name: {baseSectionName}");
                    continue;
                }

                int addedKeyCount = 0;

                foreach (var kvp in baseSection.Keys)
                {
                    if (!section.KeyExists(kvp.Key))
                    {
                        section.Keys.Insert(addedKeyCount, kvp);
                        addedKeyCount++;
                    }
                }
            }
        }

        protected override void ApplyBaseIni()
        {
            string basedOn = GetStringValue("INISystem", "BasedOn", string.Empty);
            if (!string.IsNullOrEmpty(basedOn))
            {
                string path;
                if (basedOn.Contains("$THEME_DIR$"))
                    path = basedOn.Replace("$THEME_DIR$", ProgramConstants.GetResourcePath().TrimEnd(new char[] { '/', '\\' }));
                else
                    path = Path.GetDirectoryName(FileName) + "/" + basedOn;

                // Consolidate with the INI file that this INI file is based on
                if (!File.Exists(path))
                    Logger.Log(FileName + ": Base INI file not found! " + path);

                CCIniFile baseIni = new CCIniFile(path);
                ConsolidateIniFiles(baseIni, this);
                Sections = baseIni.Sections;
            }
        }
    }
}
