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
                    string[] itemLabels = iniFile.GetStringValue(Name, "ItemLabels", "").Split(',');
                    for (int i = 0; i < items.Length; i++)
                    {
                        bool hasLabel = itemLabels.Length > i && !string.IsNullOrEmpty(itemLabels[i]);
                        XNADropDownItem item = new()
                        {
                            Text = Localize(this, $"Item{i}",
                                hasLabel ? itemLabels[i] : items[i]),
                            Tag = items[i],
                        };
                        AddItem(item);
                    }
                    return;
                case "DataWriteMode":
                    if (value.ToUpper() == "INDEX")
                        dataWriteMode = DropDownDataWriteMode.INDEX;
                    else if (value.ToUpper() == "BOOLEAN")
                        dataWriteMode = DropDownDataWriteMode.BOOLEAN;
                    else if (value.ToUpper() == "MAPCODE")
                        dataWriteMode = DropDownDataWriteMode.MAPCODE;
                    else
                        dataWriteMode = DropDownDataWriteMode.STRING;
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

        /// <summary>
        /// Applies the drop down's associated code to spawn.ini.
        /// </summary>
        /// <param name="spawnIni">The spawn INI file.</param>
        public void ApplySpawnIniCode(IniFile spawnIni)
        {
            if (dataWriteMode == DropDownDataWriteMode.MAPCODE || SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return;

            if (String.IsNullOrEmpty(spawnIniOption))
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
                default:
                case DropDownDataWriteMode.STRING:
                    spawnIni.SetStringValue("Settings", spawnIniOption, Items[SelectedIndex].Tag.ToString());
                    break;
            }

        }

        /// <summary>
        /// Applies the drop down's associated code to the map INI file.
        /// </summary>
        /// <param name="mapIni">The map INI file.</param>
        /// <param name="gameMode">Currently selected gamemode, if set.</param>
        public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
        {
            if (dataWriteMode != DropDownDataWriteMode.MAPCODE || SelectedIndex < 0 || SelectedIndex >= Items.Count) return;

            string customIniPath;
            customIniPath = Items[SelectedIndex].Tag.ToString();

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
