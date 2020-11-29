using ClientCore;
using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
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
        private bool restartRequired;
        private bool checkFilePresence;
        private bool resetUnselectableItem;

        public override void GetAttributes(IniFile iniFile)
        {
            base.GetAttributes(iniFile);

            var section = iniFile.GetSection(Name);
            if (section == null)
                return;

            for (int i = 0; i < Items.Count; i++)
            {
                itemFilesList.Add(new List<FileSourceDestinationInfo>());
                int j = 0;
                while (true)
                {
                    string fileInfo = section.GetStringValue($"Item{i}File{j}", string.Empty);

                    if (string.IsNullOrWhiteSpace(fileInfo))
                        break;

                    string[] parts = fileInfo.Split(',');
                    if (parts.Length < 2)
                    {
                        Logger.Log($"Invalid CustomSettingFileDropDown information in {Name}: {fileInfo}");
                        continue;
                    }
                    
                    FileOperationOptions options = default;
                    if (parts.Length >= 3)
                        Enum.TryParse(parts[2], out options);

                    itemFilesList[i].Add(new FileSourceDestinationInfo(parts[0], parts[1], options));

                    j++;
                }
            }
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
                case "CheckFilePresence":
                    checkFilePresence = Conversions.BooleanFromString(value, false);
                    return;
                case "ResetUnselectableItem":
                    resetUnselectableItem = Conversions.BooleanFromString(value, false);
                    return;
                case "RestartRequired":
                    restartRequired = Conversions.BooleanFromString(value, false);
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

            if (checkFilePresence)
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

                if (resetUnselectableItem && !Items[SelectedIndex].Selectable)
                    SelectedIndex = defaultValue;
            }

            return SelectedIndex != currentValue;
        }

        public bool Save()
        {
            foreach (var list in itemFilesList)
                list.ForEach(f => f.Revert());

            if (Items[SelectedIndex].Selectable)
                itemFilesList[SelectedIndex].ForEach(f => f.Apply());
            
            UserINISettings.Instance.SetCustomSettingValue(Name, SelectedIndex);

            return restartRequired && (SelectedIndex != originalState);
        }
    }
}
