using System;
using ClientCore.I18N;
using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.Settings
{
    public abstract class SettingCheckBoxBase : XNAClientCheckBox, IUserSetting
    {
        public SettingCheckBoxBase(WindowManager windowManager) : base(windowManager) { }

        public SettingCheckBoxBase(WindowManager windowManager, bool defaultValue, string settingSection, string settingKey, bool restartRequired = false) : base(windowManager)
        {
            DefaultValue = defaultValue;
            SettingSection = settingSection;
            SettingKey = settingKey;
            RestartRequired = restartRequired;
        }

        public bool DefaultValue { get; set; }

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

        private string _parentCheckBoxName;
        /// <summary>
        /// Name of parent check-box control.
        /// </summary>
        public string ParentCheckBoxName
        {
            get { return _parentCheckBoxName; }
            set
            {
                _parentCheckBoxName = value;
                UpdateParentCheckBox(FindParentCheckBox());
            }
        }

        private XNAClientCheckBox _parentCheckBox;
        /// <summary>
        /// Parent check-box control.
        /// </summary>
        public XNAClientCheckBox ParentCheckBox
        {
            get { return _parentCheckBox; }
            set
            {
                UpdateParentCheckBox(value);
                _parentCheckBoxName = _parentCheckBox != null ? _parentCheckBox.Name : null;
            }
        }

        /// <summary>
        /// Value required from parent check-box control if set.
        /// </summary>
        public bool ParentCheckBoxRequiredValue { get; set; } = true;

        protected string defaultSection = "CustomSettings";
        protected string defaultKeySuffix = "_Checked";
        protected bool originalState;

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            static string Localize(XNAControl control, string attributeName, string defaultValue, bool notify = true)
                => Translation.Instance.LookUp(control, attributeName, defaultValue, notify);

            switch (key)
            {
                case "Checked":
                case "DefaultValue":
                    DefaultValue = Conversions.BooleanFromString(value, false);
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
                case "ParentCheckBoxName":
                    ParentCheckBoxName = value;
                    return;
                case "ParentCheckBoxRequiredValue":
                    ParentCheckBoxRequiredValue = Conversions.BooleanFromString(value, true);
                    return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);

            switch (key)
            {
                case nameof(Text):
                    Text = Localize(this, nameof(Text), Text);
                    break;
                case nameof(ToolTip):
                    ToolTipText = Localize(this, nameof(ToolTip), ToolTipText);
                    break;
            }
        }

        public abstract void Load();

        public abstract bool Save();


        private XNAClientCheckBox FindParentCheckBox()
        {
            if (string.IsNullOrEmpty(ParentCheckBoxName))
                return null;

            foreach (var control in Parent.Children)
            {
                if (control is XNAClientCheckBox && control.Name == ParentCheckBoxName)
                    return control as XNAClientCheckBox;
            }

            return null;
        }

        private void UpdateParentCheckBox(XNAClientCheckBox parentCheckBox)
        {
            if (ParentCheckBox != null)
                ParentCheckBox.CheckedChanged -= ParentCheckBox_CheckedChanged;

            _parentCheckBox = parentCheckBox;
            UpdateAllowChecking();

            if (ParentCheckBox != null)
                ParentCheckBox.CheckedChanged += ParentCheckBox_CheckedChanged;
        }

        private void ParentCheckBox_CheckedChanged(object sender, EventArgs e) => UpdateAllowChecking();

        private void UpdateAllowChecking()
        {
            if (ParentCheckBox != null)
            {
                if (ParentCheckBox.Checked == ParentCheckBoxRequiredValue)
                {
                    AllowChecking = true;
                }
                else
                {
                    AllowChecking = false;
                    Checked = false;
                }
            }
        }

    }
}