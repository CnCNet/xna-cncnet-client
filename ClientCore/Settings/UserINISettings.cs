using ClientCore.Settings;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ClientCore
{
    public class UserINISettings
    {
        private static UserINISettings _instance;

        public static UserINISettings Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("UserINISettings not initialized!");

                return _instance;
            }
        }

        public static void Initialize(string iniFileName)
        {
            if (_instance != null)
                throw new InvalidOperationException("UserINISettings has already been initialized!");

            var iniFile = new IniFile(ProgramConstants.GamePath + iniFileName);

            _instance = new UserINISettings(iniFile);
        }

        protected UserINISettings(IniFile iniFile)
        {
            settingsIni = iniFile;

            const string VIDEO = "Video";
            const string MULTIPLAYER = "MultiPlayer";
            const string OPTIONS = "Options";
            const string AUDIO = "Audio";
#if YR
            const string WINDOWED_MODE_KEY = "Windowed";
            BackBufferInVRAM = new BoolSetting(iniFile, VIDEO, "VideoBackBuffer", false);
#else
            const string WINDOWED_MODE_KEY = "Video.Windowed";
            BackBufferInVRAM = new BoolSetting(iniFile, VIDEO, "UseGraphicsPatch", true);
#endif

            IngameScreenWidth = new IntSetting(iniFile, VIDEO, "ScreenWidth", 1024);
            IngameScreenHeight = new IntSetting(iniFile, VIDEO, "ScreenHeight", 600);
            ClientTheme = new IntSetting(iniFile, MULTIPLAYER, "Theme", 0);
            DetailLevel = new IntSetting(iniFile, OPTIONS, "DetailLevel", 2);
            Renderer = new StringSetting(iniFile, "Compatibility", "Renderer", string.Empty);
            WindowedMode = new BoolSetting(iniFile, VIDEO, WINDOWED_MODE_KEY, false);
            BorderlessWindowedMode = new BoolSetting(iniFile, VIDEO, "NoWindowFrame", false);

            ClientResolutionX = new IntSetting(iniFile, VIDEO, "ClientResolutionX", Screen.PrimaryScreen.Bounds.Width);
            ClientResolutionY = new IntSetting(iniFile, VIDEO, "ClientResolutionY", Screen.PrimaryScreen.Bounds.Height);
            BorderlessWindowedClient = new BoolSetting(iniFile, VIDEO, "BorderlessWindowedClient", true);

            ScoreVolume = new DoubleSetting(iniFile, AUDIO, "ScoreVolume", 0.7);
            SoundVolume = new DoubleSetting(iniFile, AUDIO, "SoundVolume", 0.7);
            VoiceVolume = new DoubleSetting(iniFile, AUDIO, "VoiceVolume", 0.7);
            IsScoreShuffle = new BoolSetting(iniFile, AUDIO, "IsScoreShuffle", true);
            ClientVolume = new DoubleSetting(iniFile, AUDIO, "ClientVolume", 1.0);
            PlayMainMenuMusic = new BoolSetting(iniFile, AUDIO, "PlayMainMenuMusic", true);

            ScrollRate = new IntSetting(iniFile, OPTIONS, "ScrollRate", 3);
            TargetLines = new BoolSetting(iniFile, OPTIONS, "UnitActionLines", true);
            ScrollCoasting = new IntSetting(iniFile, OPTIONS, "ScrollMethod", 0);
            Tooltips = new BoolSetting(iniFile, OPTIONS, "ToolTips", true);

            PlayerName = new StringSetting(iniFile, MULTIPLAYER, "Handle", string.Empty);

            ChatColor = new IntSetting(iniFile, MULTIPLAYER, "ChatColor", -1);
            PingUnofficialCnCNetTunnels = new BoolSetting(iniFile, MULTIPLAYER, "PingCustomTunnels", true);
            WritePathToRegistry = new BoolSetting(iniFile, OPTIONS, "WriteInstallationPathToRegistry", true);
            PlaySoundOnGameHosted = new BoolSetting(iniFile, MULTIPLAYER, "PlaySoundOnGameHosted", true);

            SkipConnectDialog = new BoolSetting(iniFile, MULTIPLAYER, "SkipConnectDialog", false);
            PersistentMode = new BoolSetting(iniFile, MULTIPLAYER, "PersistentMode", false);
            AutomaticCnCNetLogin = new BoolSetting(iniFile, MULTIPLAYER, "AutomaticCnCNetLogin", false);

            CheckForUpdates = new BoolSetting(iniFile, OPTIONS, "CheckforUpdates", true);
        }

        IniFile settingsIni;

        public event EventHandler SettingsSaved;

        public IntSetting IngameScreenWidth { get; private set; }
        public IntSetting IngameScreenHeight { get; private set; }
        public IntSetting ClientTheme { get; private set; }
        public IntSetting DetailLevel { get; private set; }
        public StringSetting Renderer { get; private set; }
        public BoolSetting WindowedMode { get; private set; }
        public BoolSetting BorderlessWindowedMode { get; private set; }
        public BoolSetting BackBufferInVRAM { get; private set; }
        public IntSetting ClientResolutionX { get; private set; }
        public IntSetting ClientResolutionY { get; private set; }
        public BoolSetting BorderlessWindowedClient { get; private set; }

        public DoubleSetting ScoreVolume { get; private set; }
        public DoubleSetting SoundVolume { get; private set; }
        public DoubleSetting VoiceVolume { get; private set; }
        public BoolSetting IsScoreShuffle { get; private set; }
        public DoubleSetting ClientVolume { get; private set; }
        public BoolSetting PlayMainMenuMusic { get; private set; }

        public IntSetting ScrollRate { get; private set; }
        public BoolSetting TargetLines { get; private set; }
        public IntSetting ScrollCoasting { get; private set; }
        public BoolSetting Tooltips { get; private set; }

        public StringSetting PlayerName { get; private set; }

        public IntSetting ChatColor { get; private set; }
        public BoolSetting PingUnofficialCnCNetTunnels { get; private set; }
        public BoolSetting WritePathToRegistry { get; private set; }
        public BoolSetting PlaySoundOnGameHosted { get; private set; }

        public BoolSetting SkipConnectDialog { get; private set; }
        public BoolSetting PersistentMode { get; private set; }
        public BoolSetting AutomaticCnCNetLogin { get; private set; }

        public BoolSetting CheckForUpdates { get; private set; }

        public void SaveSettings()
        {
            settingsIni.WriteIniFile();

            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
    }
}
