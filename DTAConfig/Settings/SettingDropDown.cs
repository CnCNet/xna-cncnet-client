﻿using ClientCore.Settings;

using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAConfig.Settings;

/// <summary>
/// Dropdown for toggling options in user settings INI file.
/// </summary>
public class SettingDropDown : SettingDropDownBase
{
    public SettingDropDown(WindowManager windowManager) : base(windowManager) { }

    public SettingDropDown(WindowManager windowManager, int defaultValue, string settingSection, string settingKey, bool writeItemValue = false, bool restartRequired = false)
        : base(windowManager, defaultValue, settingSection, settingKey, restartRequired)
    {
        WriteItemValue = writeItemValue;
    }

    private bool _writeItemValue;
    /// <summary>
    /// If set, dropdown item's value instead of index is written to the user settings INI.
    /// </summary>
    public bool WriteItemValue
    {
        get => _writeItemValue;
        set
        {
            _writeItemValue = value;
            defaultKeySuffix = _writeItemValue ? "_Value" : "_SelectedIndex";
        }
    }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "WriteItemValue":
                WriteItemValue = Conversions.BooleanFromString(value, false);
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public override void Load()
    {

        /* 项目“DTAConfig (net8.0-windows)”的未合并的更改
        在此之前:
                    if (WriteItemValue)
        在此之后:
                SelectedIndex = WriteItemValue)
        */
        SelectedIndex = WriteItemValue
            ? FindItemIndexByValue(UserINISettings.Instance.GetValue(SettingSection, SettingKey, null))
            : UserINISettings.Instance.GetValue(SettingSection, SettingKey, DefaultValue);

        originalState = SelectedIndex;
    }

    public override bool Save()
    {
        if (WriteItemValue)
        {
            UserINISettings.Instance.SetValue(SettingSection, SettingKey, (string)SelectedItem.Tag);
        }
        else
        {
            UserINISettings.Instance.SetValue(SettingSection, SettingKey, SelectedIndex);
        }

        return RestartRequired && (SelectedIndex != originalState);
    }

    private int FindItemIndexByValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return DefaultValue;
        }

        int index = Items.FindIndex(x => (string)x.Tag == value);

        return index < 0 ? DefaultValue : index;
    }
}