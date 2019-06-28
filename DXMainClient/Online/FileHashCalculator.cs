using ClientCore;
using DTAClient.Domain.Multiplayer;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities = Rampastring.Tools.Utilities;

namespace DTAClient.Online
{
    public class FileHashCalculator
    {
        FileHashes fh;
        const string CONFIGNAME = "FHCConfig.ini";

        string[] fileNamesToCheck = new string[]
        {
#if MO
            "Ares.dll",
            "Ares.dll.inj",
            "expandmo97.mix",
            "expandmo99.mix",
            "cncnet5.dll",
            "rulesmo.ini",
            "artmo.ini",
            "soundmo.ini",
#elif YR
            "spawner.xdp",
            "spawner2.xdp",
            "INI\\Map Code\\Cooperative.ini",
            "INI\\Map Code\\Free For All.ini",
            "INI\\Map Code\\Land Rush.ini",
            "INI\\Map Code\\Meat Grinder.ini",
            "INI\\Map Code\\Megawealth.ini",
            "INI\\Map Code\\Naval War.ini",
            "INI\\Map Code\\Standard.ini",
            "INI\\Map Code\\Team Alliance.ini",
            "INI\\Map Code\\Unholy Alliance.ini",
            "INI\\Game Options\\Allies Allowed.ini",
            "INI\\Game Options\\Brutal AI.ini",
            "INI\\Game Options\\No Dog Engi Eat.ini",
            "INI\\Game Options\\No Spawn Previews.ini",
            "INI\\Game Options\\RA2 Classic Mode.ini",
#else
            "spawner.xdp",
            "rules.ini",
            "rulesmd.ini",
            "ai.ini",
            "art.ini",
            "artmd.ini",
            "aimd.ini",
            "INI\\Rules.ini",
            "INI\\Enhance.ini",
            "INI\\Firestrm.ini",
            "INI\\Art.ini",
            "INI\\ArtE.ini",
            "INI\\ArtFS.ini",
            "INI\\AI.ini",
            "INI\\AIE.ini",
            "INI\\AIFS.ini",
#endif
            "INI\\GlobalCode.ini",
            ProgramConstants.BASE_RESOURCE_PATH + CONFIGNAME,
        };


        public FileHashCalculator()
        {
            GetFileListFromConfig();
        }

        public void CalculateHashes(List<GameMode> gameModes)
        {
            fh = new FileHashes
            {
                GameOptionsHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "GameOptions.ini"),
                ClientDXHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GetBaseResourcePath() + "clientdx.exe"),
                ClientXNAHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GetBaseResourcePath() + "clientxna.exe"),
                ClientOGLHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GetBaseResourcePath() + "clientogl.exe"),
                MainExeHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + ClientConfiguration.Instance.GetGameExecutableName()),
                LauncherExeHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + ClientConfiguration.Instance.GetGameLauncherExecutableName),
                MPMapsHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + ClientConfiguration.Instance.MPMapsIniPath),
                MPModesHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + ClientConfiguration.Instance.MPModesIniPath),
                INIHashes = string.Empty
            };

            foreach (string filePath in fileNamesToCheck)
            {
                fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, filePath);
                Logger.Log("Hash for " + filePath + ": " + 
                    Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + filePath));
            }

            #if !YR
            if (Directory.Exists(ProgramConstants.GamePath + "INI\\Map Code"))
            {
                foreach (GameMode gameMode in gameModes)
                {
                    fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, "INI\\Map Code\\" + gameMode.Name + ".ini");
                    Logger.Log("Hash for INI\\Map Code\\" + gameMode.Name + ".ini :" +
                        Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "INI\\Map Code\\" + gameMode.Name + ".ini"));
                }
            }
            #endif

            if (Directory.Exists(ProgramConstants.GamePath + "INI\\Game Options"))
            {
                List<string> files = Directory.GetFiles(
                    ProgramConstants.GamePath + "INI\\Game Options",
                    "*", SearchOption.AllDirectories).ToList();

                files.Sort();

                foreach (string fileName in files)
                {
                    fh.INIHashes = fh.INIHashes + Utilities.CalculateSHA1ForFile(fileName);
                    Logger.Log("Hash for " + fileName + ": " +
                        Utilities.CalculateSHA1ForFile(fileName));
                }
            }

            fh.INIHashes = Utilities.CalculateSHA1ForString(fh.INIHashes);
        }

        string AddToStringIfFileExists(string str, string path)
        {
            if (File.Exists(path))
            {
                return str + Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + path);
            }

            return str;
        }

        public string GetCompleteHash()
        {
            string str = fh.GameOptionsHash;
            str = str + fh.ClientDXHash;
            str = str + fh.ClientXNAHash;
            str = str + fh.ClientOGLHash;
            str = str + fh.MainExeHash;
            str = str + fh.LauncherExeHash;
            str = str + fh.INIHashes;
            str = str + fh.MPMapsHash;
            str = str + fh.MPModesHash;

            Logger.Log("Complete hash: " + Utilities.CalculateSHA1ForString(str));

            return Utilities.CalculateSHA1ForString(str);
        }

        private void GetFileListFromConfig()
        {
            IniFile filenamesconfig = new IniFile(ProgramConstants.GetBaseResourcePath() + CONFIGNAME);
            List<string> filenames = filenamesconfig.GetSectionKeys("FilenameList");
            if (filenames == null || filenames.Count < 1) return;
            filenames.Add("INI\\GlobalCode.ini");
            filenames.Add(ProgramConstants.BASE_RESOURCE_PATH + CONFIGNAME);
            fileNamesToCheck = filenames.ToArray();
        }
    }

    struct FileHashes
    {
        public string GameOptionsHash { get; set; }
        public string ClientDXHash { get; set; }
        public string ClientXNAHash { get; set; }
        public string ClientOGLHash { get; set; }
        public string INIHashes { get; set; }
        public string MPMapsHash { get; set; }
        public string MPModesHash { get; set; }
        public string MainExeHash { get; set; }
        public string LauncherExeHash { get; set; }

        public override string ToString()
        {
            return "GameOptions Hash: " + GameOptionsHash + Environment.NewLine +
                "ClientDXHash: " + ClientDXHash + Environment.NewLine +
                "ClientXNAHash: " + ClientXNAHash + Environment.NewLine +
                "ClientOGLHash: " + ClientOGLHash + Environment.NewLine +
                "INI Hashes: " + INIHashes + Environment.NewLine + 
                "MPMaps Hash: " + MPMapsHash + Environment.NewLine + 
                "MPModes Hash: " + MPModesHash + Environment.NewLine +
                "MainExe Hash: " + MainExeHash + Environment.NewLine +
                "LauncherExe Hash: " + LauncherExeHash;
        }
    }
}
