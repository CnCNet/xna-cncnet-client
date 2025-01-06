using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using ClientCore;
using ClientCore.I18N;

using Rampastring.Tools;

namespace DTAClient.Online
{
    public class FileHashCalculator
    {
        private const string CONFIGNAME = "FHCConfig.ini";
        private bool calculateGameExeHash = true;

        private static readonly IReadOnlyList<string> knownTextFileExtensions = [".txt", ".ini", ".json", ".xml"];

        private string[] fileNamesToCheck = new string[]
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

        private string finalHash = string.Empty;

        public void CalculateHashes()
        {
            FileHashes fh = new()
            {
                GameOptionsHash = CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.BASE_RESOURCE_PATH, "GameOptions.ini")),
                ClientDXHash = CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clientdx.exe")),
                ClientXNAHash = CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clientxna.exe")),
                ClientOGLHash = CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clientogl.exe")),
                ClientDXNET8Hash = string.Empty,
                ClientXNANET8Hash = string.Empty,
                ClientOGLNET8Hash = string.Empty,
                ClientUGLNET8Hash = string.Empty,
                GameExeHash = calculateGameExeHash ?
                CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.GetGameExecutableName())) : string.Empty,
                LauncherExeHash = CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.GameLauncherExecutableName)),
                MPMapsHash = CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MPMapsIniPath)),
                FHCConfigHash = CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.BASE_RESOURCE_PATH, CONFIGNAME)),
            };

            // .NET 8 hashes are optional
            FileInfo fileDX8 = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), "BinariesNET8", "Windows", "clientdx.dll");
            if (fileDX8.Exists)
                fh.ClientDXNET8Hash = CalculateSHA1ForFile(fileDX8.FullName);

            FileInfo fileXNA8 = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), "BinariesNET8", "XNA", "clientxna.dll");
            if (fileXNA8.Exists)
                fh.ClientXNANET8Hash = CalculateSHA1ForFile(fileXNA8.FullName);

            FileInfo fileOGL8 = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), "BinariesNET8", "OpenGL", "clientogl.dll");
            if (fileOGL8.Exists)
                fh.ClientOGLNET8Hash = CalculateSHA1ForFile(fileOGL8.FullName);

            FileInfo fileUGL8 = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), "BinariesNET8", "UniversalGL", "clientogl.dll");
            if (fileUGL8.Exists)
                fh.ClientUGLNET8Hash = CalculateSHA1ForFile(fileUGL8.FullName);

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

            foreach (string relativePath in fileNamesToCheck)
            {
                string fullPath = SafePath.CombineFilePath(ProgramConstants.GamePath, relativePath);
                string hash = fh.AddHashForFileIfExists(relativePath, fullPath);
                if (!string.IsNullOrEmpty(hash))
                    Logger.Log("Hash for " + relativePath + ": " + hash);
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
                    foreach (string filename in path.EnumerateFiles("*", SearchOption.AllDirectories).Select(s => Path.GetRelativePath(path.FullName, s.FullName)))
                    {
                        string fileRelativePath = SafePath.CombineFilePath(path.Name, filename);
                        string fileFullPath = SafePath.CombineFilePath(path.FullName, filename);
                        Debug.Assert(File.Exists(fileFullPath), $"File {fileFullPath} is supposed to but does not exist.");

                        string hash = fh.AddHashForFileIfExists(fileRelativePath, fileFullPath);
                        if (!string.IsNullOrEmpty(hash))
                            Logger.Log("Hash for " + fileRelativePath + ": " + hash);
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
                        string fileRelativePath = SafePath.CombineFilePath(translationFolder.Name, tgf.Source);
                        string fileFullPath = SafePath.CombineFilePath(translationFolder.FullName, tgf.Source);

                        string hash = fh.AddHashForFileIfExists(fileRelativePath, fileFullPath);
                        if (!string.IsNullOrEmpty(hash))
                            Logger.Log("Hash for " + fileRelativePath + ": " + hash);
                    }
                }
            }

            finalHash = fh.GetFinalHash();
            Logger.Log("Complete hash: " + finalHash);
        }

        public string GetCompleteHash() => finalHash;

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

        private static string NormalizePath(string path) => path.Replace('\\', '/');

        private static string CalculateSHA1ForFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            FileInfo file = SafePath.GetFile(path);
            if (!file.Exists)
                return string.Empty;

            using Stream inputStream = file.OpenRead();

            if (knownTextFileExtensions.Contains(file.Extension, StringComparer.InvariantCultureIgnoreCase))
            {
                // Normalize line endings to LF
                UTF8Encoding utf8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

                using StreamReader reader = new(inputStream, utf8Encoding, detectEncodingFromByteOrderMarks: false);
                string text = reader.ReadToEnd();
                text = text.Replace("\r\n", "\n").Trim();

                byte[] bytes = utf8Encoding.GetBytes(text);

                using SHA1 sha1 = SHA1.Create();
                return BytesToString(sha1.ComputeHash(bytes));
            }
            else
            {
                using SHA1 sha1 = SHA1.Create();
                return BytesToString(sha1.ComputeHash(inputStream));
            }
        }

        private static string BytesToString(byte[] bytes) =>
            BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();

        private class FileHashes()
        {
            public string GameOptionsHash;
            public string ClientDXHash;
            public string ClientXNAHash;
            public string ClientOGLHash;
            public string ClientDXNET8Hash;
            public string ClientXNANET8Hash;
            public string ClientOGLNET8Hash;
            public string ClientUGLNET8Hash;
            public string MPMapsHash;
            public string GameExeHash;
            public string LauncherExeHash;
            public string FHCConfigHash;

            public readonly SortedDictionary<string, string> AdditionalFileHashes = new(StringComparer.InvariantCultureIgnoreCase);

            public string AddHashForFileIfExists(string relativePath) =>
                AddHashForFileIfExists(relativePath, relativePath);

            public string AddHashForFileIfExists(string relativePath, string filePath)
            {
                Debug.Assert(!relativePath.StartsWith(ProgramConstants.GamePath), $"File path {relativePath} should be a relative path.");

                string hash = CalculateSHA1ForFile(filePath);
                if (!string.IsNullOrEmpty(hash))
                {
                    AdditionalFileHashes[NormalizePath(relativePath)] = hash;
                    return hash;
                }
                else
                {
                    return string.Empty;
                }
            }

            public string GetFinalHash()
            {
                var sb = new StringBuilder();
                sb.Append(GameOptionsHash);
                sb.Append(ClientDXHash);
                sb.Append(ClientXNAHash);
                sb.Append(ClientOGLHash);
                sb.Append(ClientDXNET8Hash);
                sb.Append(ClientXNANET8Hash);
                sb.Append(ClientOGLNET8Hash);
                sb.Append(ClientUGLNET8Hash);
                sb.Append(GameExeHash);
                sb.Append(LauncherExeHash);
                sb.Append(MPMapsHash);
                sb.Append(FHCConfigHash);

                // Append additional file hashes, ordered by key
                foreach (string fileHash in AdditionalFileHashes.Values)
                    sb.Append(fileHash);

                // Merge hashes
                string finalHash = sb.ToString();
                byte[] buffer = Encoding.ASCII.GetBytes(finalHash);
                using SHA1 sha1 = SHA1.Create();
                byte[] hash = sha1.ComputeHash(buffer);
                return BytesToString(hash);
            }
        }
    }
}
