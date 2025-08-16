using System;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using ClientCore.Extensions;
using ClientCore.I18N;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A game option drop-down for the game lobby.
    /// </summary>
    public class GameLobbyDropDown : XNAClientDropDown
    {
        public GameLobbyDropDown(WindowManager windowManager) : base(windowManager) { }

        public string OptionName { get; private set; }

        public int HostSelectedIndex { get; set; }

        public int UserSelectedIndex { get; set; }

        private DropDownDataWriteMode dataWriteMode = DropDownDataWriteMode.BOOLEAN;

        private string spawnIniOption = string.Empty;

        private string[] spawnIniValues = Array.Empty<string>();

        private int defaultIndex;

        public override void Initialize()
        {
            // Find the game lobby that this control belongs to and register ourselves as a game option.

            XNAControl parent = Parent;
            while (true)
            {
                if (parent == null)
                    break;

                // oh no, we have a circular class reference here!
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
            // shorthand for localization function
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
                case "SpawnIniValues":
                    spawnIniValues = value.Split(',');
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

        /// <summary>
        /// Applies the drop down's associated code to spawn.ini.
        /// </summary>
        public void ApplySpawnIniCode(IniFile spawnIni)
        {
            if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return;

            if (string.IsNullOrEmpty(spawnIniOption))
            {
                Logger.Log("GameLobbyDropDown.WriteSpawnIniCode: " + Name + " has no associated spawn INI option!");
                return;
            }

            switch (dataWriteMode)
            {
                case DropDownDataWriteMode.BOOLEAN:
                    spawnIni.SetBooleanValue("Settings", spawnIniOption, SelectedIndex > 0);
                    break;
                case DropDownDataWriteMode.INDEX:
                    spawnIni.SetIntValue("Settings", spawnIniOption, SelectedIndex);
                    break;
                case DropDownDataWriteMode.STRING:
                    spawnIni.SetStringValue("Settings", spawnIniOption, Items[SelectedIndex].Tag.ToString());
                    break;
                case DropDownDataWriteMode.SPAWN_SPAWNMAP:
                    if (spawnIniValues.Length > SelectedIndex)
                    {
                        spawnIni.SetStringValue("Settings", spawnIniOption, spawnIniValues[SelectedIndex]);
                    }
                    else
                    {
                        Logger.Log($"GameLobbyDropDown: SpawnIniValues missing index {SelectedIndex} for {Name}");
                    }
                    break;
            }
        }

        /// <summary>
        /// Applies the drop down's associated code to the map INI file.
        /// </summary>
        public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
        {
            if ((dataWriteMode != DropDownDataWriteMode.MAPCODE &&
                 dataWriteMode != DropDownDataWriteMode.SPAWN_SPAWNMAP) ||
                 SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return;

            string customIniPath = Items[SelectedIndex].Tag.ToString();
            MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
        }

        public override void OnLeftClick(InputEventArgs inputEventArgs)
        {
            // FIXME there's a discrepancy with how base XNAUI handles this
            // it doesn't set handled if changing the setting is not allowed
            inputEventArgs.Handled = true;
            
            if (!AllowDropDown)
                return;

            base.OnLeftClick(inputEventArgs);
            UserSelectedIndex = SelectedIndex;
        }
    }
}