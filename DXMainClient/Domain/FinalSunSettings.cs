using System.IO;
using Rampastring.Tools;
using ClientCore;
using System.Text;

namespace DTAClient.Domain
{
    public static class FinalSunSettings
    {
        /// <summary>
        /// Checks for the existence of the FinalSun settings file and writes it if it doesn't exist.
        /// </summary>
        public static void WriteFinalSunIni()
        {
            // the encoding of the FinalSun/FinalAlert ini file should be ANSI instead of UTF-8. Otherwise, the map editor will not work in a non-ASCII path. ANSI doesn't mean a specific codepage, it means the default non-Unicode codepage which can be changed from Control Panel.
            try
            {
                string finalSunIniPath = ClientConfiguration.Instance.FinalSunIniPath;
                var finalSunIniFile = new FileInfo(Path.Combine(ProgramConstants.GamePath, finalSunIniPath));
#if NET48
                var encoding1252 = Encoding.GetEncoding(1252);
#else
                var encoding1252 = CodePagesEncodingProvider.Instance.GetEncoding(1252);
#endif

                Logger.Log("Checking for the existence of FinalSun.ini.");
                if (finalSunIniFile.Exists)
                {
                    Logger.Log("FinalSun settings file exists.");

                    IniFile iniFile = new IniFile();
                    iniFile.FileName = finalSunIniFile.FullName;
                    iniFile.Encoding = encoding1252;
                    iniFile.Parse();
                    
                    iniFile.SetStringValue("FinalSun", "Language", "English");
                    iniFile.SetStringValue("FinalSun", "FileSearchLikeTS", "yes");
                    iniFile.SetStringValue("TS", "Exe", ProgramConstants.GamePath.Replace('/', '\\'));
                    iniFile.WriteIniFile();

                    return;
                }

                Logger.Log("FinalSun.ini doesn't exist - writing default settings.");

                if (!finalSunIniFile.Directory.Exists)
                    finalSunIniFile.Directory.Create();

                using var sw = new StreamWriter(finalSunIniFile.FullName, false, encoding1252);

                sw.WriteLine("[FinalSun]");
                sw.WriteLine("Language=English");
                sw.WriteLine("FileSearchLikeTS=yes");
                sw.WriteLine("");
                sw.WriteLine("[TS]");
                sw.WriteLine("Exe=" + ProgramConstants.GamePath.Replace('/', '\\'));
                sw.WriteLine("");
                sw.WriteLine("[UserInterface]");
                sw.WriteLine("EasyView=0");
                sw.WriteLine("NoSounds=0");
                sw.WriteLine("DisableAutoLat=0");
                sw.WriteLine("ShowBuildingCells=0");
            }
            catch
            {
                Logger.Log("An exception occurred while checking the existence of FinalSun settings");
            }
        }
    }
}
