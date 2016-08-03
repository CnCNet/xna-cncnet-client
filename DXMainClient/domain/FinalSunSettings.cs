using System.IO;
using System.Windows.Forms;
using Rampastring.Tools;

namespace DTAClient.Domain
{
    public static class FinalSunSettings
    {
        /// <summary>
        /// Checks for the existence of the FinalSun settings file and writes it if it doesn't exist.
        /// </summary>
        public static void WriteFinalSunIni()
        {
            try
            {
                Logger.Log("Checking for the existence of FinalSun.ini.");
                if (File.Exists(MainClientConstants.gamepath + MainClientConstants.FINALSUN_INI))
                {
                    Logger.Log("FinalSun settings file exists.");

                    IniFile iniFile = new IniFile(MainClientConstants.gamepath + MainClientConstants.FINALSUN_INI);

                    iniFile.SetStringValue("FinalSun", "Language", "English");
                    iniFile.SetStringValue("FinalSun", "FileSearchLikeTS", "yes");
                    iniFile.SetStringValue("TS", "Exe", Application.ExecutablePath);
                    iniFile.WriteIniFile();

                    return;
                }

                Logger.Log("FinalSun.ini doesn't exist - writing default settings.");
                StreamWriter sw = new StreamWriter(MainClientConstants.gamepath + MainClientConstants.FINALSUN_INI);

                sw.WriteLine("[FinalSun]");
                sw.WriteLine("Language=English");
                sw.WriteLine("FileSearchLikeTS=yes");
                sw.WriteLine("");
                sw.WriteLine("[TS]");
                sw.WriteLine("Exe=" + Application.ExecutablePath);
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
