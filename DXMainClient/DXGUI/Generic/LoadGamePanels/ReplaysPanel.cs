#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.PlatformShim;

using ClientGUI;

using DTAClient.Domain;
using DTAClient.DXGUI.Campaign;

using Microsoft.Xna.Framework;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic.LoadGamePanels;

/// <summary>
/// Lists recorded replays and launches playback.
/// </summary>
public class ReplaysPanel : XNAPanel
{
    private const int MAX_LISTED_MISMATCHES = 4;

    private const int LIST_HEIGHT = 240;
    private const int DETAILS_HEIGHT = 100;
    private const int ROW_SPACING = 24;
    private const int CHECK_BOX_HEIGHT = 20;

    private const int DROPDOWN_WIDTH = 190;
    private const int DROPDOWN_HEIGHT = 21;
    private const int CHECK_BOX_WIDTH = 230;
    private const int COLUMN_LABEL_GAP = 8;
    private const int COLUMN_GAP = 40;

    private const int FIXED_COLUMNS_WIDTH = 320;

    /// <summary>
    /// Frame counts offered for the spawner's seek checkpoint interval. A lower option seeks more
    /// precisely at the cost of disk space and a brief pause whenever one is taken.
    /// </summary>
    private static readonly int[] SeekCheckpointIntervals = { 0, 375, 750, 1500 };

    public ReplaysPanel(WindowManager windowManager, DiscordHandler discordHandler,
        CampaignTagSelector campaignTagSelector) : base(windowManager)
    {
        this.discordHandler = discordHandler;
        this.campaignTagSelector = campaignTagSelector;
    }

    private readonly DiscordHandler discordHandler;
    private readonly CampaignTagSelector campaignTagSelector;

    private XNAMultiColumnListBox lbReplayList = null!;
    private XNATextBlock tbDetails = null!;
    private XNAContextMenu replayContextMenu = null!;

    private XNAClientCheckBox chkShroudEnabled = null!;
    private XNAClientCheckBox chkFollowRecordedCamera = null!;
    private XNAClientCheckBox chkShowSelections = null!;
    private XNAClientCheckBox chkShowChatAndBeacons = null!;
    private XNAClientDropDown ddGameSpeed = null!;
    private XNAClientDropDown ddSeekCheckpoints = null!;
    private XNAClientDropDown ddWatchAs = null!;

    private List<ReplayGame> replays = new List<ReplayGame>();

    /// <summary>Players available in ddWatchAs, one-for-one with its items after the leading Spectator entry.</summary>
    private IReadOnlyList<ReplayPlayer> watchAsPlayers = Array.Empty<ReplayPlayer>();

    public event EventHandler? SelectionChanged;

    public event EventHandler? LaunchRequested;

    public string LaunchButtonText => "Play".L10N("Client:Main:ButtonPlayReplay");

    public bool CanLaunch => SelectedReplay?.IsPlayable == true;

    public bool CanDelete => lbReplayList.SelectedIndex > -1;

    private ReplayGame? SelectedReplay
        => lbReplayList.SelectedIndex > -1 && lbReplayList.SelectedIndex < replays.Count
            ? replays[lbReplayList.SelectedIndex]
            : null;

    private bool IsSpectatorSelected => ddWatchAs.SelectedIndex == 0;

    private ReplayPlayer? SelectedPlayer
    {
        get
        {
            int index = ddWatchAs.SelectedIndex - 1;
            return index >= 0 && index < watchAsPlayers.Count ? watchAsPlayers[index] : null;
        }
    }

