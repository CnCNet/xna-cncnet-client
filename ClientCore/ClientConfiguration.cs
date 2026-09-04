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
        private const string V3_TUNNEL_NEGOTIATION = "V3TunnelNegotiation";
        private const string V3_MATCHMAKING = "V3Matchmaking";

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
        private readonly string[] skillLevelOptions;
        private readonly int maxSkillLevelIndex;

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

            skillLevelOptions = SkillLevelOptions.SplitWithCleanup();
            maxSkillLevelIndex = Math.Max(0, skillLevelOptions.Length - 1);
            if (maxSkillLevelIndex == 0)
                throw new ClientConfigurationException("No skill level options defined in ClientDefinitions.ini.");
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

        public bool IgnoreBattleIni => clientDefinitionsIni.GetBooleanValue(SETTINGS, "IgnoreBattleIni", false);

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

        public string[] AlwaysTrustedDomains = { "cncnet.org", "gamesurge.net", "dronebl.org" };

        public bool ShowGameIconInGameList => clientDefinitionsIni.GetBooleanValue(SETTINGS, "ShowGameIconInGameList", true);

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

        public bool SettingsIniAsKeyboardIni => SettingsIniName == KeyboardINI;

        public string KeyboardHotkeySection => clientDefinitionsIni.GetStringValue(
            SETTINGS,
            "KeyboardHotkeySection",
            ClientGameType == ClientType.RA ? "WinHotKeys" : "Hotkey");

        public int MinimumIngameWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumIngameWidth", 640);

        public int MinimumIngameHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumIngameHeight", 480);

        public int MaximumIngameWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumIngameWidth", int.MaxValue);

        public int MaximumIngameHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumIngameHeight", int.MaxValue);

        public string[] CustomIngameResolutions => clientDefinitionsIni.GetStringListValue(SETTINGS, "CustomIngameResolutions", string.Empty);

        public bool CopyMissionsToSpawnmapINI => clientDefinitionsIni.GetBooleanValue(SETTINGS, "CopyMissionsToSpawnmapINI", true);

        public string AllowedCustomGameModes => clientDefinitionsIni.GetStringValue(SETTINGS, "AllowedCustomGameModes", "Standard,Custom Map");

        public int InactiveHostWarningMessageSeconds => clientDefinitionsIni.GetIntValue(SETTINGS, "InactiveHostWarningMessageSeconds", 0);

        public int InactiveHostKickSeconds => clientDefinitionsIni.GetIntValue(SETTINGS, "InactiveHostKickSeconds", 0) + InactiveHostWarningMessageSeconds;

        public bool InactiveHostKickEnabled => InactiveHostWarningMessageSeconds > 0 && InactiveHostKickSeconds > 0;

        public string SkillLevelOptions => clientDefinitionsIni.GetStringValue(SETTINGS, "SkillLevelOptions", "Any,Beginner,Intermediate,Pro");

        public string[] GetSkillLevelOptions() => skillLevelOptions;

        public int NormalizeSkillLevel(int skillLevel) => Math.Clamp(skillLevel, 0, maxSkillLevelIndex);

        public int DefaultSkillLevelIndex => NormalizeSkillLevel(clientDefinitionsIni.GetIntValue(SETTINGS, "DefaultSkillLevelIndex", 0));

        public bool CampaignTagSelectorEnabled => clientDefinitionsIni.GetBooleanValue(SETTINGS, "CampaignTagSelectorEnabled", false);

        public bool ReturnToMainMenuOnMissionLaunch => clientDefinitionsIni.GetBooleanValue(SETTINGS, "ReturnToMainMenuOnMissionLaunch", true);

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

        /// <summary>
        /// Semicolon-delimited list of additional STUN server hosts to query for P2P endpoint discovery.
        /// Falls back to official/recommended tunnel addresses if not set.
        /// </summary>
        public string P2PStunServers => networkDefinitionsIni.GetStringValue(SETTINGS, "P2PStunServers", string.Empty);

        public List<string> IRCServers => GetIRCServers();

        #endregion

        #region V3 tunnel negotiation

        /// <summary>
        /// How long (ms) the non-decider in a pairwise negotiation keeps sending Connected
        /// packets to candidate tunnels overall before giving up.
        /// </summary>
        public int V3NonDeciderTotalTimeoutMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "NonDeciderTotalTimeoutMs", 20000);

        /// <summary>
        /// How long (ms) to wait for a candidate tunnel to answer the Connected handshake before
        /// treating it as unreachable, when the pair could not agree a shortlist through
        /// matchmaking. Long, because the two peers begin negotiating off different IRC events and
        /// can be many seconds apart.
        /// </summary>
        public int V3ConnectedPhaseTimeoutMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "ConnectedPhaseTimeoutMs", 15000);

        /// <summary>
        /// How long (ms) to wait for the Connected handshake when matchmaking agreed the shortlist.
        /// Short, because that exchange leaves both peers entering the relay negotiation within
        /// about a second of each other. This is what an unreachable candidate costs.
        /// </summary>
        public int V3ConnectedPhaseTimeoutSyncedMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "ConnectedPhaseTimeoutSyncedMs", 5000);

        /// <summary>
        /// How long (ms) the decider waits for a candidate tunnel's ping cycle to finish once
        /// connected. If this elapses first, the tunnel is judged on whatever pings did arrive.
        /// Must exceed <see cref="V3PingsPerTunnel"/> × <see cref="V3PingTimeoutMs"/>, or a tunnel
        /// losing packets gets judged on a truncated sample.
        /// </summary>
        public int V3DeciderPingPhaseTimeoutMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "DeciderPingPhaseTimeoutMs", 12000);

        /// <summary>
        /// Number of pings sent to each candidate relay tunnel during negotiation.
        /// </summary>
        public int V3PingsPerTunnel => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "PingsPerTunnel", 10);

        /// <summary>
        /// Timeout (ms) for a single ping to a candidate relay tunnel before it's considered dropped.
        /// </summary>
        public int V3PingTimeoutMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "PingTimeoutMs", 1000);

        /// <summary>
        /// Number of pings sent to each candidate direct P2P path during the upgrade round.
        /// </summary>
        /// <remarks>
        /// Fewer than <see cref="V3PingsPerTunnel"/>, since a direct path answers in single-digit
        /// milliseconds and dead candidates are the common case. Not many fewer, though: the
        /// packet-loss penalty is a percentage, so the sample count sets how coarsely a direct path
        /// is judged against the relay it must beat — one lost probe out of four reads as 25% loss
        /// and outweighs the whole latency advantage of a LAN path, whatever its real quality.
        /// </remarks>
        public int V3P2PPingsPerTunnel => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "P2PPingsPerTunnel", 6);

        /// <summary>
        /// Timeout (ms) for a single ping to a candidate direct P2P path before it's considered dropped.
        /// </summary>
        public int V3P2PPingTimeoutMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "P2PPingTimeoutMs", 1000);

        /// <summary>
        /// Delay (ms) between the non-decider's repeated Connected packets to a single candidate tunnel.
        /// </summary>
        public int V3NonDeciderConnectedIntervalMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "NonDeciderConnectedIntervalMs", 500);

        /// <summary>
        /// How long (ms) the P2P upgrade round waits for the peer's candidate addresses.
        /// </summary>
        public int V3P2PCandidateExchangeTimeoutMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "P2PCandidateExchangeTimeoutMs", 3000);

        /// <summary>
        /// How many copies of the local P2P candidate list are sent over the relay during the
        /// upgrade round.
        /// </summary>
        /// <remarks>
        /// The exchange is unacknowledged and the two directions are independent, so losing every
        /// copy costs the pair its direct connection for the whole round — the peer gives up
        /// without punching, and this side then has no direct path that answers. Cheap insurance:
        /// the payload is six bytes per address.
        /// </remarks>
        public int V3P2PCandidateSendCount => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "P2PCandidateSendCount", 3);

        /// <summary>
        /// Delay (ms) between copies of the local P2P candidate list, so they are not all lost to
        /// the same burst of congestion.
        /// </summary>
        public int V3P2PCandidateSendIntervalMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "P2PCandidateSendIntervalMs", 150);

        /// <summary>
        /// How long (ms) the non-decider waits for the P2P upgrade tunnel choice before falling back to the relay.
        /// </summary>
        public int V3P2PUpgradeNonDeciderTimeoutMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "P2PUpgradeNonDeciderTimeoutMs", 10000);

        /// <summary>
        /// How long (ms) the decider waits for the peer to start punching a direct P2P path before falling back to the relay.
        /// </summary>
        public int V3P2PUpgradeConnectedTimeoutMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "P2PUpgradeConnectedTimeoutMs", 3000);

        /// <summary>
        /// Delay (ms) between retries of the decider's TunnelChoice message while waiting for the non-decider's ack.
        /// </summary>
        public int V3TunnelChoiceRetryIntervalMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "TunnelChoiceRetryIntervalMs", 1000);

        /// <summary>
        /// How many of the decider's TunnelChoice retries the non-decider stays available to answer
        /// after its round is already won, expressed as a count of
        /// <see cref="V3TunnelChoiceRetryIntervalMs"/>. 0 tears down as soon as the ack is sent.
        /// </summary>
        /// <remarks>
        /// The ack is a single unacknowledged datagram, so without this a client that loses it
        /// takes the decider's whole retry budget with it: the pair is reported as having failed to
        /// establish the path it actually agreed, and the two sides end up on different tunnels
        /// without either being able to detect it. Costs nothing but a slightly later teardown —
        /// the pair already reads as succeeded by then.
        /// </remarks>
        public int V3NonDeciderAckLingerRetries => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "NonDeciderAckLingerRetries", 3);

        /// <summary>
        /// Maximum number of retries for the decider's TunnelChoice message before giving up on the negotiation.
        /// </summary>
        public int V3TunnelChoiceMaxRetries => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "TunnelChoiceMaxRetries", 10);

        /// <summary>
        /// Fraction (0-1) of candidate tunnels that must finish their ping cycle before the decider
        /// picks a winner, rather than waiting for every candidate. 1.0 waits for all of them.
        /// </summary>
        /// <remarks>
        /// Raised now that matchmaking cuts the candidates to a handful; discarding several on
        /// timing alone throws away the measurement the shortlist exists to inform. Kept below 1.0
        /// so a single dead candidate cannot hold the round to the ping-phase timeout — at the
        /// default candidate count this waits for all but one.
        /// </remarks>
        public double V3EarlySelectionThreshold => networkDefinitionsIni.GetDoubleValue(V3_TUNNEL_NEGOTIATION, "EarlySelectionThreshold", 0.8);

        /// <summary>
        /// Ping penalty (ms) applied per percentage point of packet loss when ranking negotiated tunnels/paths.
        /// </summary>
        public int V3PacketLossWeight => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "PacketLossWeight", 10);

        /// <summary>
        /// How often (seconds) the currently-selected tunnel server is pinged in V2/V3 static mode.
        /// </summary>
        public double V3CurrentTunnelPingIntervalSeconds => networkDefinitionsIni.GetDoubleValue(V3_TUNNEL_NEGOTIATION, "CurrentTunnelPingIntervalSeconds", 20.0);

        /// <summary>
        /// Number of ping cycles between automatic refreshes of the full tunnel server list.
        /// </summary>
        public uint V3CyclesPerTunnelListRefresh => (uint)networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "CyclesPerTunnelListRefresh", 3);

        /// <summary>
        /// Ping (ms) above which a tunnel server is considered to be responding badly.
        /// </summary>
        public int V3TunnelFailedPingAmountMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "TunnelFailedPingAmountMs", 2000);

        /// <summary>
        /// Number of consecutive bad pings before a tunnel server is reported as failed.
        /// </summary>
        public int V3TunnelFailedConsecutivePings => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "TunnelFailedConsecutivePings", 2);

        /// <summary>
        /// How many unanswered ICMP probes in a row a tunnel's last good ping is kept for before
        /// it reads as unknown. 0 discards the measurement on the first miss.
        /// </summary>
        /// <remarks>
        /// Only one probe is sent per tunnel list refresh, so on a lossy connection a single lost
        /// datagram would otherwise leave a healthy tunnel unmeasured for a whole refresh
        /// interval — long enough for matchmaking to drop it from the shortlist and never try it.
        /// Kept small: a tunnel that has actually gone away should stop advertising a stale
        /// latency quickly, and tunnel-failure detection reads the raw probe result regardless.
        /// </remarks>
        public int V3RetainedPingFailures => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "RetainedPingFailures", 2);

        /// <summary>
        /// Interval (seconds) between P2P keepalive ping rounds sent to negotiated peers.
        /// </summary>
        public double V3KeepAliveIntervalSeconds => networkDefinitionsIni.GetDoubleValue(V3_TUNNEL_NEGOTIATION, "KeepAliveIntervalSeconds", 15.0);

        /// <summary>
        /// How often (seconds) the keepalive monitor's timer ticks to check for due pings/misses.
        /// </summary>
        public double V3KeepAliveTickSeconds => networkDefinitionsIni.GetDoubleValue(V3_TUNNEL_NEGOTIATION, "KeepAliveTickSeconds", 5.0);

        /// <summary>
        /// Number of consecutive missed keepalive pings before a P2P path is treated as dead.
        /// </summary>
        public int V3KeepAliveMaxMisses => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "KeepAliveMaxMisses", 3);

        /// <summary>
        /// How long (seconds) a P2P probe reply is cached for before it's queried again.
        /// </summary>
        public double V3ProbeReplyCacheSeconds => networkDefinitionsIni.GetDoubleValue(V3_TUNNEL_NEGOTIATION, "ProbeReplyCacheSeconds", 5.0);

        #endregion

        #region V3 matchmaking tunnels

        /// <summary>
        /// Whether the matchmaking phase runs before dynamic-tunnel negotiation. When disabled, or
        /// when no matchmaking tunnel is reachable, each pair negotiates over its own
        /// locally-ranked shortlist instead, which works but picks a worse tunnel.
        /// </summary>
        public bool V3MatchmakingEnabled => networkDefinitionsIni.GetBooleanValue(V3_MATCHMAKING, "Enabled", true);

        /// <summary>
        /// Number of relay tunnels the matchmaking phase shortlists for a pair to negotiate over.
        /// </summary>
        public int V3MatchmakingCandidateCount => networkDefinitionsIni.GetIntValue(V3_MATCHMAKING, "CandidateCount", 6);

        /// <summary>
        /// How many of the shortlist's slots are reserved for tunnels outside the top pick's
        /// country, rather than being filled by score.
        /// </summary>
        /// <remarks>
        /// Ranking on combined latency alone returns tunnels in the same region, often on the same
        /// transit, so one bad link can take out every candidate at once. The reserved slot will
        /// usually lose on latency and never be chosen, so it costs little.
        /// </remarks>
        public int V3MatchmakingDiversitySlots => networkDefinitionsIni.GetIntValue(V3_MATCHMAKING, "DiversitySlots", 1);

        /// <summary>
        /// Fraction (0-1) of a tunnel's advertised capacity at which it stops being shortlisted,
        /// so negotiation never steers players onto a server that is about to reject them.
        /// </summary>
        public double V3MatchmakingCapacityThreshold => networkDefinitionsIni.GetDoubleValue(V3_MATCHMAKING, "CapacityThreshold", 0.85);

        /// <summary>
        /// How long (ms) to wait for the peer's tunnel list, and for the decider's shortlist,
        /// before falling back to a locally-ranked shortlist. Sized to absorb the gap between the
        /// two peers starting their negotiations, not the round trip itself. The wait ends as soon
        /// as the exchange completes, so it only costs time when a peer is missing.
        /// </summary>
        public int V3MatchmakingExchangeTimeoutMs => networkDefinitionsIni.GetIntValue(V3_MATCHMAKING, "ExchangeTimeoutMs", 10000);

        /// <summary>
        /// Delay (ms) between repeats of the tunnel list while waiting for the shortlist. The list
        /// is sent over every matchmaking server, so a repeat covers the loss of a whole server
        /// rather than just a datagram.
        /// </summary>
        public int V3MatchmakingRetryIntervalMs => networkDefinitionsIni.GetIntValue(V3_MATCHMAKING, "RetryIntervalMs", 400);

        /// <summary>
        /// Size of the shortlist each side falls back to when the matchmaking exchange fails.
        /// Larger than <see cref="V3MatchmakingCandidateCount"/> on purpose: after the
        /// deterministic slots, the remainder is ranked locally by each side, and the extra
        /// entries raise the chance those locally ranked halves overlap too.
        /// </summary>
        public int V3MatchmakingFallbackCandidateCount => networkDefinitionsIni.GetIntValue(V3_MATCHMAKING, "FallbackCandidateCount", 8);

        /// <summary>
        /// How many of the fallback shortlist's slots are filled deterministically from the
        /// pair's player IDs rather than by local ranking. Both peers compute the same slots
        /// without communicating, so a pair whose locally ranked lists share nothing (e.g. South
        /// America vs South Africa) still holds candidates in common. 0 restores a purely local
        /// ranking, which can leave such a pair with no mutual tunnel at all.
        /// </summary>
        public int V3MatchmakingFallbackDeterministicSlots => networkDefinitionsIni.GetIntValue(V3_MATCHMAKING, "FallbackDeterministicSlots", 4);

        /// <summary>
        /// How many negotiations in a row may test a tunnel without a single packet arriving
        /// through it before it is ranked below tunnels that have not failed. 0 ranks on latency
        /// alone.
        /// </summary>
        /// <remarks>
        /// A tunnel that answers ICMP but drops UDP looks ideal to a latency ranking, then wastes a
        /// candidate slot and the entire connect budget. Demoted rather than excluded: it is still
        /// shortlisted when too few others exist, and one success clears the count.
        /// </remarks>
        public int V3MatchmakingHandshakeFailureThreshold => networkDefinitionsIni.GetIntValue(V3_MATCHMAKING, "HandshakeFailureThreshold", 2);

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

        public string CustomMissionPath => clientDefinitionsIni.GetStringValue(SETTINGS, "CustomMissionPath", "Maps/CustomMissions");

        public List<(string extension, string copyAs)> GetCustomMissionSupplementFiles()
        {
            List<(string extension, string copyAs)> files = new();
            Dictionary<string, int> extensionToIndex = new(StringComparer.OrdinalIgnoreCase);

            int index = 0;
            while (true)
            {
                string extensionKey = $"CustomMissionSupplementFile{index}Extension";
                string copyAsKey = $"CustomMissionSupplementFile{index}CopyAs";

                string extension = clientDefinitionsIni.GetStringValue(SETTINGS, extensionKey, null)?.Trim();

                // Stop iteration if the extension key is missing
                if (string.IsNullOrWhiteSpace(extension))
                    break;

                string copyAs = clientDefinitionsIni.GetStringValue(SETTINGS, copyAsKey, null);

                // Validate that copyAs is not empty
                if (string.IsNullOrWhiteSpace(copyAs))
                    throw new ClientConfigurationException($"Configuration key '{copyAsKey}' is required when '{extensionKey}' is present for supplement file {index}.");

                // Validate that extension is unique
                if (extensionToIndex.TryGetValue(extension, out int firstIndex))
                    throw new ClientConfigurationException($"Duplicate extension '{extension}' found in supplement files. Extension is used in both file {firstIndex} and file {index}.");

                extensionToIndex.Add(extension, index);
                files.Add((extension, copyAs));
                index++;
            }

            return files;
        }

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
