using System;
using System.Collections.Generic;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using ClientCore.Extensions;
using ClientCore.I18N;
using System.IO;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public class GameLobbyDropDown : XNAClientDropDown
    {
        public GameLobbyDropDown(WindowManager windowManager) : base(windowManager) { }

        public string OptionName { get; private set; }

        public int HostSelectedIndex { get; set; }

        public int UserSelectedIndex { get; set; }

        private DropDownDataWriteMode dataWriteMode = DropDownDataWriteMode.BOOLEAN;

        private string spawnIniOption = string.Empty;

        private int defaultIndex;

        private List<string> spawnIniValues;

        public override void Initialize()
        {
            XNAControl parent = Parent;
            while (parent != null)
            {
                if (parent is GameLobbyBase gameLobby)
                {
                    gameLobby.DropDowns.Add(this);
                    break;
                }
                parent = parent.Parent;
            }

            base.Initialize();
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            static string Localize(XNAControl control, string attributeName, string defaultValue, bool notify = true)
                => Translation.Instance.LookUp(control, attributeName, defaultValue, notify);

            switch (key)
            {
                case "Items":
                    string[] items = value.Split(',');
                    string[] itemLabels = iniFile.GetStringListValue(Name, "ItemLabels", "");
                    for (int i = 0; i < items.Length; i++)
                    {
                        bool hasLabel = itemLabels.Length > i && !string.IsNullOrEmpty(itemLabels[i]);
                        XNADropDownItem item = new()
                        {
                            Text = Localize(this, $"Item{i}", hasLabel ? itemLabels[i] : items[i]),
                            Tag = items[i],
                        };
                        AddItem(item);
                    }
                    return;

                case "SpawnIniValues":
                    spawnIniValues = new List<string>(value.Split(','));
                    return;

                case "DataWriteMode":
                    switch (value.ToUpper())
                    {
                        case "INDEX":
                            dataWriteMode = DropDownDataWriteMode.INDEX;
                            break;
                        case "BOOLEAN":
                            dataWriteMode = DropDownDataWriteMode.BOOLEAN;
                            break;
                        case "MAPCODE":
                            dataWriteMode = DropDownDataWriteMode.MAPCODE;
                            break;
                        case "SPAWN_SPAWNMAP":
                            dataWriteMode = DropDownDataWriteMode.SPAWN_SPAWNMAP;
                            break;
                        default:
                            dataWriteMode = DropDownDataWriteMode.STRING;
                            break;
                    }
                    return;

                case "SpawnIniOption":
                    spawnIniOption = value;
                    return;

                case "DefaultIndex":
                    SelectedIndex = int.Parse(value);
                    defaultIndex = SelectedIndex;
                    HostSelectedIndex = SelectedIndex;
                    UserSelectedIndex = SelectedIndex;
                    return;

                case "OptionName":
                    OptionName = Localize(this, "OptionName", value);
                    return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public void ApplySpawnIniCode(IniFile spawnIni)
        {
            if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return;

            switch (dataWriteMode)
            {
                case DropDownDataWriteMode.BOOLEAN:
                    if (!string.IsNullOrEmpty(spawnIniOption))
                    {
                        bool boolValue = SelectedIndex > 0;
                        spawnIni.SetBooleanValue("Settings", spawnIniOption, boolValue);
                    }
                    return;

                case DropDownDataWriteMode.INDEX:
                    if (!string.IsNullOrEmpty(spawnIniOption))
                    {
                        spawnIni.SetIntValue("Settings", spawnIniOption, SelectedIndex);
                    }
                    return;

                case DropDownDataWriteMode.STRING:
                    if (!string.IsNullOrEmpty(spawnIniOption))
                    {
                        spawnIni.SetStringValue("Settings", spawnIniOption, Items[SelectedIndex].Tag.ToString());
                    }
                    return;

                case DropDownDataWriteMode.SPAWN_SPAWNMAP:
                    if (!string.IsNullOrEmpty(spawnIniOption) && spawnIniValues != null && SelectedIndex < spawnIniValues.Count)
                    {
                        spawnIni.SetStringValue("Settings", spawnIniOption, spawnIniValues[SelectedIndex]);
                    }

                    string itemIniPath = Items[SelectedIndex].Tag as string;
                    if (!string.IsNullOrEmpty(itemIniPath))
                    {
                        if (!File.Exists(itemIniPath))
                        {
                            Logger.Log($"GameLobbyDropDown: Failed to load {itemIniPath} for dropdown {Name}");
                        }
                        else
                        {
                            IniFile additionalIni = new IniFile(itemIniPath);
                            IniFile.ConsolidateIniFiles(spawnIni, additionalIni);
                        }
                    }
                    return;
            }
        }

        public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
        {
            if (dataWriteMode != DropDownDataWriteMode.MAPCODE || SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return;

            string customIniPath = Items[SelectedIndex].Tag.ToString();
            MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
        }

        public override void OnLeftClick()
        {
            if (!AllowDropDown)
                return;

            base.OnLeftClick();
            UserSelectedIndex = SelectedIndex;
        }
    }
}