    public override void Initialize()
    {
        Name = nameof(ReplaysPanel);
        DrawBorders = false;

        lbReplayList = new XNAMultiColumnListBox(WindowManager);
        lbReplayList.Name = nameof(lbReplayList);
        lbReplayList.ClientRectangle = new Rectangle(0, 0, Width, LIST_HEIGHT);
        lbReplayList.AddColumn("REPLAY".L10N("Client:Main:ReplayNameColumnHeader"), Width - FIXED_COLUMNS_WIDTH);
        lbReplayList.AddColumn("DATE / TIME".L10N("Client:Main:ReplayDateTimeColumnHeader"), 140);
        lbReplayList.AddColumn("LENGTH".L10N("Client:Main:ReplayLengthColumnHeader"), 70);
        lbReplayList.AddColumn("PLAYERS".L10N("Client:Main:ReplayPlayersColumnHeader"), 110);
        lbReplayList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
        lbReplayList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
        lbReplayList.SelectedIndexChanged += LbReplayList_SelectedIndexChanged;
        lbReplayList.RightClick += LbReplayList_RightClick;
        lbReplayList.AllowKeyboardInput = true;

        replayContextMenu = new XNAContextMenu(WindowManager);
        replayContextMenu.Name = nameof(replayContextMenu);
        replayContextMenu.Width = 160;
        replayContextMenu.AddItem("Show in folder".L10N("Client:Main:ReplayShowInFolder"),
            selectAction: () => ReplayManager.OpenDirectory());

        tbDetails = new XNATextBlock(WindowManager);
        tbDetails.Name = nameof(tbDetails);
        tbDetails.ClientRectangle = new Rectangle(0, lbReplayList.Bottom + 6, Width, DETAILS_HEIGHT);
        tbDetails.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
        tbDetails.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

        var lblPlayback = new XNALabel(WindowManager);
        lblPlayback.Name = nameof(lblPlayback);
        lblPlayback.ClientRectangle = new Rectangle(0, tbDetails.Bottom + 10, 0, 0);
        lblPlayback.Text = "Playback:".L10N("Client:Main:ReplayPlaybackSettings");

        int firstRowY = lblPlayback.Bottom + 8;

        // Three dropdown labels of different lengths share one left column, so the dropdowns
        // themselves line up: every label starts at X 0, every dropdown starts past the widest one.
        var lblWatchAs = new XNALabel(WindowManager);
        lblWatchAs.Name = nameof(lblWatchAs);
        lblWatchAs.Text = "Watch as:".L10N("Client:Main:ReplayWatchAs");

        var lblGameSpeed = new XNALabel(WindowManager);
        lblGameSpeed.Name = nameof(lblGameSpeed);
        lblGameSpeed.Text = "Speed:".L10N("Client:Main:ReplayPlaybackSpeed");

        var lblSeekCheckpoints = new XNALabel(WindowManager);
        lblSeekCheckpoints.Name = nameof(lblSeekCheckpoints);
        lblSeekCheckpoints.Text = "Seek checkpoints:".L10N("Client:Main:ReplaySeekCheckpoints");

        int dropDownLabelWidth = new[] { lblWatchAs.Width, lblGameSpeed.Width, lblSeekCheckpoints.Width }.Max();
        int dropDownX = dropDownLabelWidth + COLUMN_LABEL_GAP;
        int checkBoxX = dropDownX + DROPDOWN_WIDTH + COLUMN_GAP;

        int watchAsRowY = firstRowY;
        int gameSpeedRowY = watchAsRowY + ROW_SPACING;
        int seekCheckpointsRowY = gameSpeedRowY + ROW_SPACING;

        lblWatchAs.ClientRectangle = new Rectangle(0, watchAsRowY + 3, lblWatchAs.Width, lblWatchAs.Height);
        lblGameSpeed.ClientRectangle = new Rectangle(0, gameSpeedRowY + 3, lblGameSpeed.Width, lblGameSpeed.Height);
        lblSeekCheckpoints.ClientRectangle = new Rectangle(0, seekCheckpointsRowY + 3,
            lblSeekCheckpoints.Width, lblSeekCheckpoints.Height);

        ddWatchAs = new XNAClientDropDown(WindowManager);
        ddWatchAs.Name = nameof(ddWatchAs);
        ddWatchAs.ClientRectangle = new Rectangle(dropDownX, watchAsRowY, DROPDOWN_WIDTH, DROPDOWN_HEIGHT);
        ddWatchAs.ToolTipText = ("Whose eyes you watch through: as a spectator who can see the whole map, " +
            "or as one of the players, seeing only what they saw.")
            .L10N("Client:Main:ReplayWatchAsTooltip");
        ddWatchAs.SelectedIndexChanged += DdWatchAs_SelectedIndexChanged;

        ddGameSpeed = new XNAClientDropDown(WindowManager);
        ddGameSpeed.Name = nameof(ddGameSpeed);
        ddGameSpeed.ClientRectangle = new Rectangle(dropDownX, gameSpeedRowY, DROPDOWN_WIDTH, DROPDOWN_HEIGHT);
        ddGameSpeed.ToolTipText = ("How fast the replay is played back.").L10N("Client:Main:ReplayPlaybackSpeedTooltip");
        PopulateGameSpeedDropDown();

        ddSeekCheckpoints = new XNAClientDropDown(WindowManager);
        ddSeekCheckpoints.Name = nameof(ddSeekCheckpoints);
        ddSeekCheckpoints.ClientRectangle = new Rectangle(dropDownX, seekCheckpointsRowY, DROPDOWN_WIDTH, DROPDOWN_HEIGHT);
        ddSeekCheckpoints.ToolTipText =
            "For faster seeking, a lower option is better.".L10N("Client:Main:ReplaySeekCheckpointsTooltip");
        PopulateSeekCheckpointsDropDown();

        // The checkboxes form their own column to the right of the dropdowns.
        chkShroudEnabled = CreateCheckBox(nameof(chkShroudEnabled), checkBoxX, watchAsRowY, CHECK_BOX_WIDTH,
            "Shroud enabled".L10N("Client:Main:ReplayShroudEnabled"),
            "Hides the map outside of what the watched player could see."
                .L10N("Client:Main:ReplayShroudEnabledTooltip"),
            UserINISettings.Instance.ReplayPlaybackShroudEnabled);

        chkFollowRecordedCamera = CreateCheckBox(nameof(chkFollowRecordedCamera), checkBoxX, gameSpeedRowY, CHECK_BOX_WIDTH,
            "Follow recorded camera".L10N("Client:Main:ReplayFollowRecordedCamera"),
            "Keeps the camera on the recorded view instead of letting you move it yourself."
                .L10N("Client:Main:ReplayFollowRecordedCameraTooltip"),
            UserINISettings.Instance.ReplayPlaybackFollowCamera);

        chkShowSelections = CreateCheckBox(nameof(chkShowSelections), checkBoxX, seekCheckpointsRowY, CHECK_BOX_WIDTH,
            "Show recorded selections".L10N("Client:Main:ReplayShowSelections"),
            "Highlights the units the watched player had selected."
                .L10N("Client:Main:ReplayShowSelectionsTooltip"),
            UserINISettings.Instance.ReplayPlaybackShowSelections);

        int chatAndBeaconsRowY = seekCheckpointsRowY + ROW_SPACING;

        chkShowChatAndBeacons = CreateCheckBox(nameof(chkShowChatAndBeacons), checkBoxX, chatAndBeaconsRowY, CHECK_BOX_WIDTH,
            "Show chat and beacons".L10N("Client:Main:ReplayShowChatAndBeacons"),
            "Replays chat messages, beacons and taunts.".L10N("Client:Main:ReplayShowChatAndBeaconsTooltip"),
            UserINISettings.Instance.ReplayPlaybackShowChatAndBeacons);

        AddChild(lbReplayList);
        AddChild(replayContextMenu);
        AddChild(tbDetails);
        AddChild(lblPlayback);
        AddChild(lblWatchAs);
        AddChild(ddWatchAs);
        AddChild(lblGameSpeed);
        AddChild(ddGameSpeed);
        AddChild(lblSeekCheckpoints);
        AddChild(ddSeekCheckpoints);
        AddChild(chkShroudEnabled);
        AddChild(chkFollowRecordedCamera);
        AddChild(chkShowSelections);
        AddChild(chkShowChatAndBeacons);

        base.Initialize();
    }

