using Rampastring.Tools;
using System.IO;

namespace ClientCore
{
    public class CCIniFile : IniFile
    {
        public CCIniFile(string path) : base(path)
        {

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
