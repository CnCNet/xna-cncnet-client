using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using ClientCore.Display;
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

        /// <summary>
        /// Returns the [AutoMapOverlays] section from ClientDefinitions.ini, or null if absent.
        /// </summary>
        public IniSection GetAutoMapOverlaysSection() => clientDefinitionsIni.GetSection("AutoMapOverlays");

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
        public ClientType ClientGameType => _ClientGameType ??= ClientType.FromString(_ClientGameTypeString);

        public string DiscordAppId => clientDefinitionsIni.GetStringValue(SETTINGS, "DiscordAppId", string.Empty);

        public int SendSleep => clientDefinitionsIni.GetIntValue(SETTINGS, "SendSleep", 2500);

        public int LoadingScreenCount => clientDefinitionsIni.GetIntValue(SETTINGS, "LoadingScreenCount", 2);

        public int ThemeCount => clientDefinitionsIni.GetSectionKeys("Themes").Count;

        public string LocalGame => clientDefinitionsIni.GetStringValue(SETTINGS, "LocalGame", "DTA");

        public bool SidebarHack => clientDefinitionsIni.GetBooleanValue(SETTINGS, "SidebarHack", false);

        public int MinimumRenderWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumRenderWidth", 1280);
        public int MinimumRenderHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumRenderHeight", 768);
        public ScreenResolution MinimumRenderResolution => (MinimumRenderWidth, MinimumRenderHeight);

        public int MaximumRenderWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumRenderWidth", 1280);
        public int MaximumRenderHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumRenderHeight", 800);
        public ScreenResolution MaximumRenderResolution => (MaximumRenderWidth, MaximumRenderHeight);

        public int MinimumClientWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumClientWidth", 800);
        public int MinimumClientHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumClientHeight", 600);
        public ScreenResolution MinimumClientResolution => (MinimumClientWidth, MinimumClientHeight);

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

        private string[] GetCoopDifficultyINIPaths()
        {
            List<string> paths = new List<string>();
            var pathString = clientDefinitionsIni.GetStringValue(SETTINGS, nameof(CoopDifficultyINIPaths), string.Empty);

            if (string.IsNullOrEmpty(pathString))
            {
                paths.Add(SafePath.CombineFilePath("INI", "Map Code", "Coop Difficulty Easy.ini"));
                paths.Add(SafePath.CombineFilePath("INI", "Map Code", "Coop Difficulty Medium.ini"));
                paths.Add(SafePath.CombineFilePath("INI", "Map Code", "Coop Difficulty Hard.ini"));
            }
            else
            {
                var pathStrings = pathString.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var path in pathStrings)
                {
                    paths.Add(SafePath.CombineFilePath(path));
                }
            }
            
            return paths.ToArray();
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

        public string[] CoopDifficultyINIPaths => GetCoopDifficultyINIPaths();

        public string KeyboardINI => clientDefinitionsIni.GetStringValue(SETTINGS, "KeyboardINI", "Keyboard.ini");

        public bool SettingsIniAsKeyboardIni => SettingsIniName == KeyboardINI;

        public string KeyboardHotkeySection => clientDefinitionsIni.GetStringValue(
            SETTINGS,
            "KeyboardHotkeySection",
            ClientGameType == ClientType.RA ? "WinHotKeys" : "Hotkey");

        public int MinimumIngameWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumIngameWidth", 640);
        public int MinimumIngameHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumIngameHeight", 480);
        public ScreenResolution MinimumIngameResolution => (MinimumIngameWidth, MinimumIngameHeight);

        public int MaximumIngameWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumIngameWidth", int.MaxValue);
        public int MaximumIngameHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumIngameHeight", int.MaxValue);
        public ScreenResolution MaximumIngameResolution => (MaximumIngameWidth, MaximumIngameHeight);

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

        /// <summary>Enables replay recording and playback for compatible game packages.</summary>
        public bool ReplaySupport => clientDefinitionsIni.GetBooleanValue(SETTINGS, "ReplaySupport", false);

        /// <summary>Replay directory relative to the game directory.</summary>
        public string ReplaysDirectory => clientDefinitionsIni.GetStringValue(SETTINGS, "ReplaysDirectory", "Replays");

        /// <summary>Replay file extension without a leading dot.</summary>
        public string ReplayFileExtension => clientDefinitionsIni.GetStringValue(SETTINGS, "ReplayFileExtension", "yrrp");

        /// <summary>Page listing downloadable versions of the game package.</summary>
        public string ReleasesURL => clientDefinitionsIni.GetStringValue(SETTINGS, "ReleasesURL", string.Empty);

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
        /// Ping penalty (ms) applied per percentage point of packet loss when comparing paths.
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
        /// How many unanswered probes in a row a tunnel's last good ping is kept for before it
        /// reads as unknown. 0 discards the measurement on the first miss.
        /// </summary>
        /// <remarks>
        /// One probe is sent per tunnel list refresh, so on a lossy connection a single lost
        /// datagram would otherwise leave a healthy tunnel unmeasured for a whole refresh interval.
        /// Kept small: a tunnel that has actually gone away should stop advertising a stale
        /// latency quickly.
        /// </remarks>
        public int V3RetainedPingFailures => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "RetainedPingFailures", 2);

        /// <summary>
        /// Most V3 UDP pings (sender 0, receiver 0) sent to one tunnel per minute. Tunnel servers
        /// answer at most 20 per IP per minute, shared by everyone behind the same public address,
        /// so a client must stay well under that.
        /// </summary>
        public int V3MaxPingsPerTunnelPerMinute => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "MaxPingsPerTunnelPerMinute", 6);

        /// <summary>
        /// How long (ms) to wait for a V3 UDP ping reply before counting it as lost.
        /// </summary>
        public int V3UdpPingTimeoutMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "UdpPingTimeoutMs", 1500);

        /// <summary>
        /// Interval (ms) between heartbeats on each active path while in the lobby.
        /// </summary>
        public int V3HeartbeatIntervalLobbyMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "HeartbeatIntervalLobbyMs", 500);

        /// <summary>
        /// Interval (ms) between heartbeats on each active path while a game is running.
        /// </summary>
        public int V3HeartbeatIntervalGameMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "HeartbeatIntervalGameMs", 250);

        /// <summary>
        /// Silence (ms) on a pair's path in the lobby before fallback tunnels are brought up
        /// alongside it.
        /// </summary>
        public int V3SuspectAfterLobbyMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "SuspectAfterLobbyMs", 2000);

        /// <summary>
        /// Silence (ms) on a pair's path in the lobby before it is abandoned for a fallback.
        /// </summary>
        public int V3CommitAfterLobbyMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "CommitAfterLobbyMs", 5000);

        /// <summary>
        /// Silence (ms) on a pair's path in game before fallback tunnels are brought up alongside
        /// it. Game traffic is copied onto the first fallback that answers, so this is roughly how
        /// long a dead tunnel stalls the game.
        /// </summary>
        public int V3SuspectAfterGameMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "SuspectAfterGameMs", 1000);

        /// <summary>
        /// Silence (ms) on a pair's path in game before it is abandoned for a fallback. Longer
        /// than <see cref="V3SuspectAfterGameMs"/> so a short burst of loss does not move the pair.
        /// </summary>
        public int V3CommitAfterGameMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "CommitAfterGameMs", 3000);

        /// <summary>
        /// How many tunnels of the ranked list are brought up at once when a pair first connects.
        /// </summary>
        public int V3BringUpBatchSize => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "BringUpBatchSize", 4);

        /// <summary>
        /// How long (ms) a bring-up batch has for any of its tunnels to answer before the next
        /// batch is tried.
        /// </summary>
        public int V3BringUpBatchTimeoutMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "BringUpBatchTimeoutMs", 6000);

        /// <summary>
        /// How long (ms) the decider keeps measuring a bring-up batch after its first tunnel
        /// answers, so the choice is made on a few round trips rather than on whichever answered
        /// first.
        /// </summary>
        public int V3BringUpSettleMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "BringUpSettleMs", 2000);

        /// <summary>
        /// How many fallback tunnels are brought up at once when a pair's path goes quiet.
        /// </summary>
        public int V3FailoverBatchSize => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "FailoverBatchSize", 2);

        /// <summary>
        /// How long (ms) a failover batch has to answer before the next one is tried. Answering
        /// takes about one and a half round trips through the fallback.
        /// </summary>
        public int V3FailoverBatchTimeoutMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "FailoverBatchTimeoutMs", 2000);

        /// <summary>
        /// How long (ms) the decider waits after the first fallback answers before promoting the
        /// best one, so a slightly slower but better-ranked candidate is not passed over.
        /// </summary>
        public int V3FailoverSettleMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "FailoverSettleMs", 300);

        /// <summary>
        /// How many tunnels the agreed ranked list for a pair holds. The head is brought up first;
        /// the rest are the pair's fallbacks.
        /// </summary>
        public int V3RankedListSize => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "RankedListSize", 16);

        /// <summary>
        /// Delays (seconds, comma-separated) between automatic retries of a pair that could not
        /// connect. The last value repeats for as long as both players stay in the lobby.
        /// </summary>
        public string V3RetryDelaysSeconds => networkDefinitionsIni.GetStringValue(V3_TUNNEL_NEGOTIATION, "RetryDelaysSeconds", "5,15,60");

        /// <summary>
        /// How many automatic retries fail before the lobby suggests a manual renegotiation.
        /// </summary>
        public int V3RetriesBeforeHint => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "RetriesBeforeHint", 3);

        /// <summary>
        /// Smallest improvement (ms) a fallback tunnel must offer before a working pair is moved to it.
        /// </summary>
        public int V3DegradeMarginMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "DegradeMarginMs", 25);

        /// <summary>
        /// Smallest improvement, as a fraction of the current score, a fallback tunnel must offer
        /// before a working pair is moved to it. The larger of this and
        /// <see cref="V3DegradeMarginMs"/> applies.
        /// </summary>
        public double V3DegradeMarginFraction => networkDefinitionsIni.GetDoubleValue(V3_TUNNEL_NEGOTIATION, "DegradeMarginFraction", 0.2);

        /// <summary>
        /// How long (seconds) a fallback must keep predicting better than the current path before
        /// it is trialled.
        /// </summary>
        public int V3DegradeSustainSeconds => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "DegradeSustainSeconds", 10);

        /// <summary>
        /// Packet loss (percent) on the current path, measured over the last 10 seconds, at which
        /// a fallback is trialled even if it does not predict a lower latency.
        /// </summary>
        public double V3DegradeLossPercent => networkDefinitionsIni.GetDoubleValue(V3_TUNNEL_NEGOTIATION, "DegradeLossPercent", 2.0);

        /// <summary>
        /// How long (ms) a trialled tunnel is measured, carrying a copy of the pair's traffic,
        /// before the decider decides whether to move to it.
        /// </summary>
        public int V3TrialDurationMs => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "TrialDurationMs", 5000);

        /// <summary>
        /// Minimum time (seconds) between path changes of one pair, other than for failures.
        /// </summary>
        public int V3SwitchCooldownSeconds => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "SwitchCooldownSeconds", 30);

        /// <summary>
        /// How many degradation switches within five minutes stop further degradation switching
        /// for a pair for <see cref="V3DegradeFreezeSeconds"/>. Failure switching is unaffected.
        /// </summary>
        public int V3DegradeFreezeAfterSwitches => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "DegradeFreezeAfterSwitches", 3);

        /// <summary>
        /// How long (seconds) degradation switching stays off for a pair that has been flapping.
        /// </summary>
        public int V3DegradeFreezeSeconds => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "DegradeFreezeSeconds", 600);

        /// <summary>
        /// How often (seconds) the two sides of a pair swap fresh tunnel ping tables over their
        /// working path, so the fallback list stays current.
        /// </summary>
        public int V3PingTableIntervalSeconds => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "PingTableIntervalSeconds", 30);

        /// <summary>
        /// How often (seconds) a pair in the lobby sends a short burst at game packet rate to
        /// measure loss under load. 0 disables it.
        /// </summary>
        public int V3LoadProbeIntervalSeconds => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "LoadProbeIntervalSeconds", 120);

        /// <summary>
        /// How long (seconds) a pair whose peer has gone silent everywhere keeps retrying before
        /// it is reported as failed.
        /// </summary>
        public int V3PeerLostSeconds => networkDefinitionsIni.GetIntValue(V3_TUNNEL_NEGOTIATION, "PeerLostSeconds", 15);

        #endregion

        #region V3 matchmaking tunnels

        /// <summary>
        /// Whether peers swap tunnel tables over the matchmaking servers. When disabled, or when
        /// none answers, the swap goes over IRC instead, which works but is slower.
        /// </summary>
        public bool V3MatchmakingEnabled => networkDefinitionsIni.GetBooleanValue(V3_MATCHMAKING, "Enabled", true);

        /// <summary>
        /// Fraction (0-1) of a tunnel's advertised capacity at which it stops being ranked, so
        /// negotiation never steers players onto a server that is about to reject them.
        /// </summary>
        public double V3MatchmakingCapacityThreshold => networkDefinitionsIni.GetDoubleValue(V3_MATCHMAKING, "CapacityThreshold", 0.85);

        /// <summary>
        /// How long (ms) to wait for the peer's table, and for the decider's ranked list, before
        /// the attempt counts as failed and is retried later. Sized to absorb the gap between the
        /// two peers starting, not the round trip itself.
        /// </summary>
        public int V3MatchmakingExchangeTimeoutMs => networkDefinitionsIni.GetIntValue(V3_MATCHMAKING, "ExchangeTimeoutMs", 15000);

        /// <summary>
        /// Delay (ms) between repeats of the table while waiting for the ranked list.
        /// </summary>
        public int V3MatchmakingRetryIntervalMs => networkDefinitionsIni.GetIntValue(V3_MATCHMAKING, "RetryIntervalMs", 400);

        /// <summary>
        /// How long (ms) the swap waits on the matchmaking servers before also sending the table
        /// over IRC.
        /// </summary>
        public int V3MatchmakingIrcFallbackDelayMs => networkDefinitionsIni.GetIntValue(V3_MATCHMAKING, "IrcFallbackDelayMs", 3000);

        /// <summary>
        /// How many bring-ups in a row may try a tunnel without it carrying anything from the peer
        /// before it is ranked below tunnels that have not failed. 0 disables the check.
        /// </summary>
        /// <remarks>
        /// A tunnel can answer pings while refusing registrations, for example when it is full or
        /// in maintenance. Demoted rather than excluded, and one success clears the count.
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
