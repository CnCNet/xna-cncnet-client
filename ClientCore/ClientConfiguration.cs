using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.I18N;

using Rampastring.Tools;

namespace ClientCore
{
    public class ClientConfiguration
    {
        private const string GENERAL = "General";
        private const string AUDIO = "Audio";
        private const string SETTINGS = "Settings";
        private const string LINKS = "Links";
        private const string TRANSLATIONS = "Translations";
        private const string USER_DEFAULTS = "UserDefaults";

        public const string CLIENT_SETTINGS = "DTACnCNetClient.ini";
        public const string GAME_OPTIONS = "GameOptions.ini";
        public const string CLIENT_DEFS = "ClientDefinitions.ini";
        public const string NETWORK_DEFS_LOCAL = "NetworkDefinitions.local.ini";
        public const string NETWORK_DEFS = "NetworkDefinitions.ini";

        private static ClientConfiguration _instance;

        private IniFile gameOptions_ini;
        private IniFile DTACnCNetClient_ini;
        private IniFile clientDefinitionsIni;
        private IniFile networkDefinitionsIni;

        protected ClientConfiguration()
        {
            var baseResourceDirectory = SafePath.GetDirectory(ProgramConstants.GetBaseResourcePath());

            if (!baseResourceDirectory.Exists)
                throw new FileNotFoundException($"Couldn't find {CLIENT_DEFS} at {baseResourceDirectory} (directory doesn't exist). Please verify that you're running the client from the correct directory.");

            FileInfo clientDefinitionsFile = SafePath.GetFile(baseResourceDirectory.FullName, CLIENT_DEFS);

            if (!(clientDefinitionsFile?.Exists ?? false))
                throw new FileNotFoundException($"Couldn't find {CLIENT_DEFS} at {baseResourceDirectory}. Please verify that you're running the client from the correct directory.");

            clientDefinitionsIni = new IniFile(clientDefinitionsFile.FullName);

            DTACnCNetClient_ini = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetResourcePath(), CLIENT_SETTINGS));

            gameOptions_ini = new IniFile(SafePath.CombineFilePath(baseResourceDirectory.FullName, GAME_OPTIONS));

            string networkDefsPathLocal = SafePath.CombineFilePath(ProgramConstants.GetResourcePath(), NETWORK_DEFS_LOCAL);
            if (File.Exists(networkDefsPathLocal))
            {
                networkDefinitionsIni = new IniFile(networkDefsPathLocal);
                Logger.Log("Loaded network definitions from NetworkDefinitions.local.ini (user override)");
            }
            else
            {
                string networkDefsPath = SafePath.CombineFilePath(ProgramConstants.GetResourcePath(), NETWORK_DEFS);
                networkDefinitionsIni = new IniFile(networkDefsPath);
            }

