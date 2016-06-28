using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ClientCore
{
    public class CCIniFile : IniFile
    {
        public CCIniFile(string path) : base(path)
        {

        }

        protected override void ApplyBaseIni()
        {
            string basedOn = GetStringValue("INISystem", "BasedOn", String.Empty);
            if (!String.IsNullOrEmpty(basedOn))
            {
                string path = String.Empty;
                if (basedOn.Contains("$THEME_DIR$"))
                {
                    path = basedOn.Replace("$THEME_DIR$\\", ProgramConstants.GetResourcePath());
                }
                else
                    path = Path.GetDirectoryName(FileName) + "\\" + basedOn;

                // Consolidate with the INI file that this INI file is based on
                if (!File.Exists(path))
                    Logger.Log(FileName + ": Base INI file not found! " + path);

                CCIniFile baseIni = new CCIniFile(path);
                ConsolidateIniFiles(baseIni, this);
                this.Sections = baseIni.Sections;
            }
        }
    }
}