    private XNAClientCheckBox CreateCheckBox(string name, int x, int y, int width, string text,
        string toolTip, bool isChecked)
    {
        var checkBox = new XNAClientCheckBox(WindowManager);
        checkBox.Name = name;
        checkBox.ClientRectangle = new Rectangle(x, y, width, CHECK_BOX_HEIGHT);
        checkBox.Text = text;
        checkBox.ToolTipText = toolTip;
        checkBox.Checked = isChecked;
        checkBox.CheckedChanged += PlaybackSetting_Changed;
        return checkBox;
    }

    private void PopulateGameSpeedDropDown()
    {
        ddGameSpeed.AddItem("As recorded".L10N("Client:Main:ReplayPlaybackSpeedAsRecorded"));

        foreach (int fps in ReplayGame.PlaybackSpeedLadder)
        {
            ddGameSpeed.AddItem(string.Format(
                "{0} FPS".L10N("Client:Main:ReplayPlaybackSpeedItem"), fps));
        }

        // Store FPS instead of an index so ladder changes preserve the selection.
        int storedFPS = UserINISettings.Instance.ReplayPlaybackGameSpeed;
        int ladderIndex = Array.IndexOf(ReplayGame.PlaybackSpeedLadder, storedFPS);
        ddGameSpeed.SelectedIndex = ladderIndex >= 0 ? ladderIndex + 1 : 0;
        ddGameSpeed.SelectedIndexChanged += PlaybackSetting_Changed;
    }

