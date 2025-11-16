using System;

using ClientGUI;

using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Multiplayer.GameLobby;

using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Generic;

public enum CheckBoxMapScoringMode
{
    /// <summary>
    /// The value of the check box makes no difference for scoring maps.
    /// </summary>
    Irrelevant = 0,

    /// <summary>
    /// The check box prevents map scoring when it's checked.
    /// </summary>
    DenyWhenChecked = 1,

    /// <summary>
    /// The check box prevents map scoring when it's unchecked.
    /// </summary>
    DenyWhenUnchecked = 2
}

/// <summary>
/// A game option check box for the game lobby or campaign.
/// </summary>
// TODO split the logic between descendants better and clean up
public class GameSessionCheckBox : XNAClientCheckBox, IGameSessionSetting
{
    public GameSessionCheckBox(WindowManager windowManager) : base (windowManager) { }

    public bool AllowChanges { get; set; } = true;

    public bool AffectsSpawnIni => !string.IsNullOrWhiteSpace(spawnIniOption);
    public bool AffectsMapCode => !string.IsNullOrWhiteSpace(customIniPath);

    public bool AllowScoring
        => !((mapScoringMode == CheckBoxMapScoringMode.DenyWhenChecked && Checked)
             || (mapScoringMode == CheckBoxMapScoringMode.DenyWhenUnchecked && !Checked));

    private CheckBoxMapScoringMode mapScoringMode = CheckBoxMapScoringMode.Irrelevant;

    private string spawnIniOption;

    private string customIniPath;

    protected bool reversed;

    private bool defaultValue;

    private string enabledSpawnIniValue = "True";
    private string disabledSpawnIniValue = "False";

    /// <summary>
    /// Whether this checkbox should be included in the GAME broadcast.
    /// </summary>
    public bool BroadcastToLobby { get; private set; }

    /// <summary>
    /// Whether the icon should be shown in the game list.
    /// </summary>
    public bool IconShownInGameList { get; private set; }

    /// <summary>
    /// Whether the icon should be shown on the right side of the game list.
    /// Only applies if IconShownInGameList is true.
    /// </summary>
    public bool IconShownInGameListOnRight { get; private set; }

    /// <summary>
    /// Whether the icon should be shown in the game information panel.
    /// </summary>
    public bool IconShownInGameInfo { get; private set; }

    /// <summary>
    /// Whether the icon should be shown in the game filters panel.
    /// </summary>
    public bool IconShownInFilters { get; private set; }

    /// <summary>
    /// The texture name for the icon when setting is enabled.
    /// </summary>
    public string EnabledIcon { get; private set; }

    /// <summary>
    /// The texture name for the icon when setting is disabled.
    /// </summary>
    public string DisabledIcon { get; private set; }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "SpawnIniOption":
                spawnIniOption = value;
                return;
            case "EnabledSpawnIniValue":
                enabledSpawnIniValue = value;
                return;
            case "DisabledSpawnIniValue":
                disabledSpawnIniValue = value;
                return;
            case "CustomIniPath":
                customIniPath = value;
                return;
            case "Reversed":
                reversed = Conversions.BooleanFromString(value, false);
                return;
            case "Checked":
                bool checkedValue = Conversions.BooleanFromString(value, false);
                Checked = checkedValue;
                defaultValue = checkedValue;
                return;
            case "MapScoringMode":
                mapScoringMode = (CheckBoxMapScoringMode)Enum.Parse(typeof(CheckBoxMapScoringMode), value);
                return;
            case "BroadcastToLobby":
                BroadcastToLobby = Conversions.BooleanFromString(value, false);
                return;
            case "IconShownInGameList":
                IconShownInGameList = Conversions.BooleanFromString(value, false);
                return;
            case "IconShownInGameListOnRight":
                IconShownInGameListOnRight = Conversions.BooleanFromString(value, false);
                return;
            case "IconShownInGameInfo":
                IconShownInGameInfo = Conversions.BooleanFromString(value, false);
                return;
            case "IconShownInFilters":
                IconShownInFilters = Conversions.BooleanFromString(value, false);
                return;
            case "EnabledIcon":
                EnabledIcon = value;
                return;
            case "DisabledIcon":
                DisabledIcon = value;
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public int Value
    {
        get => Checked ? 1 : 0;  // 0 = unchecked/off, 1 = checked/on
        set => Checked = value != 0;  // 0 = unchecked/off, 1 = checked/on
    }

    public void ApplySpawnIniCode(IniFile spawnIni)
    {
        if (!AffectsSpawnIni)
            return;

        string value = disabledSpawnIniValue;
        if (Checked != reversed)
        {
            value = enabledSpawnIniValue;
        }

        spawnIni.SetStringValue("Settings", spawnIniOption, value);
    }
        
    public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
    {
        if (!AffectsMapCode || Checked == reversed)
            return;

        MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
    }

    public override void OnLeftClick(InputEventArgs inputEventArgs)
    {
        // FIXME there's a discrepancy with how base XNAUI handles this
        // it doesn't set handled if changing the setting is not allowed
        inputEventArgs.Handled = true;
            
        if (!AllowChanges)
            return;

        base.OnLeftClick(inputEventArgs);
    }
}