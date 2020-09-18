using System.IO;
using Rampastring.Tools;
using ClientCore;

namespace DTAClient.Domain
{
    public static class FinalSunSettings
    {
        /// <summary>
        /// Checks for the existence of the FinalSun settings file and writes it if it doesn't exist.
        /// </summary>
        public static void WriteFinalSunIni()
        {
            // the encoding of the FinalSun/FinalAlert ini file should be ANSI instead of UTF-8. Otherwise, the map editor will not work in a non-ASCII path. Be sure to use .NET Framework instead of .NET Core as the latter doesn't support ANSI. Also, ANSI doesn't mean a specific codepage, it means the default non-Unicode codepage which can be changed from Control Panel.
            try
            {
                string finalSunIniPath = ClientConfiguration.Instance.FinalSunIniPath;

                Logger.Log("Checking for the existence of FinalSun.ini.");
                if (File.Exists(ProgramConstants.GamePath + finalSunIniPath))
                {
                    Logger.Log("FinalSun settings file exists.");

                    IniFile iniFile = new IniFile();
                    iniFile.FileName = ProgramConstants.GamePath + finalSunIniPath;
                    iniFile.Encoding = System.Text.Encoding.Default;
                    iniFile.Parse();
                    
                    iniFile.SetStringValue("FinalSun", "Language", "English");
                    iniFile.SetStringValue("FinalSun", "FileSearchLikeTS", "yes");
                    iniFile.SetStringValue("TS", "Exe", ProgramConstants.GamePath.Replace('/', '\\'));
                    iniFile.WriteIniFile();

                    return;
                }

                Logger.Log("FinalSun.ini doesn't exist - writing default settings.");

                StreamWriter sw = new StreamWriter(ProgramConstants.GamePath + finalSunIniPath, false, System.Text.Encoding.Default);

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
                sw.Close();
            }
            catch
            {
                Logger.Log("An exception occured while checking the existence of FinalSun settings");
            }
        }
    }
}