    private void PopulateSeekCheckpointsDropDown()
    {
        foreach (int interval in SeekCheckpointIntervals)
        {
            ddSeekCheckpoints.AddItem(interval <= 0
                ? "Off".L10N("Client:Main:ReplaySeekCheckpointsOff")
                : string.Format("Every {0} frames".L10N("Client:Main:ReplaySeekCheckpointsItem"), interval));
        }

        // Store the interval instead of an index, so changing the table preserves the selection.
        int storedInterval = UserINISettings.Instance.ReplayPlaybackKeyframeInterval;
        int index = Array.IndexOf(SeekCheckpointIntervals, storedInterval);
        ddSeekCheckpoints.SelectedIndex = index >= 0 ? index : Array.IndexOf(SeekCheckpointIntervals, 750);
        ddSeekCheckpoints.SelectedIndexChanged += PlaybackSetting_Changed;
    }

    /// <summary>Frames between seek checkpoints, or 0 for none.</summary>
    private int SelectedKeyframeInterval
    {
        get
        {
            int index = ddSeekCheckpoints.SelectedIndex;
            return index >= 0 && index < SeekCheckpointIntervals.Length ? SeekCheckpointIntervals[index] : 750;
        }
    }

    /// <summary>Selected playback rate in FPS, or 0 for the recorded speed.</summary>
    private int SelectedPlaybackFPS
    {
        get
        {
            int index = ddGameSpeed.SelectedIndex - 1;
            return index >= 0 && index < ReplayGame.PlaybackSpeedLadder.Length
                ? ReplayGame.PlaybackSpeedLadder[index]
                : 0;
        }
    }

    private void PlaybackSetting_Changed(object? sender, EventArgs e)
    {
        UserINISettings.Instance.ReplayPlaybackShroudEnabled.Value = chkShroudEnabled.Checked;
        UserINISettings.Instance.ReplayPlaybackFollowCamera.Value = chkFollowRecordedCamera.Checked;
        UserINISettings.Instance.ReplayPlaybackShowSelections.Value = chkShowSelections.Checked;
        UserINISettings.Instance.ReplayPlaybackSpectator.Value = IsSpectatorSelected;
        UserINISettings.Instance.ReplayPlaybackShowChatAndBeacons.Value = chkShowChatAndBeacons.Checked;
        UserINISettings.Instance.ReplayPlaybackGameSpeed.Value = SelectedPlaybackFPS;
        UserINISettings.Instance.ReplayPlaybackKeyframeInterval.Value = SelectedKeyframeInterval;

        UserINISettings.Instance.SaveSettings();

        UpdatePlaybackOptionAvailability();
    }

    private void DdWatchAs_SelectedIndexChanged(object? sender, EventArgs e)
        => PlaybackSetting_Changed(sender, e);

    private void LbReplayList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateForSelection();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LbReplayList_RightClick(object? sender, EventArgs e)
    {
        if (lbReplayList.HoveredIndex < 0 || lbReplayList.HoveredIndex >= lbReplayList.ItemCount)
            return;

        replayContextMenu.Open(GetCursorPoint());
    }

