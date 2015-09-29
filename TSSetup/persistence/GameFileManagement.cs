using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using dtasetup.domain;
using System.IO;
using ClientCore;

namespace dtasetup.persistence
{
    /// <summary>
    ///     Manages the files in the game folder
    /// </summary>
    public static class GameFileManagement
    {
        /// <summary>
        ///     This function tests the existing language.dll file for known versions
        /// </summary>
        /// <returns>an object of the LanguageDllId enum class with the file identification</returns>
        public static LanguageDllId getLanguageDllId()
        {
            return getLanguageDllId(MainClientConstants.gamepath + MainClientConstants.LANGUAGE_DLL);
        }

        /// <summary>
        ///     This function tests the existing thipx32.dll file for known versions
        /// </summary>
        /// <param name="dllpath">Full path and filename of the thipx32.dll to check</param>
        /// <returns>an object of the Thipx32DllID enum class with the file identification</returns>
        public static LanguageDllId getLanguageDllId(String dllpath)
        {
            if (!File.Exists(dllpath))
                return LanguageDllId.NOTEXIST;
            String fileMD5 = Utilities.calculateMD5ForFile(dllpath);
            if (fileMD5.Equals("9f6ba036ed491af7fbada6dcfdece71f"))
                return LanguageDllId.GAMEORIG; // should be impossible since it's not in the mod.
            if (fileMD5.Equals(MainClientConstants.gamepath + "Resources\\language_800x600.dll"))
                return LanguageDllId.DTA800X600;
            if (fileMD5.Equals(MainClientConstants.gamepath + "Resources\\language_1024x720.dll"))
                return LanguageDllId.DTA1024X720;
            return LanguageDllId.UNKNOWN;
        }

        /// <summary>
        ///  Replace the current language.dll with an internally stored version
        /// </summary>
        public static Boolean setLanguageDll(LanguageDllId languagedll)
        {
            if (getLanguageDllId() != languagedll)
            {
                byte[] file = null;
                switch (languagedll)
                {
                    case LanguageDllId.DTA800X600:
                        if (!File.Exists(MainClientConstants.gamepath + "Resources\\language_800x600.dll"))
                            return false;
                        file = File.ReadAllBytes(MainClientConstants.gamepath + "Resources\\language_800x600.dll");
                        break;
                    case LanguageDllId.DTA1024X720:
                        if (!File.Exists(MainClientConstants.gamepath + "Resources\\language_1024x720.dll"))
                            return false;
                        file = File.ReadAllBytes(MainClientConstants.gamepath + "Resources\\language_1024x720.dll");
                        break;
                }
                if (file != null)
                    try
                    {
                        File.WriteAllBytes(MainClientConstants.gamepath + MainClientConstants.LANGUAGE_DLL, file);
                        return true;
                    }
                    catch { }
            }
            return false;
        }
    }
}