            RefreshTranslationGameFiles();
        }

        /// <summary>
        /// Singleton Pattern. Returns the object of this class.
        /// </summary>
        /// <returns>The object of the ClientConfiguration class.</returns>
        public static ClientConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ClientConfiguration();
                }
                return _instance;
            }
        }

        public void RefreshSettings()
        {
            DTACnCNetClient_ini = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetResourcePath(), CLIENT_SETTINGS));
        }

        #region Client settings

        private string _mainMenuMusicName = null;
        public string MainMenuMusicName => _mainMenuMusicName ??= GetMainMenuMusicName();
        private string GetMainMenuMusicName()
        {
            string raw = DTACnCNetClient_ini.GetStringValue(GENERAL, "MainMenuTheme", "mainmenu");
            string[] parts = raw.SplitWithCleanup();
            string chosen = parts.Length > 0
                ? parts[new Random().Next(parts.Length)]
                : "mainmenu";

            return SafePath.CombineFilePath(chosen);
        }

        public float DefaultAlphaRate => DTACnCNetClient_ini.GetSingleValue(GENERAL, "AlphaRate", 0.005f);

        public float CheckBoxAlphaRate => DTACnCNetClient_ini.GetSingleValue(GENERAL, "CheckBoxAlphaRate", 0.05f);

        public float IndicatorAlphaRate => DTACnCNetClient_ini.GetSingleValue(GENERAL, "IndicatorAlphaRate", 0.05f);

        #region Color settings

        public string UILabelColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "UILabelColor", "0,0,0");

        public string UIHintTextColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "HintTextColor", "128,128,128");

        public string DisabledButtonColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "DisabledButtonColor", "108,108,108");

        public string AltUIColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "AltUIColor", "255,255,255");

        public string ButtonHoverColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "ButtonHoverColor", "255,192,192");

        public string MapPreviewNameBackgroundColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "MapPreviewNameBackgroundColor", "0,0,0,144");

        public string MapPreviewNameBorderColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "MapPreviewNameBorderColor", "128,128,128,128");

        public string MapPreviewStartingLocationHoverRemapColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "StartingLocationHoverColor", "255,255,255,128");

        public bool MapPreviewStartingLocationUsePlayerRemapColor => DTACnCNetClient_ini.GetBooleanValue(GENERAL, "StartingLocationsUsePlayerRemapColor", false);

        public string AltUIBackgroundColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "AltUIBackgroundColor", "196,196,196");

        public string WindowBorderColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "WindowBorderColor", "128,128,128");

        public string PanelBorderColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "PanelBorderColor", "255,255,255");

        public string ListBoxHeaderColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "ListBoxHeaderColor", "255,255,255");

        public string DefaultChatColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "DefaultChatColor", "0,255,0");

        public string AdminNameColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "AdminNameColor", "255,0,0");

        public string ReceivedPMColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "PrivateMessageOtherUserColor", "196,196,196");

        public string SentPMColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "PrivateMessageColor", "128,128,128");

        public int DefaultPersonalChatColorIndex => DTACnCNetClient_ini.GetIntValue(GENERAL, "DefaultPersonalChatColorIndex", 0);

        public string ListBoxFocusColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "ListBoxFocusColor", "64,64,168");

        public string HoverOnGameColor => DTACnCNetClient_ini.GetStringValue(GENERAL, "HoverOnGameColor", "32,32,84");

        public IniSection GetParserConstants() => DTACnCNetClient_ini.GetSection("ParserConstants");

        #endregion

        #region Tool tip settings

        public int ToolTipFontIndex => DTACnCNetClient_ini.GetIntValue(GENERAL, "ToolTipFontIndex", 0);

        public int ToolTipOffsetX => DTACnCNetClient_ini.GetIntValue(GENERAL, "ToolTipOffsetX", 0);

        public int ToolTipOffsetY => DTACnCNetClient_ini.GetIntValue(GENERAL, "ToolTipOffsetY", 0);

        public int ToolTipMargin => DTACnCNetClient_ini.GetIntValue(GENERAL, "ToolTipMargin", 4);

        public float ToolTipDelay => DTACnCNetClient_ini.GetSingleValue(GENERAL, "ToolTipDelay", 0.67f);

        public float ToolTipAlphaRatePerSecond => DTACnCNetClient_ini.GetSingleValue(GENERAL, "ToolTipAlphaRate", 4.0f);

        #endregion

        #region Audio options

        public float SoundGameLobbyJoinCooldown => DTACnCNetClient_ini.GetSingleValue(AUDIO, "SoundGameLobbyJoinCooldown", 0.25f);

        public float SoundGameLobbyLeaveCooldown => DTACnCNetClient_ini.GetSingleValue(AUDIO, "SoundGameLobbyLeaveCooldown", 0.25f);

        public float SoundMessageCooldown => DTACnCNetClient_ini.GetSingleValue(AUDIO, "SoundMessageCooldown", 0.25f);

        public float SoundPrivateMessageCooldown => DTACnCNetClient_ini.GetSingleValue(AUDIO, "SoundPrivateMessageCooldown", 0.25f);

        public float SoundGameLobbyGetReadyCooldown => DTACnCNetClient_ini.GetSingleValue(AUDIO, "SoundGameLobbyGetReadyCooldown", 5.0f);

        public float SoundGameLobbyReturnCooldown => DTACnCNetClient_ini.GetSingleValue(AUDIO, "SoundGameLobbyReturnCooldown", 1.0f);

        #endregion

        #endregion

        #region Game options

        public string Sides => gameOptions_ini.GetStringValue(GENERAL, nameof(Sides), "GDI,Nod,Allies,Soviet");

        public string InternalSideIndices => gameOptions_ini.GetStringValue(GENERAL, nameof(InternalSideIndices), string.Empty);

        public string SpectatorInternalSideIndex => gameOptions_ini.GetStringValue(GENERAL, nameof(SpectatorInternalSideIndex), string.Empty);

        #endregion

        #region Client definitions

        private string _ClientGameTypeString => clientDefinitionsIni.GetStringValue(SETTINGS, "ClientGameType", string.Empty);
        private ClientType? _ClientGameType = null;
        public ClientType ClientGameType => _ClientGameType ??= ClientTypeHelper.FromString(_ClientGameTypeString);

        public string DiscordAppId => clientDefinitionsIni.GetStringValue(SETTINGS, "DiscordAppId", string.Empty);

        public int SendSleep => clientDefinitionsIni.GetIntValue(SETTINGS, "SendSleep", 2500);

        public int LoadingScreenCount => clientDefinitionsIni.GetIntValue(SETTINGS, "LoadingScreenCount", 2);

        public int ThemeCount => clientDefinitionsIni.GetSectionKeys("Themes").Count;

        public string LocalGame => clientDefinitionsIni.GetStringValue(SETTINGS, "LocalGame", "DTA");

        public bool SidebarHack => clientDefinitionsIni.GetBooleanValue(SETTINGS, "SidebarHack", false);

        public int MinimumRenderWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumRenderWidth", 1280);

        public int MinimumRenderHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumRenderHeight", 768);

        public int MaximumRenderWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumRenderWidth", 1280);

        public int MaximumRenderHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumRenderHeight", 800);

        public string[] RecommendedResolutions => clientDefinitionsIni.GetStringListValue(SETTINGS, "RecommendedResolutions",
            $"{MinimumRenderWidth}x{MinimumRenderHeight},{MaximumRenderWidth}x{MaximumRenderHeight}");

        public string WindowTitle => clientDefinitionsIni.GetStringValue(SETTINGS, "WindowTitle", string.Empty)
            .L10N("INI:ClientDefinitions:WindowTitle");

        public string InstallationPathRegKey => clientDefinitionsIni.GetStringValue(SETTINGS, "RegistryInstallPath", "TiberianSun");

        public string CnCNetLiveStatusIdentifier => clientDefinitionsIni.GetStringValue(SETTINGS, "CnCNetLiveStatusIdentifier", "cncnet5_ts");

        public string BattleFSFileName => clientDefinitionsIni.GetStringValue(SETTINGS, "BattleFSFileName", "BattleFS.ini");

        public string MapEditorExePath => SafePath.CombineFilePath(clientDefinitionsIni.GetStringValue(SETTINGS, "MapEditorExePath", SafePath.CombineFilePath("FinalSun", "FinalSun.exe")));

        public string UnixMapEditorExePath => clientDefinitionsIni.GetStringValue(SETTINGS, "UnixMapEditorExePath", Instance.MapEditorExePath);

        public bool ModMode => clientDefinitionsIni.GetBooleanValue(SETTINGS, "ModMode", false);

        public string LongGameName => clientDefinitionsIni.GetStringValue(SETTINGS, "LongGameName", "Tiberian Sun");

        public string LongSupportURL => clientDefinitionsIni.GetStringValue(SETTINGS, "LongSupportURL", "https://www.moddb.com/members/rampastring");

        public string ShortSupportURL => clientDefinitionsIni.GetStringValue(SETTINGS, "ShortSupportURL", "www.moddb.com/members/rampastring");

        public string ChangelogURL => clientDefinitionsIni.GetStringValue(SETTINGS, "ChangelogURL", "https://www.moddb.com/mods/the-dawn-of-the-tiberium-age/tutorials/change-log");

        public string CreditsURL => clientDefinitionsIni.GetStringValue(SETTINGS, "CreditsURL", "https://www.moddb.com/mods/the-dawn-of-the-tiberium-age/tutorials/credits#Rampastring");

        public string ManualDownloadURL => clientDefinitionsIni.GetStringValue(SETTINGS, "ManualDownloadURL", string.Empty);

        public string FinalSunIniPath => SafePath.CombineFilePath(clientDefinitionsIni.GetStringValue(SETTINGS, "FSIniPath", SafePath.CombineFilePath("FinalSun", "FinalSun.ini")));

        public int MaxNameLength => clientDefinitionsIni.GetIntValue(SETTINGS, "MaxNameLength", 16);

        public bool UseIsometricCells => clientDefinitionsIni.GetBooleanValue(SETTINGS, "UseIsometricCells", true);

        public int WaypointCoefficient => clientDefinitionsIni.GetIntValue(SETTINGS, "WaypointCoefficient", 128);

        public int MapCellSizeX => clientDefinitionsIni.GetIntValue(SETTINGS, "MapCellSizeX", 48);

        public int MapCellSizeY => clientDefinitionsIni.GetIntValue(SETTINGS, "MapCellSizeY", 24);

        public bool UseBuiltStatistic => clientDefinitionsIni.GetBooleanValue(SETTINGS, "UseBuiltStatistic", false);

        public string WindowedModeKey => clientDefinitionsIni.GetStringValue(SETTINGS, "WindowedModeKey", "Video.Windowed");

        public bool CopyResolutionDependentLanguageDLL => clientDefinitionsIni.GetBooleanValue(SETTINGS, "CopyResolutionDependentLanguageDLL", true);

        public string StatisticsLogFileName => clientDefinitionsIni.GetStringValue(SETTINGS, "StatisticsLogFileName", "DTA.LOG");

        public string[] TrustedDomains => clientDefinitionsIni.GetStringListValue(SETTINGS, "TrustedDomains", string.Empty);

        public string[] AlwaysTrustedDomains = {"cncnet.org", "gamesurge.net", "dronebl.org", "discord.com", "discord.gg", "youtube.com", "youtu.be"};

        public (string Name, string Path) GetThemeInfoFromIndex(int themeIndex) => clientDefinitionsIni.GetStringValue("Themes", themeIndex.ToString(), ",").Split(',').AsTuple2();

        /// <summary>
        /// Returns the directory path for a theme, or null if the specified
        /// theme name doesn't exist.
        /// </summary>
        /// <param name="themeName">The name of the theme.</param>
        /// <returns>The path to the theme's directory.</returns>
        public string GetThemePath(string themeName)
        {
            var themeSection = clientDefinitionsIni.GetSection("Themes");
            foreach (var key in themeSection.Keys)
            {
                var (name, path) = key.Value.Split(',');
                if (name == themeName)
                    return path;
            }

            return null;
        }

        public string SettingsIniName => clientDefinitionsIni.GetStringValue(SETTINGS, "SettingsFile", "Settings.ini");

        public string TranslationIniName => clientDefinitionsIni.GetStringValue(TRANSLATIONS, nameof(TranslationIniName), "Translation.ini");

        public string TranslationsFolderPath => SafePath.CombineDirectoryPath(
            clientDefinitionsIni.GetStringValue(TRANSLATIONS, "TranslationsFolder",
                SafePath.CombineDirectoryPath("Resources", "Translations")));

        private List<TranslationGameFile> _translationGameFiles;

        public List<TranslationGameFile> TranslationGameFiles => _translationGameFiles;

        /// <summary>
        /// Force a refresh of the translation game files list.
        /// </summary>
        public void RefreshTranslationGameFiles()
        {
            _translationGameFiles = ParseTranslationGameFiles();
        }

        /// <summary>
        /// Looks up the list of files to try and copy into the game folder with a translation.
        /// </summary>
        /// <returns>Source/destination relative path pairs.</returns>
        /// <exception cref="IniParseException">Thrown when the syntax of the list is invalid.</exception>
        private List<TranslationGameFile> ParseTranslationGameFiles()
        {
            List<TranslationGameFile> gameFiles = new();
            if (!clientDefinitionsIni.SectionExists(TRANSLATIONS))
                return gameFiles;

            foreach (string key in clientDefinitionsIni.GetSectionKeys(TRANSLATIONS))
            {
                // the syntax is GameFileX=path/to/source.file,path/to/destination.file[,checked]
                // where X can be any text or number
                if (!key.StartsWith("GameFile"))
                    continue;

                string value = clientDefinitionsIni.GetStringValue(TRANSLATIONS, key, string.Empty);
                string[] parts = clientDefinitionsIni.GetStringListValue(TRANSLATIONS, key, string.Empty);

                // fail explicitly if the syntax is wrong
                if (parts.Length is < 2 or > 3
                    || (parts.Length == 3 && parts[2].ToUpperInvariant() != "CHECKED"))
                {
                    throw new IniParseException($"Invalid syntax for value of {key}! " +
                        $"Expected path/to/source.file,path/to/destination.file[,checked], read {value}.");
                }

                bool isChecked = parts.Length == 3 && parts[2].ToUpperInvariant() == "CHECKED";

                gameFiles.Add(new(Source: parts[0].Trim(), Target: parts[1].Trim(), isChecked));
            }

            return gameFiles;
        }

        public string ExtraExeCommandLineParameters => clientDefinitionsIni.GetStringValue(SETTINGS, "ExtraCommandLineParams", string.Empty);

        public string MPMapsIniPath => SafePath.CombineFilePath(clientDefinitionsIni.GetStringValue(SETTINGS, "MPMapsPath", SafePath.CombineFilePath("INI", "MPMaps.ini")));

        public string KeyboardINI => clientDefinitionsIni.GetStringValue(SETTINGS, "KeyboardINI", "Keyboard.ini");

        public int MinimumIngameWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumIngameWidth", 640);

        public int MinimumIngameHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumIngameHeight", 480);

        public int MaximumIngameWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumIngameWidth", int.MaxValue);

        public int MaximumIngameHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumIngameHeight", int.MaxValue);

        public bool CopyMissionsToSpawnmapINI => clientDefinitionsIni.GetBooleanValue(SETTINGS, "CopyMissionsToSpawnmapINI", true);

        public string AllowedCustomGameModes => clientDefinitionsIni.GetStringValue(SETTINGS, "AllowedCustomGameModes", "Standard,Custom Map");

        public int InactiveHostWarningMessageSeconds => clientDefinitionsIni.GetIntValue(SETTINGS, "InactiveHostWarningMessageSeconds", 0);

        public int InactiveHostKickSeconds => clientDefinitionsIni.GetIntValue(SETTINGS, "InactiveHostKickSeconds", 0) + InactiveHostWarningMessageSeconds;

        public bool InactiveHostKickEnabled => InactiveHostWarningMessageSeconds > 0 && InactiveHostKickSeconds > 0;

        public string SkillLevelOptions => clientDefinitionsIni.GetStringValue(SETTINGS, "SkillLevelOptions", "Any,Beginner,Intermediate,Pro");
        
        public int DefaultSkillLevelIndex => clientDefinitionsIni.GetIntValue(SETTINGS, "DefaultSkillLevelIndex", 0);
        
        public string GetGameExecutableName()
        {
            string[] exeNames = clientDefinitionsIni.GetStringListValue(SETTINGS, "GameExecutableNames", "Game.exe");

            return exeNames[0];
        }

        public string GameLauncherExecutableName => clientDefinitionsIni.GetStringValue(SETTINGS, "GameLauncherExecutableName", string.Empty);

        public string[] GetCompatibilityCheckExecutables()
        {
            string[] exeNames = clientDefinitionsIni.GetStringListValue(SETTINGS, "CompatibilityCheckExecutables", string.Empty);

            return exeNames;
        }

        public bool SaveSkirmishGameOptions => clientDefinitionsIni.GetBooleanValue(SETTINGS, "SaveSkirmishGameOptions", false);
        
        public bool SaveCampaignGameOptions => clientDefinitionsIni.GetBooleanValue(SETTINGS, "SaveCampaignGameOptions", false);

        public bool CreateSavedGamesDirectory => clientDefinitionsIni.GetBooleanValue(SETTINGS, "CreateSavedGamesDirectory", false);

        public bool DisableMultiplayerGameLoading => clientDefinitionsIni.GetBooleanValue(SETTINGS, "DisableMultiplayerGameLoading", false);

        public bool DisplayPlayerCountInTopBar => clientDefinitionsIni.GetBooleanValue(SETTINGS, "DisplayPlayerCountInTopBar", false);

        /// <summary>
        /// The name of the executable in the main game directory that selects
        /// the correct main client executable.
        /// For example, DTA.exe in case of DTA.
        /// </summary>
        public string LauncherExe => clientDefinitionsIni.GetStringValue(SETTINGS, "LauncherExe", string.Empty);

        public bool UseClientRandomStartLocations => clientDefinitionsIni.GetBooleanValue(SETTINGS, "UseClientRandomStartLocations", false);

        /// <summary>
        /// Returns the name of the game executable file that is used on
        /// Linux and macOS.
        /// </summary>
        public string UnixGameExecutableName => clientDefinitionsIni.GetStringValue(SETTINGS, "UnixGameExecutableName", "wine-dta.sh");

        /// <summary>
        /// List of files that are not distributed but required to play.
        /// </summary>
        public string[] RequiredFiles => clientDefinitionsIni.GetStringListValue(SETTINGS, "RequiredFiles", String.Empty);

        /// <summary>
        /// List of files that can interfere with the mod functioning.
        /// </summary>
        public string[] ForbiddenFiles => clientDefinitionsIni.GetStringListValue(SETTINGS, "ForbiddenFiles", String.Empty);

        /// <summary>
        /// The main map file extension that is read by the client.
        /// </summary>
        public string MapFileExtension => clientDefinitionsIni.GetStringValue(SETTINGS, "MapFileExtension", "map");
        
        /// <summary>
        /// This tells the client which supplemental map files are ok to copy over during "spawnmap.ini" file creation.
        /// IE, if "BIN" is listed, then the client will look for and copy the file "map_a.bin"
        /// when writing the spawnmap.ini file for map file "map_a.ini".
        /// 
        /// This configuration should be in the form "SupplementalMapFileExtensions=bin,mix"
        /// </summary>
        public IEnumerable<string> SupplementalMapFileExtensions
            => clientDefinitionsIni.GetStringValue(SETTINGS, "SupplementalMapFileExtensions", null)?
                .Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        /// <summary>
        /// This prevents users from joining games that are incompatible/on a different game version than the current user.
        /// Default: false
        /// </summary>
        public bool DisallowJoiningIncompatibleGames => clientDefinitionsIni.GetBooleanValue(SETTINGS, nameof(DisallowJoiningIncompatibleGames), false);

        /// <summary>
        /// Activates warnings for development builds of XNA Client
        /// </summary>
        public bool ShowDevelopmentBuildWarnings => clientDefinitionsIni.GetBooleanValue(SETTINGS, nameof(ShowDevelopmentBuildWarnings), true);

        #endregion

        #region Network definitions

        public string CnCNetTunnelListURL => networkDefinitionsIni.GetStringValue(SETTINGS, "CnCNetTunnelListURL", "https://cncnet.org/master-list");

        public string CnCNetPlayerCountURL => networkDefinitionsIni.GetStringValue(SETTINGS, "CnCNetPlayerCountURL", "https://api.cncnet.org/status");

        public string CnCNetMapDBDownloadURL => networkDefinitionsIni.GetStringValue(SETTINGS, "CnCNetMapDBDownloadURL", "https://mapdb.cncnet.org");

        public string CnCNetMapDBUploadURL => networkDefinitionsIni.GetStringValue(SETTINGS, "CnCNetMapDBUploadURL", "https://mapdb.cncnet.org/upload");

        public bool DisableDiscordIntegration => networkDefinitionsIni.GetBooleanValue(SETTINGS, "DisableDiscordIntegration", false);

        public List<string> IRCServers => GetIRCServers();

        #endregion

        #region User default settings

        public bool UserDefault_BorderlessWindowedClient => clientDefinitionsIni.GetBooleanValue(USER_DEFAULTS, "BorderlessWindowedClient", true);

        public bool UserDefault_IntegerScaledClient => clientDefinitionsIni.GetBooleanValue(USER_DEFAULTS, "IntegerScaledClient", false);

        public bool UserDefault_WriteInstallationPathToRegistry => clientDefinitionsIni.GetBooleanValue(USER_DEFAULTS, "WriteInstallationPathToRegistry", true);

        #endregion

        #region Game networking defaults

        /// <summary>
        /// Default value for FrameSendRate setting written in spawn.ini.
        /// </summary>
        public int DefaultFrameSendRate => clientDefinitionsIni.GetIntValue(SETTINGS, nameof(DefaultFrameSendRate), 7);

        /// <summary>
        /// Default value for Protocol setting written in spawn.ini.
        /// </summary>
        public int DefaultProtocolVersion => clientDefinitionsIni.GetIntValue(SETTINGS, nameof(DefaultProtocolVersion), 2);

        /// <summary>
        /// Default value for MaxAhead setting written in spawn.ini.
        /// </summary>
        public int DefaultMaxAhead => clientDefinitionsIni.GetIntValue(SETTINGS, nameof(DefaultMaxAhead), 0);

        #endregion

        public List<string> GetIRCServers()
        {
            List<string> servers = [];

            IniSection serversSection = networkDefinitionsIni.GetSection("IRCServers");
            if (serversSection != null)
                foreach ((_, string value) in serversSection.Keys)
                    if (!string.IsNullOrWhiteSpace(value))
                        servers.Add(value);

            return servers;
        }

        public bool DiscordIntegrationGloballyDisabled => string.IsNullOrWhiteSpace(DiscordAppId) || DisableDiscordIntegration;

        public OSVersion GetOperatingSystemVersion()
        {
#if NETFRAMEWORK
            // OperatingSystem.IsWindowsVersionAtLeast() is the preferred API but is not supported on earlier .NET versions
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Version osVersion = Environment.OSVersion.Version;

                if (osVersion.Major <= 4)
                    return OSVersion.UNKNOWN;

                if (osVersion.Major == 5)
                    return OSVersion.WINXP;

                if (osVersion.Major == 6 && osVersion.Minor == 0)
                    return OSVersion.WINVISTA;

                if (osVersion.Major == 6 && osVersion.Minor <= 1)
                    return OSVersion.WIN7;

                return OSVersion.WIN810;
            }

            if (ProgramConstants.ISMONO)
                return OSVersion.UNIX;

            // http://mono.wikia.com/wiki/Detecting_the_execution_platform
            int p = (int)Environment.OSVersion.Platform;
            if (p == 4 || p == 6 || p == 128)
                return OSVersion.UNIX;

            return OSVersion.UNKNOWN;
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (OperatingSystem.IsWindowsVersionAtLeast(6, 2))
                    return OSVersion.WIN810;
                else if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                    return OSVersion.WIN7;
                else if (OperatingSystem.IsWindowsVersionAtLeast(6, 0))
                    return OSVersion.WINVISTA;
                else if (OperatingSystem.IsWindowsVersionAtLeast(5, 0))
                    return OSVersion.WINXP;
                else
                    return OSVersion.UNKNOWN;
            }

            return OSVersion.UNIX;
#endif
        }
    }

    /// <summary>
    /// An exception that is thrown when a client configuration file contains invalid or
    /// unexpected settings / data or required settings / data are missing.
    /// </summary>
    public class ClientConfigurationException : Exception
    {
        public ClientConfigurationException(string message) : base(message)
        {
        }
    }
}