    private void UpdateForSelection()
    {
        UpdateDetails();
        UpdateWatchAsDropDown();
    }

    private void UpdateWatchAsDropDown()
    {
        // Watch-as indices are replay-specific, so reset for the newly selected replay.
        ddWatchAs.SelectedIndexChanged -= DdWatchAs_SelectedIndexChanged;

        watchAsPlayers = SelectedReplay?.Players ?? (IReadOnlyList<ReplayPlayer>)Array.Empty<ReplayPlayer>();

        ddWatchAs.Items.Clear();
        ddWatchAs.SelectedIndex = -1;

        ddWatchAs.AddItem(Renderer.GetSafeString("Spectator".L10N("Client:Main:ReplayWatchAsSpectator"),
            ddWatchAs.FontIndex));

        foreach (ReplayPlayer player in watchAsPlayers)
        {
            ddWatchAs.AddItem(Renderer.GetSafeString(DescribeWatchAsPlayer(player),
                ddWatchAs.FontIndex));
        }

        if (UserINISettings.Instance.ReplayPlaybackSpectator)
        {
            ddWatchAs.SelectedIndex = 0;
        }
        else
        {
            int recorderIndex = -1;
            for (int i = 0; i < watchAsPlayers.Count; i++)
            {
                if (watchAsPlayers[i].IsRecorder)
                {
                    recorderIndex = i;
                    break;
                }
            }

            ddWatchAs.SelectedIndex = recorderIndex >= 0 ? recorderIndex + 1 : 0;
        }

        ddWatchAs.SelectedIndexChanged += DdWatchAs_SelectedIndexChanged;

        UpdatePlaybackOptionAvailability();
    }

    private void UpdatePlaybackOptionAvailability()
    {
        bool spectating = IsSpectatorSelected;
        bool watchingRecordingPlayer = spectating || SelectedPlayer?.IsRecorder != false;

        // The spawner ignores these options for spectator or alternate-player views.
        chkShroudEnabled.AllowChecking = !spectating;

        chkFollowRecordedCamera.AllowChecking = watchingRecordingPlayer;
        chkShowSelections.AllowChecking = watchingRecordingPlayer;
    }

    private static string DescribeWatchAsPlayer(ReplayPlayer player)
    {
        if (player.IsRecorder)
        {
            return string.Format("{0} (recorded)".L10N("Client:Main:ReplayWatchAsRecorder"),
                player.Name);
        }

        return player.IsSpectator
            ? string.Format("{0} (spectator)".L10N("Client:Main:ReplayWatchAsPlayerSpectator"), player.Name)
            : player.Name;
    }

    public void Refresh()
    {
        // Preserve selection by file name because deletion changes list indices.
        string? previouslySelected = SelectedReplay?.FileName;

        lbReplayList.ClearItems();
        lbReplayList.SelectedIndex = -1;

        replays = ReplayManager.List();

        foreach (ReplayGame replay in replays)
        {
            string[] columns =
            {
                Renderer.GetSafeString(replay.GUIName, lbReplayList.FontIndex),
                replay.RecordedAt.ToString("g"),
                FormatDuration(replay),
                replay.IsPlayable ? replay.Players.Count.ToString() : "-"
            };

            if (replay.IsPlayable)
            {
                lbReplayList.AddItem(columns, true);
                continue;
            }

            // Keep unsupported replays selectable so the details pane can explain them.
            lbReplayList.AddItem(Array.ConvertAll(columns,
                text => new XNAListBoxItem(text, UISettings.ActiveSettings.DisabledItemColor)));
        }

        if (replays.Count > 0)
        {
            int restored = previouslySelected == null
                ? 0
                : replays.FindIndex(replay =>
                    string.Equals(replay.FileName, previouslySelected, StringComparison.OrdinalIgnoreCase));

            lbReplayList.SelectedIndex = restored < 0 ? 0 : restored;
        }

        UpdateForSelection();
    }

