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
                GameOptionsHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.BASE_RESOURCE_PATH, "GameOptions.ini")),
                ClientDXHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "Binaries", "Windows", "clientdx.dll")),
                ClientXNAHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "Binaries", "XNA", "clientxna.dll")),
                ClientOGLHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "Binaries", "OpenGL", "clientogl.dll")),
                ClientUGLHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "Binaries", "UniversalGL", "clientogl.dll")),
                GameExeHash = calculateGameExeHash ?
                Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.GetGameExecutableName())) : string.Empty,
                LauncherExeHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.GameLauncherExecutableName)),
                MPMapsHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MPMapsIniPath)),
                FHCConfigHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.BASE_RESOURCE_PATH, CONFIGNAME)),
                INIHashes = string.Empty
            };

            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + CONFIGNAME + ": " + fh.FHCConfigHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\GameOptions.ini: " + fh.GameOptionsHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\Binaries\\Windows\\clientdx.dll: " + fh.ClientDXHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\Binaries\\XNA\\clientxna.dll: " + fh.ClientXNAHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\Binaries\\OpenGL\\clientogl.dll: " + fh.ClientOGLHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\Binaries\\UniversalGL\\clientogl.dll: " + fh.ClientUGLHash);
            Logger.Log("Hash for " + ClientConfiguration.Instance.MPMapsIniPath + ": " + fh.MPMapsHash);

            if (calculateGameExeHash)
                Logger.Log("Hash for " + ClientConfiguration.Instance.GetGameExecutableName() + ": " + fh.GameExeHash);

            if (!string.IsNullOrEmpty(ClientConfiguration.Instance.GameLauncherExecutableName))
                Logger.Log("Hash for " + ClientConfiguration.Instance.GameLauncherExecutableName + ": " + fh.LauncherExeHash);

            foreach (string filePath in fileNamesToCheck)
            {
                fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, filePath);
                Logger.Log("Hash for " + filePath + ": " +
                    Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, filePath)));
            }

            DirectoryInfo[] iniPaths =
            {
#if !YR
               SafePath.GetDirectory(ProgramConstants.GamePath, "INI", "Map Code"),
#endif
               SafePath.GetDirectory(ProgramConstants.GamePath, "INI", "Game Options")
            };

            foreach (DirectoryInfo path in iniPaths)
            {
                if (path.Exists)
                {
                    List<string> files = path.EnumerateFiles("*", SearchOption.AllDirectories).Select(s => s.Name).ToList();

                    files.Sort(StringComparer.Ordinal);

                    foreach (string filename in files)
                    {
                        string sha1 = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, filename));
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
                return str + Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, path));

            return str;
        }

        public string GetCompleteHash()
        {
            string str = fh.GameOptionsHash;
            str += fh.ClientDXHash;
            str += fh.ClientXNAHash;
            str += fh.ClientOGLHash;
            str += fh.ClientUGLHash;
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
            IniFile config = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), CONFIGNAME));
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

        private record struct FileHashes(
            string GameOptionsHash,
            string ClientDXHash,
            string ClientXNAHash,
            string ClientOGLHash,
            string ClientUGLHash,
            string INIHashes,
            string MPMapsHash,
            string GameExeHash,
            string LauncherExeHash,
            string FHCConfigHash);
    }
}
