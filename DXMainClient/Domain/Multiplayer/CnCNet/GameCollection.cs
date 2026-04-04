using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using Rampastring.Tools;
using ClientCore;
using ClientCore.Extensions;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A class for storing the collection of supported CnCNet games.
    /// </summary>
    public class GameCollection
    {
        public List<CnCNetGame> GameList { get; private set; }

        public GameCollection()
        {
            Initialize();
        }

        public void Initialize()
        {
            GameList = new List<CnCNetGame>();

            // Default supported games. Images are loaded lazily in background threads;
            // textures are created on demand on the main thread when first accessed.
            var defaultGames = new DefaultCnCNetGame[]
            {
                new DefaultCnCNetGame("DTAClient.Icons.dtaicon.png")
                {
                    ChatChannel = "#cncnet-dta",
                    ClientExecutableName = "DTA.exe",
                    GameBroadcastChannel = "#cncnet-dta-games",
                    InternalName = "dta",
                    RegistryInstallPath = "HKCU\\Software\\TheDawnOfTheTiberiumAge",
                    UIName = "Dawn of the Tiberium Age".L10N("Client:ClientCore:DawnoftheTiberiumAge")
                },

                new DefaultCnCNetGame("DTAClient.Icons.tiicon.png")
                {
                    ChatChannel = "#cncnet-ti",
                    ClientExecutableName = "TI_Launcher.exe",
                    GameBroadcastChannel = "#cncnet-ti-games",
                    InternalName = "ti",
                    RegistryInstallPath = "HKCU\\Software\\TwistedInsurrection",
                    UIName = "Twisted Insurrection".L10N("Client:ClientCore:TwistedInsurrection")
                },

                new DefaultCnCNetGame("DTAClient.Icons.moicon.png")
                {
                    ChatChannel = "#cncnet-mo",
                    ClientExecutableName = "MentalOmegaClient.exe",
                    GameBroadcastChannel = "#cncnet-mo-games",
                    InternalName = "mo",
                    RegistryInstallPath = "HKCU\\Software\\MentalOmega",
                    UIName = "Mental Omega".L10N("Client:ClientCore:MentalOmega")
                },

                new DefaultCnCNetGame("DTAClient.Icons.rricon.png")
                {
                    ChatChannel = "#redres-lobby",
                    ClientExecutableName = "RRLauncher.exe",
                    GameBroadcastChannel = "#redres-games",
                    InternalName = "rr",
                    RegistryInstallPath = "HKLM\\Software\\RedResurrection",
                    UIName = "YR Red-Resurrection".L10N("Client:ClientCore:YRRedResurrection")
                },

                new DefaultCnCNetGame("DTAClient.Icons.reicon.png")
                {
                    ChatChannel = "#riseoftheeast",
                    ClientExecutableName = "RELauncher.exe",
                    GameBroadcastChannel = "#rote-games",
                    InternalName = "re",
                    RegistryInstallPath = "HKLM\\Software\\RiseoftheEast",
                    UIName = "Rise of the East".L10N("Client:ClientCore:RiseoftheEast")
                },

                new DefaultCnCNetGame("DTAClient.Icons.cncricon.png")
                {
                    ChatChannel = "#cncreloaded",
                    ClientExecutableName = "CnCReloadedClient.exe",
                    GameBroadcastChannel = "#cncreloaded-games",
                    InternalName = "cncr",
                    RegistryInstallPath = "HKCU\\Software\\CnCReloaded",
                    UIName = "C&C: Reloaded".L10N("Client:ClientCore:CnCReloaded")
                },

                new DefaultCnCNetGame("DTAClient.Icons.tdicon.png")
                {
                    ChatChannel = "#cncnet-td",
                    ClientExecutableName = "TiberianDawn.exe",
                    GameBroadcastChannel = "#cncnet-td-games",
                    InternalName = "td",
                    RegistryInstallPath = "HKLM\\Software\\Westwood\\Tiberian Dawn",
                    UIName = "Tiberian Dawn".L10N("Client:ClientCore:TiberianDawn"),
                    Supported = false
                },

                new DefaultCnCNetGame("DTAClient.Icons.raicon.png")
                {
                    ChatChannel = "#cncnet-ra",
                    ClientExecutableName = "RedAlert.exe",
                    GameBroadcastChannel = "#cncnet-ra-games",
                    InternalName = "ra",
                    RegistryInstallPath = "HKLM\\Software\\Westwood\\Red Alert",
                    UIName = "Red Alert".L10N("Client:ClientCore:RedAlert")
                },

                new DefaultCnCNetGame("DTAClient.Icons.d2kicon.png")
                {
                    ChatChannel = "#cncnet-d2k",
                    ClientExecutableName = "Dune2000.exe",
                    GameBroadcastChannel = "#cncnet-d2k-games",
                    InternalName = "d2k",
                    RegistryInstallPath = "HKLM\\Software\\Westwood\\Dune 2000",
                    UIName = "Dune 2000".L10N("Client:ClientCore:Dune2000"),
                    Supported = false
                },

                new DefaultCnCNetGame("DTAClient.Icons.tsicon.png")
                {
                    ChatChannel = "#cncnet-ts",
                    ClientExecutableName = "TiberianSun.exe",
                    GameBroadcastChannel = "#cncnet-ts-games",
                    InternalName = "ts",
                    RegistryInstallPath = "HKLM\\Software\\Westwood\\Tiberian Sun",
                    UIName = "Tiberian Sun".L10N("Client:ClientCore:TiberianSun")
                },

                new DefaultCnCNetGame("DTAClient.Icons.yricon.png")
                {
                    ChatChannel = "#cncnet-yr",
                    ClientExecutableName = "CnCNetClientYR.exe",
                    GameBroadcastChannel = "#cncnet-yr-games",
                    InternalName = "yr",
                    RegistryInstallPath = "HKLM\\Software\\Westwood\\Yuri's Revenge",
                    UIName = "Yuri's Revenge".L10N("Client:ClientCore:YurisRevenge")
                },

                new DefaultCnCNetGame("DTAClient.Icons.ssicon.png")
                {
                    ChatChannel = "#cncnet-ss",
                    ClientExecutableName = "SoleSurvivor.exe",
                    GameBroadcastChannel = "#cncnet-ss-games",
                    InternalName = "ss",
                    RegistryInstallPath = "HKLM\\Software\\Westwood\\Sole Survivor",
                    UIName = "Sole Survivor".L10N("Client:ClientCore:SoleSurvivor"),
                    Supported = false
                }
            };

            // CnCNet chat.
            var otherGames = new DefaultCnCNetGame[]
            {
                new DefaultCnCNetGame("DTAClient.Icons.cncneticon.png")
                {
                    ChatChannel = "#cncnet",
                    InternalName = "cncnet",
                    UIName = "General CnCNet Chat".L10N("Client:ClientCore:GeneralCnCNetChat"),
                    AlwaysEnabled = true
                }
            };

            GameList.AddRange(defaultGames);
            GameList.AddRange(GetCustomGames(defaultGames.Concat<CnCNetGame>(otherGames).ToList()));
            GameList.AddRange(otherGames);

            if (GetGameIndexFromInternalName(ClientConfiguration.Instance.LocalGame) == -1)
            {
                throw new ClientConfigurationException("Could not find a game in the game collection matching LocalGame value of " +
                    ClientConfiguration.Instance.LocalGame + ".");
            }

            // Fire-and-forget background preloading of images.
            var gamesToPreload = GameList.ToList();
            _ = Task.Run(() =>
            {
                foreach (var game in gamesToPreload)
                    _ = game.Image;
            });
        }

        private List<CnCNetGame> GetCustomGames(List<CnCNetGame> existingGames)
        {
            IniFile iniFile = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "GameCollectionConfig.ini"));

            List<CnCNetGame> customGames = new List<CnCNetGame>();

            var section = iniFile.GetSection("CustomGames");

            if (section == null)
                return customGames;

            HashSet<string> customGameIDs = new HashSet<string>();
            foreach (var kvp in section.Keys)
            {
                if (!iniFile.SectionExists(kvp.Value))
                    continue;

                string ID = iniFile.GetStringValue(kvp.Value, "InternalName", string.Empty).ToLowerInvariant();

                if (string.IsNullOrEmpty(ID))
                    throw new GameCollectionConfigurationException("InternalName for game " + kvp.Value + " is not defined or set to an empty value.");

                if (ID.Length > ProgramConstants.GAME_ID_MAX_LENGTH)
                {
                    throw new GameCollectionConfigurationException("InternalGame for game " + kvp.Value + " is set to a value that exceeds length limit of " +
                        ProgramConstants.GAME_ID_MAX_LENGTH + " characters.");
                }

                if (existingGames.Find(g => g.InternalName == ID) != null || customGameIDs.Contains(ID))
                    throw new GameCollectionConfigurationException("Game with InternalName " + ID.ToUpperInvariant() + " already exists in the game collection.");

                string iconFilename = iniFile.GetStringValue(kvp.Value, "IconFilename", ID + "icon.png");
                customGames.Add(new CustomCnCNetGame(iconFilename)
                {
                    InternalName = ID,
                    UIName = iniFile.GetStringValue(kvp.Value, "UIName", ID.ToUpperInvariant()),
                    ChatChannel = GetIRCChannelNameFromIniFile(iniFile, kvp.Value, "ChatChannel"),
                    GameBroadcastChannel = GetIRCChannelNameFromIniFile(iniFile, kvp.Value, "GameBroadcastChannel"),
                    ClientExecutableName = iniFile.GetStringValue(kvp.Value, "ClientExecutableName", string.Empty),
                    RegistryInstallPath = iniFile.GetStringValue(kvp.Value, "RegistryInstallPath", "HKCU\\Software\\"
                            + ID.ToUpperInvariant())
                });
                customGameIDs.Add(ID);
            }

            return customGames;
        }

        private string GetIRCChannelNameFromIniFile(IniFile iniFile, string section, string key)
        {
            string channel = iniFile.GetStringValue(section, key, string.Empty);

            if (string.IsNullOrEmpty(channel))
                throw new GameCollectionConfigurationException(key + " for game " + section + " is not defined or set to an empty value.");

            if (channel.Contains(' ') || channel.Contains(',') || channel.Contains((char)7))
                throw new GameCollectionConfigurationException(key + " for game " + section + " contains characters not allowed on IRC channel names.");

            if (!channel.StartsWith("#"))
                return "#" + channel;

            return channel;
        }

        /// <summary>
        /// Gets the index of a CnCNet supported game based on its internal name.
        /// </summary>
        /// <param name="gameName">The internal name (suffix) of the game.</param>
        /// <returns>The index of the specified CnCNet game. -1 if the game is unknown or not supported.</returns>
        public int GetGameIndexFromInternalName(string gameName)
        {
            for (int gId = 0; gId < GameList.Count; gId++)
            {
                CnCNetGame game = GameList[gId];

                if (gameName.ToLowerInvariant() == game.InternalName)
                    return gId;
            }

            return -1;
        }

        /// <summary>
        /// Seeks the supported game list for a specific game's internal name and if found,
        /// returns the game's full name. Otherwise returns the internal name specified in the param.
        /// </summary>
        /// <param name="gameName">The internal name of the game to seek for.</param>
        /// <returns>The full name of a supported game based on its internal name.
        /// Returns the given parameter if the name isn't found in the supported game list.</returns>
        public string GetGameNameFromInternalName(string gameName)
        {
            CnCNetGame game = GameList.Find(g => g.InternalName == gameName.ToLowerInvariant());

            if (game == null)
                return gameName;

            return game.UIName;
        }

        /// <summary>
        /// Returns the full UI name of a game based on its index in the game list.
        /// </summary>
        /// <param name="gameIndex">The index of the CnCNet supported game.</param>
        /// <returns>The UI name of the game.</returns>
        public string GetFullGameNameFromIndex(int gameIndex)
        {
            return GameList[gameIndex].UIName;
        }

        /// <summary>
        /// Returns the internal name of a game based on its index in the game list.
        /// </summary>
        /// <param name="gameIndex">The index of the CnCNet supported game.</param>
        /// <returns>The internal name (suffix) of the game.</returns>
        public string GetGameIdentifierFromIndex(int gameIndex)
        {
            return GameList[gameIndex].InternalName;
        }

        public string GetGameBroadcastingChannelNameFromIdentifier(string gameIdentifier)
        {
            CnCNetGame game = GameList.Find(g => g.InternalName == gameIdentifier.ToLowerInvariant());
            if (game == null)
                return null;
            return game.GameBroadcastChannel;
        }

        public string GetGameChatChannelNameFromIdentifier(string gameIdentifier)
        {
            CnCNetGame game = GameList.Find(g => g.InternalName == gameIdentifier.ToLowerInvariant());
            if (game == null)
                return null;
            return game.ChatChannel;
        }
    }

    /// <summary>
    /// An exception that is thrown when configuration for a game to add to game collection
    /// contains invalid or unexpected settings / data or required settings / data are missing.
    /// </summary>
    class GameCollectionConfigurationException : Exception
    {
        public GameCollectionConfigurationException(string message) : base(message)
        {
        }
    }
}