    private void UpdateDetails()
    {
        ReplayGame? replay = SelectedReplay;

        if (replay == null)
        {
            tbDetails.Text = "Select a replay to see its details.".L10N("Client:Main:ReplayNoSelection");
            return;
        }

        if (!replay.IsPlayable)
        {
            tbDetails.Text = DescribeUnplayable(replay);
            return;
        }

        var details = new StringBuilder();

        details.Append(SafeForDetails(replay.GUIName));
        if (!string.IsNullOrWhiteSpace(replay.UIGameMode))
            details.Append(" - ").Append(SafeForDetails(replay.UIGameMode));
        details.AppendLine();

        if (replay.Players.Count > 0)
        {
            details.Append(string.Format("Players: {0}".L10N("Client:Main:ReplayDetailPlayers"),
                SafeForDetails(string.Join(", ", replay.Players.Select(player => player.Name)))));
            details.AppendLine();
        }

        details.Append(string.Format("Recorded {0}".L10N("Client:Main:ReplayDetailRecorded"),
            replay.RecordedAt.ToString("f")));
        details.AppendLine();

        if (replay.IsComplete)
        {
            details.Append(string.Format("Length: {0} - {1} frames"
                .L10N("Client:Main:ReplayDetailLength"),
                FormatDuration(replay), replay.TotalFrames.ToString("N0")));
            details.AppendLine();
        }

        details.Append(string.Format("Version: {0}".L10N("Client:Main:ReplayDetailVersion"),
            SafeForDetails(GetDisplayVersion(replay))));

        if (!replay.IsComplete)
        {
            details.AppendLine();
            details.Append("This recording did not finish cleanly, so it stops early."
                .L10N("Client:Main:ReplayIncomplete"));
        }

        tbDetails.Text = details.ToString();
    }

    private ReplayPlayer? LaunchPerspective => IsSpectatorSelected ? null : SelectedPlayer;

    public void Launch()
    {
        ReplayGame? replay = SelectedReplay;

        // Double-click can call Launch even when the button is disabled.
        if (replay == null || !replay.IsPlayable)
            return;

        Logger.Log("Loading replay " + replay.FileName);

        if (!replay.TryReadSpawnFiles(out string spawnIniContent, out byte[] spawnMapContent))
        {
            Logger.Log("Replay file is missing spawn file data: " + replay.FileName);
            XNAMessageBox.Show(WindowManager,
                "Replay Unreadable".L10N("Client:Main:ReplayUnreadableTitle"),
                ("This replay does not contain the game setup it was recorded with, so it " +
                 "cannot be played back.").L10N("Client:Main:ReplayUnreadableText"));
            return;
        }

        IniFile spawnIni = ReadReplaySpawnIni(spawnIniContent);

        List<string> fileMismatches;
        try
        {
            // Check the same supplemental files that campaign recording had installed.
            PrepareMissionFiles(spawnIni);
            fileMismatches = ReplayFileHashes.FindMismatches(spawnIni);
        }
        finally
        {
            // A mismatch prompt can be cancelled, so leave no mission files installed yet.
            CustomMissionHelper.DeleteSupplementalMissionFiles();
        }

        if (fileMismatches.Count > 0)
        {
            ShowMismatchPrompt(replay, spawnIni, spawnMapContent, LaunchPerspective, fileMismatches);
            return;
        }

        StartPlayback(replay, spawnIni, spawnMapContent, LaunchPerspective);
    }

    private static IniFile ReadReplaySpawnIni(string spawnIniContent)
    {
        using MemoryStream stream = new MemoryStream(EncodingExt.UTF8NoBOM.GetBytes(spawnIniContent));
        return new IniFile(stream, EncodingExt.UTF8NoBOM, applyBaseIni: false);
    }

    private void PrepareMissionFiles(IniFile spawnIni)
    {
        CustomMissionHelper.DeleteSupplementalMissionFiles();

        if (!spawnIni.GetBooleanValue("Settings", "IsSinglePlayer", false))
            return;

        int customMissionID = spawnIni.GetIntValue("Settings", "CustomMissionID", 0);
        if (campaignTagSelector.UniqueIDToMissions.TryGetValue(customMissionID, out Mission mission))
            CustomMissionHelper.CopySupplementalMissionFiles(mission);
    }

