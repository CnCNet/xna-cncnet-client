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
        private FileHashes fh;
        private const string CONFIGNAME = "FHCConfig.ini";
        private bool calculateGameExeHash = true;

        string[] fileNamesToCheck = new string[]
        {
#if ARES
            "Ares.dll",
            "Ares.dll.inj",
            "Ares.mix",
            "Syringe.exe",
            "cncnet5.dll",
            "rulesmd.ini",
            "artmd.ini",
            "soundmd.ini",
            "aimd.ini",
            "shroud.shp",
#elif YR
            "spawner.xdp",
            "spawner2.xdp",
            "artmd.ini",
            "soundmd.ini",
            "aimd.ini",
            "shroud.shp",
            "INI/Map Code/Cooperative.ini",
            "INI/Map Code/Free For All.ini",
            "INI/Map Code/Land Rush.ini",
            "INI/Map Code/Meat Grinder.ini",
            "INI/Map Code/Megawealth.ini",
            "INI/Map Code/Naval War.ini",
            "INI/Map Code/Standard.ini",
            "INI/Map Code/Team Alliance.ini",
            "INI/Map Code/Unholy Alliance.ini",
            "INI/Game Options/Allies Allowed.ini",
            "INI/Game Options/Brutal AI.ini",
            "INI/Game Options/No Dog Engi Eat.ini",
            "INI/Game Options/No Spawn Previews.ini",
            "INI/Game Options/RA2 Classic Mode.ini",
            "INI/Map Code/GlobalCode.ini",
            "INI/Map Code/MultiplayerGlobalCode.ini",
#elif TS
            "spawner.xdp",
            "rules.ini",
            "ai.ini",
            "art.ini",
            "shroud.shp",
            "INI/Rules.ini",
            "INI/Enhance.ini",
            "INI/Firestrm.ini",
            "INI/Art.ini",
            "INI/ArtE.ini",
            "INI/ArtFS.ini",
            "INI/AI.ini",
            "INI/AIE.ini",
            "INI/AIFS.ini",
#endif
        };

        public FileHashCalculator() => ParseConfigFile();

        public void CalculateHashes(List<GameMode> gameModes)
        {
            fh = new FileHashes
            {
                GameOptionsHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "GameOptions.ini"),
                ClientDXHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GetBaseResourcePath() + "clientdx.exe"),
                ClientXNAHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GetBaseResourcePath() + "clientxna.exe"),
                ClientOGLHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GetBaseResourcePath() + "clientogl.exe"),
                GameExeHash = calculateGameExeHash ?
                Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + ClientConfiguration.Instance.GetGameExecutableName()) : string.Empty,
                LauncherExeHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + ClientConfiguration.Instance.GameLauncherExecutableName),
                MPMapsHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + ClientConfiguration.Instance.MPMapsIniPath),
                FHCConfigHash = Utilities.CalculateSHA1ForFile(ProgramConstants.BASE_RESOURCE_PATH + CONFIGNAME),
                INIHashes = string.Empty
            };

            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + CONFIGNAME + ": " + fh.FHCConfigHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "GameOptions.ini: " + fh.GameOptionsHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "clientdx.exe: " + fh.ClientDXHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "clientxna.exe: " + fh.ClientXNAHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "clientogl.exe: " + fh.ClientOGLHash);
            Logger.Log("Hash for " + ClientConfiguration.Instance.MPMapsIniPath + ": " + fh.MPMapsHash);

            if (calculateGameExeHash)
                Logger.Log("Hash for " + ClientConfiguration.Instance.GetGameExecutableName() + ": " + fh.GameExeHash);

            if (!string.IsNullOrEmpty(ClientConfiguration.Instance.GameLauncherExecutableName))
                Logger.Log("Hash for " + ClientConfiguration.Instance.GameLauncherExecutableName + ": " + fh.LauncherExeHash);

            foreach (string filePath in fileNamesToCheck)
            {
                fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, filePath);
                Logger.Log("Hash for " + filePath + ": " +
                    Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + filePath));
            }

            string[] iniPaths = new string[]
            {
#if !YR
                ProgramConstants.GamePath + "INI/Map Code",
#endif
                ProgramConstants.GamePath + "INI/Game Options"
            };

            foreach (string path in iniPaths)
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    List<string> files = Directory.GetFiles(path, "*", SearchOption.AllDirectories).
                        Select(s => s.Replace(ProgramConstants.GamePath, "").Replace("\\", "/")).ToList();

                    files.Sort(StringComparer.Ordinal);

                    foreach (string filename in files)
                    {
                        string sha1 = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + filename);
                        fh.INIHashes += sha1;
                        Logger.Log("Hash for " + filename + ": " + sha1);
                    }
                }
            }

            fh.INIHashes = Utilities.CalculateSHA1ForString(fh.INIHashes);
        }

        string AddToStringIfFileExists(string str, string path)
        {
            if (File.Exists(path))
                return str + Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + path);

            return str;
        }

        public string GetCompleteHash()
        {
            string str = fh.GameOptionsHash;
            str += fh.ClientDXHash;
            str += fh.ClientXNAHash;
            str += fh.ClientOGLHash;
            str += fh.GameExeHash;
            str += fh.LauncherExeHash;
            str += fh.INIHashes;
            str += fh.MPMapsHash;
            str += fh.FHCConfigHash;

            Logger.Log("Complete hash: " + Utilities.CalculateSHA1ForString(str));

            return Utilities.CalculateSHA1ForString(str);
        }

        private void ParseConfigFile()
        {
            IniFile config = new IniFile(ProgramConstants.GetBaseResourcePath() + CONFIGNAME);
            calculateGameExeHash = config.GetBooleanValue("Settings", "CalculateGameExeHash", true);

            List<string> keys = config.GetSectionKeys("FilenameList");
            if (keys == null || keys.Count < 1)
                return;

            List<string> filenames = new List<string>();
            foreach (string key in keys)
            {
                string value = config.GetStringValue("FilenameList", key, string.Empty);
                filenames.Add(value == string.Empty ? key : value);
            }

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
        public string GameExeHash { get; set; }
        public string LauncherExeHash { get; set; }
        public string FHCConfigHash { get; set; }

        public override string ToString()
        {
            return "GameOptions Hash: " + GameOptionsHash + Environment.NewLine +
                "ClientDXHash: " + ClientDXHash + Environment.NewLine +
                "ClientXNAHash: " + ClientXNAHash + Environment.NewLine +
                "ClientOGLHash: " + ClientOGLHash + Environment.NewLine +
                "INI Hashes: " + INIHashes + Environment.NewLine +
                "MPMaps Hash: " + MPMapsHash + Environment.NewLine +
                "MainExe Hash: " + GameExeHash + Environment.NewLine +
                "LauncherExe Hash: " + LauncherExeHash + Environment.NewLine +
                "FHCConfig Hash: " + FHCConfigHash;
        }
    }
}
