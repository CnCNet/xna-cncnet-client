using ClientCore;
using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System.Collections.Generic;
using System.IO;

namespace DTAConfig.CustomSettings
{
    /// <summary>
    /// A dropdown that switches between multiple sets of files.
    /// </summary>
    public class CustomSettingFileDropDown : XNAClientDropDown, ICustomSetting
    {
        public CustomSettingFileDropDown(WindowManager windowManager) : base(windowManager) { }

        private List<List<FileSourceDestinationInfo>> itemFilesList = new List<List<FileSourceDestinationInfo>>();

        private int defaultValue;
        private int originalState;

        public bool RestartRequired { get; private set; }
        public bool CheckAvailability { get; private set; }
        public bool ResetUnavailableValue { get; private set; }

        public override void GetAttributes(IniFile iniFile)
        {
            base.GetAttributes(iniFile);

            var section = iniFile.GetSection(Name);
            if (section == null)
                return;

            for (int i = 0; i < Items.Count; i++)
                itemFilesList.Add(FileSourceDestinationInfo.ParseFSDInfoList(section, $"Item{i}File"));
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "Items":
                    string[] items = value.Split(',');
                    for (int i = 0; i < items.Length; i++)
                    {
                        XNADropDownItem item = new XNADropDownItem();
                        item.Text = items[i];
                        AddItem(item);
                    }
                    return;
                case "DefaultValue":
                    defaultValue = Conversions.IntFromString(value, 0);
                    return;
                case "CheckAvailability":
                    CheckAvailability = Conversions.BooleanFromString(value, false);
                    return;
                case "ResetUnavailableValue":
                    ResetUnavailableValue = Conversions.BooleanFromString(value, false);
                    return;
                case "RestartRequired":
                    RestartRequired = Conversions.BooleanFromString(value, false);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public void Load()
        {
            SelectedIndex = UserINISettings.Instance.GetCustomSettingValue(Name, defaultValue);
            originalState = SelectedIndex;
        }

        public bool RefreshSetting()
        {
            int currentValue = SelectedIndex;

            if (CheckAvailability)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    Items[i].Selectable = true;
                    foreach (var fileInfo in itemFilesList[i])
                    {
                        if (!File.Exists(fileInfo.SourcePath))
                        {
                            Items[i].Selectable = false;
                            break;
                        }
                    }
                }

                if (ResetUnavailableValue && !Items[SelectedIndex].Selectable)
                    SelectedIndex = defaultValue;
            }

            return SelectedIndex != currentValue;
        }

        public bool Save()
        {
            UserINISettings.Instance.SetCustomSettingValue(Name, SelectedIndex);

            if (Items[SelectedIndex].Selectable)
            {
                for (int i = 0; i < itemFilesList.Count; i++)
                {
                    if (i != SelectedIndex)
                        itemFilesList[i].ForEach(f => f.Revert());
                }

                itemFilesList[SelectedIndex].ForEach(f => f.Apply());
            }
            else // selected item is unavailable, don't do anything
            {
                Logger.Log($"{nameof(CustomSettingFileDropDown)}: " +
                    $"The selected item ({Items[SelectedIndex].Text}) is unavailable in {Name}");
                return false;
            }

            return RestartRequired && (SelectedIndex != originalState);
        }
    }
}