    private void ShowMismatchPrompt(ReplayGame replay, IniFile spawnIni, byte[] spawnMapContent,
        ReplayPlayer? perspective, List<string> fileMismatches)
    {
        foreach (string mismatch in fileMismatches)
            Logger.Log("Replay file mismatch: " + mismatch);

        string details = string.Join("\n\n", fileMismatches.Take(MAX_LISTED_MISMATCHES));

        if (fileMismatches.Count > MAX_LISTED_MISMATCHES)
        {
            details += "\n\n" + string.Format("...and {0} more".L10N("Client:Main:ReplayMoreMismatches"),
                fileMismatches.Count - MAX_LISTED_MISMATCHES);
        }

        var msgBox = new XNAMessageBox(WindowManager,
            "Replay File Mismatch".L10N("Client:Main:ReplayFileMismatchTitle"),
            string.Format(("This replay was recorded with different game files, so it will " +
                "most likely not play back correctly - it may load and then do nothing.\n\n" +
                "Recorded with: {0}\n" +
                "You have: {1}\n\n" +
                "{2}\n\n" +
                "Play it anyway?").L10N("Client:Main:ReplayFileMismatchText"),
                SafeForDialog(GetDisplayVersion(replay)), SafeForDialog(ReplayManager.GameClientVersion),
                SafeForDialog(details)),
            XNAMessageBoxButtons.YesNo);

        msgBox.YesClickedAction = _ => StartPlayback(replay, spawnIni, spawnMapContent, perspective);
        msgBox.Show();
    }

    private void StartPlayback(ReplayGame replay, IniFile spawnIni, byte[] spawnMapContent,
        ReplayPlayer? perspective)
    {
        spawnIni.SetStringValue("Settings", "ReplayFile", ReplayManager.GetRelativePath(replay.FileName));

        // ReplayViewPlayer is a spawn.ini player slot.
        spawnIni.SetIntValue("Settings", "ReplayViewPlayer", perspective?.SpawnIniIndex ?? 0);

        // Use the selected player's loading screen when available.
        if (perspective != null && !perspective.IsRecorder && perspective.SideIndex >= 0)
        {
            spawnIni.SetStringValue("Settings", "CustomLoadScreen",
                LoadingScreenController.GetLoadScreenName(perspective.SideIndex.ToString()));
        }

        // Playback speed changes pacing, not the recorded simulation speed.
        spawnIni.SetIntValue("Settings", "ReplayPlaybackSpeed", SelectedPlaybackFPS);

        spawnIni.SetBooleanValue("Settings", "ReplayShroudEnabled", chkShroudEnabled.Checked);
        // The spawner's flag is phrased as freedom, not following, so this is the negation of the checkbox.
        spawnIni.SetBooleanValue("Settings", "ReplayFreeCamera", !chkFollowRecordedCamera.Checked);
        spawnIni.SetBooleanValue("Settings", "ReplayShowSelections", chkShowSelections.Checked);
        spawnIni.SetBooleanValue("Settings", "ReplaySpectator", IsSpectatorSelected);
        spawnIni.SetBooleanValue("Settings", "ReplayShowChatAndBeacons", chkShowChatAndBeacons.Checked);

        // Keyframes make rewinding faster and are deleted after playback.
        spawnIni.SetIntValue("Settings", "ReplayKeyframeInterval", SelectedKeyframeInterval);
        spawnIni.SetIntValue("Settings", "ReplayKeyframeStorageLimitMB",
            UserINISettings.Instance.ReplayKeyframeStorageLimitMB.Value);

        spawnIni.SetBooleanValue("Settings", "EnableReplayRecording", false);

        spawnIni.RemoveSection(ReplayFileHashes.SECTION);

        FileInfo spawnerSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);
        if (spawnerSettingsFile.Exists)
            spawnerSettingsFile.Delete();

        spawnIni.FileName = spawnerSettingsFile.FullName;
        spawnIni.Encoding = EncodingExt.UTF8NoBOM;
        spawnIni.WriteIniFile();

