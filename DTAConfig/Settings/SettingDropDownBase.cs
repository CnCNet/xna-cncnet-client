using ClientCore.I18N;
using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.Settings
{
    public abstract class SettingDropDownBase : XNAClientDropDown, IUserSetting
    {
        public SettingDropDownBase(WindowManager windowManager) : base(windowManager) { }

        public SettingDropDownBase(WindowManager windowManager, int defaultValue, string settingSection, string settingKey, bool restartRequired = false)
            : base(windowManager)
        {
            DefaultValue = defaultValue;
            SettingSection = settingSection;
            SettingKey = settingKey;
            RestartRequired = restartRequired;
        }

        public int DefaultValue { get; set; }

        private string _settingSection;
        public string SettingSection
        {
            get => string.IsNullOrEmpty(_settingSection) ? defaultSection : _settingSection;
            set => _settingSection = value;
        }

        private string _settingKey;
        public string SettingKey
        {
            get => string.IsNullOrEmpty(_settingKey) ? $"{Name}{defaultKeySuffix}" : _settingKey;
            set => _settingKey = value;
        }

        public bool RestartRequired { get; set; }

        protected string defaultSection = "CustomSettings";
        protected string defaultKeySuffix = "_SelectedIndex";
        protected int originalState;

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            // shorthand for localization function
            static string Localize(XNAControl control, string attributeName, string defaultValue, bool notify = true)
                => Translation.Instance.LookUp(control, attributeName, defaultValue, notify);

            switch (key)
            {
                case "Items":
                    string[] items = value.Split(',');
                    for (int i = 0; i < items.Length; i++)
                    {
                        XNADropDownItem item = new XNADropDownItem
                        {
                            Text = Localize(this, $"Item{i}", items[i]),
                            Tag = items[i]
                        };
                        AddItem(item);
                    }
                    return;
                case "DefaultValue":
                    DefaultValue = Conversions.IntFromString(value, 0);
                    return;
                case "SettingSection":
                    SettingSection = string.IsNullOrEmpty(value) ? SettingSection : value;
                    return;
                case "SettingKey":
                    SettingKey = string.IsNullOrEmpty(value) ? SettingKey : value;
                    return;
                case "RestartRequired":
                    RestartRequired = Conversions.BooleanFromString(value, false);
                    return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);

            switch (key)
            {
                case nameof(ToolTip):
                    ToolTipText = Localize(this, nameof(ToolTip), ToolTipText);
                    break;
            }
        }

        public abstract void Load();

        public abstract bool Save();
    }
}