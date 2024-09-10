using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using ClientCore.I18N;
using DTAClient.Domain.Multiplayer;
using Rampastring.Tools;
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
                ClientDXHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clientdx.exe")),
                ClientXNAHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clientxna.exe")),
                ClientOGLHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clientogl.exe")),
                ClientDXNET8Hash = string.Empty,
                ClientXNANET8Hash = string.Empty,
                ClientOGLNET8Hash = string.Empty,
                ClientUGLNET8Hash = string.Empty,
                GameExeHash = calculateGameExeHash ?
                Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.GetGameExecutableName())) : string.Empty,
                LauncherExeHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.GameLauncherExecutableName)),
                MPMapsHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MPMapsIniPath)),
                FHCConfigHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.BASE_RESOURCE_PATH, CONFIGNAME)),
                INIHashes = string.Empty
            };

            // .NET 8 hashes are optional
            FileInfo fileDX8 = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), "BinariesNET8", "Windows", "clientdx.dll");
            if (fileDX8.Exists)
                fh.ClientDXNET8Hash = Utilities.CalculateSHA1ForFile(fileDX8.FullName);

            FileInfo fileXNA8 = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), "BinariesNET8", "XNA", "clientxna.dll");
            if (fileXNA8.Exists)
                fh.ClientXNANET8Hash = Utilities.CalculateSHA1ForFile(fileXNA8.FullName);

            FileInfo fileOGL8 = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), "BinariesNET8", "OpenGL", "clientogl.dll");
            if (fileOGL8.Exists)
                fh.ClientOGLNET8Hash = Utilities.CalculateSHA1ForFile(fileOGL8.FullName);

            FileInfo fileUGL8 = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), "BinariesNET8", "UniversalGL", "clientogl.dll");
            if (fileUGL8.Exists) 
                fh.ClientUGLNET8Hash = Utilities.CalculateSHA1ForFile(fileUGL8.FullName);

            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + CONFIGNAME + ": " + fh.FHCConfigHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\GameOptions.ini: " + fh.GameOptionsHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\clientdx.exe: " + fh.ClientDXHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\clientxna.exe: " + fh.ClientXNAHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\clientogl.exe: " + fh.ClientOGLHash);
            Logger.Log("Hash for ClientDXNET8: " + fh.ClientDXNET8Hash);
            Logger.Log("Hash for ClientXNANET8: " + fh.ClientXNANET8Hash);
            Logger.Log("Hash for ClientOGLNET8: " + fh.ClientOGLNET8Hash);
            Logger.Log("Hash for ClientUGLNET8: " + fh.ClientUGLNET8Hash);
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

            // Add the hashes for each checked file from the available translations

            if (Directory.Exists(ClientConfiguration.Instance.TranslationsFolderPath))
            {
                DirectoryInfo translationsFolderPath = SafePath.GetDirectory(ClientConfiguration.Instance.TranslationsFolderPath);

                List<TranslationGameFile> translationGameFiles = ClientConfiguration.Instance.TranslationGameFiles
                    .Where(tgf => tgf.Checked).ToList();

                foreach (DirectoryInfo translationFolder in translationsFolderPath.EnumerateDirectories())
                {
                    foreach (TranslationGameFile tgf in translationGameFiles)
                    {
                        string filePath = SafePath.CombineFilePath(translationFolder.FullName, tgf.Source);
                        if (File.Exists(filePath))
                        {
                            string sha1 = Utilities.CalculateSHA1ForFile(filePath);
                            fh.INIHashes += sha1;

                            string fileRelativePath = filePath;
                            if (filePath.StartsWith(ProgramConstants.GamePath))
                                fileRelativePath = fileRelativePath.Substring(ProgramConstants.GamePath.Length).TrimStart(Path.DirectorySeparatorChar);

                            Logger.Log("Hash for " + fileRelativePath + ": " + sha1);
                        }
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
            str += fh.ClientDXNET8Hash;
            str += fh.ClientXNANET8Hash;
            str += fh.ClientOGLNET8Hash;
            str += fh.ClientUGLNET8Hash;
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
            string ClientDXNET8Hash,
            string ClientXNANET8Hash,
            string ClientOGLNET8Hash,
            string ClientUGLNET8Hash,
            string INIHashes,
            string MPMapsHash,
            string GameExeHash,
            string LauncherExeHash,
            string FHCConfigHash);
    }
}