        FileInfo spawnMapIniFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNMAP_INI);
        if (spawnMapIniFile.Exists)
            spawnMapIniFile.Delete();

        // Campaign missions can load from game archives without an embedded map.
        if (spawnMapContent.Length > 0)
            File.WriteAllBytes(spawnMapIniFile.FullName, spawnMapContent);

        Logger.Log("Extracted spawn files from replay successfully");

        discordHandler.UpdatePresence(replay.GUIName, true);

        LaunchRequested?.Invoke(this, EventArgs.Empty);

        bool gameStarted = false;
        Action onGameStarted = () => gameStarted = true;
        GameProcessLogic.GameProcessStarted += onGameStarted;

        try
        {
            PrepareMissionFiles(spawnIni);
            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;
            GameProcessLogic.StartGameProcess(WindowManager);
        }
        finally
        {
            GameProcessLogic.GameProcessStarted -= onGameStarted;

            // Startup can return early without raising GameProcessExited.
            if (!gameStarted)
            {
                GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;
                CustomMissionHelper.DeleteSupplementalMissionFiles();
            }
        }
    }

    private void GameProcessExited_Callback()
    {
        WindowManager.AddCallback(new Action(GameProcessExited), null);
    }

    protected virtual void GameProcessExited()
    {
        GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;

        CustomMissionHelper.DeleteSupplementalMissionFiles();

        discordHandler.UpdatePresence();
        Refresh();
    }

    public void Delete()
    {
        ReplayGame? replay = SelectedReplay;
        if (replay == null)
            return;

        var msgBox = new XNAMessageBox(WindowManager, "Delete Confirmation".L10N("Client:Main:DeleteConfirmationTitle"),
            string.Format(("The following replay will be deleted permanently:\n\n" +
                "Filename: {0}\n" +
                "Replay: {1}\n" +
                "Date and time: {2}\n\n" +
                "Are you sure you want to proceed?").L10N("Client:Main:DeleteReplayConfirmationText"),
                SafeForDialog(replay.FileName), SafeForDialog(replay.GUIName),
                replay.RecordedAt.ToString()),
            XNAMessageBoxButtons.YesNo);

        msgBox.YesClickedAction = _ =>
        {
            if (!ReplayManager.Delete(replay))
            {
                XNAMessageBox.Show(WindowManager,
                    "Deleting Replay Failed".L10N("Client:Main:ReplayDeleteFailedTitle"),
                    string.Format(("The replay {0} could not be deleted. It may still be open in " +
                        "the game or in another program.").L10N("Client:Main:ReplayDeleteFailedText"),
                        SafeForDialog(replay.FileName)));
            }

            Refresh();
        };
        msgBox.Show();
    }

    /// <summary>Makes replay metadata renderable with the details font.</summary>
    private string SafeForDetails(string text) => Renderer.GetSafeString(text, tbDetails.FontIndex);

    /// <summary>Makes replay metadata renderable in message boxes.</summary>
    private static string SafeForDialog(string text) => Renderer.GetSafeString(text, 0);

    private string DescribeUnplayable(ReplayGame replay)
    {
        var details = new StringBuilder();

        details.Append(SafeForDetails(replay.GUIName));
        details.AppendLine();
        details.Append(string.Format("Recorded {0}".L10N("Client:Main:ReplayDetailRecorded"),
            replay.RecordedAt.ToString("f")));
        details.AppendLine();

        details.Append(("This replay was recorded in a newer format than this version of the game " +
            "can read. Update the game to watch it.").L10N("Client:Main:ReplayUnsupportedVersion"));

        return details.ToString();
    }

    private static string GetDisplayVersion(ReplayGame replay)
        => string.IsNullOrWhiteSpace(replay.GameClientVersion)
            ? "Unknown".L10N("Client:Main:ReplayUnknownVersion")
            : replay.GameClientVersion;

    private static string FormatDuration(ReplayGame replay)
    {
        if (!replay.IsPlayable || !replay.IsComplete)
            return "?";

        TimeSpan duration = replay.Duration;
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}"
            : $"{duration.Minutes}:{duration.Seconds:D2}";
    }
}
