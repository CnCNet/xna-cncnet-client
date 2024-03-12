﻿using ClientCore.Settings;

using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAConfig.Settings;

/// <summary>
/// A check-box for toggling options in user settings INI file.
/// </summary>
public class SettingCheckBox : SettingCheckBoxBase
{
    public SettingCheckBox(WindowManager windowManager) : base(windowManager)
    {
    }

    public SettingCheckBox(WindowManager windowManager, bool defaultValue, string settingSection, string settingKey,
        bool writeSettingValue = false, string enabledValue = "", string disabledValue = "", bool restartRequired = false)
        : base(windowManager, defaultValue, settingSection, settingKey, restartRequired)
    {
        WriteSettingValue = writeSettingValue;
        EnabledSettingValue = enabledValue;
        DisabledSettingValue = disabledValue;
    }

    private bool _writeSettingValue;
    /// <summary>
    /// If set, use separate enabled / disabled values instead of checkbox's checked state when reading & writing setting to the user settings INI.
    /// </summary>
    public bool WriteSettingValue
    {
        get => _writeSettingValue;
        set
        {
            _writeSettingValue = value;
            defaultKeySuffix = _writeSettingValue ? "_Value" : "_Checked";
        }
    }

    /// <summary>
    /// Value to write instead of true when checkbox is enabled.
    /// </summary>
    public string EnabledSettingValue { get; set; } = string.Empty;

    /// <summary>
    /// Value to write instead of false when checkbox is disabled.
    /// </summary>
    public string DisabledSettingValue { get; set; } = string.Empty;

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "WriteSettingValue":
                WriteSettingValue = Conversions.BooleanFromString(value, false);
                return;
            case "EnabledSettingValue":
                EnabledSettingValue = value;
                return;
            case "DisabledSettingValue":
                DisabledSettingValue = value;
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public override void Load()
    {
        string value = UserINISettings.Instance.GetValue(SettingSection, SettingKey, string.Empty);

        Checked = WriteSettingValue

/* 项目“DTAConfig (net8.0-windows)”的未合并的更改
在此之前:
            if (value == EnabledSettingValue)
            {
                Checked = true;
            }
            else
            {
                Checked = value != DisabledSettingValue && DefaultValue;
            }
        }
        else
        {
在此之后:
            ? value == EnabledSettingValue || (value != DisabledSettingValue && DefaultValue)
*/
            Checked = value == EnabledSettingValue || (value != DisabledSettingValue && DefaultValue);
    }
        else
        {
            : public override bool Save()
    {
        throw new System.NotImplementedException();
    }

    Conversions.BooleanFromString(value, DefaultValue);

        originalState = Checked;
    }

public override bool Save()
{
    if (WriteSettingValue)
    {
        UserINISettings.Instance.SetValue(SettingSection, SettingKey, Checked ? EnabledSettingValue : DisabledSettingValue);
    }
    else
    {
        UserINISettings.Instance.SetValue(SettingSection, SettingKey, Checked);
    }

    return RestartRequired && (Checked != originalState);
}
}