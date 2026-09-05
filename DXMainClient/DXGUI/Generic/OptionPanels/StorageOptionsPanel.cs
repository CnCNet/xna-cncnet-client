#nullable enable

using System;
using System.IO;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using DTAClient.Domain;

using Microsoft.Xna.Framework;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic.OptionPanels;

/// <summary>Configures how much disk space is used for various client features.</summary>
class StorageOptionsPanel : XNAOptionsPanel
{
    private const int TEXT_BOX_WIDTH = 70;
    private const int TEXT_BOX_HEIGHT = 21;
    private const int TEXT_BOX_X = 170;
    private const int ROW_SPACING = 30;
    private const int MAX_KEPT_FILES_LIMIT = 100000;
    private const int MAX_FOLDER_SIZE_LIMIT_MB = 1024 * 1024;

    public StorageOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
        : base(windowManager, iniSettings)
    {
    }

    private XNATextBox tbMaxKeptLogFiles = null!;
    private XNATextBox tbMaxLogFolderSize = null!;
    private XNATextBox tbMaxKeptSavedGames = null!;
    private XNATextBox tbMaxSavedGameFolderSize = null!;

    private XNATextBox? tbMaxKeptReplays;
    private XNATextBox? tbMaxReplayFolderSize;
    private XNATextBox? tbReplayKeyframeStorageLimit;
    private XNALabel? lblReplayUsage;

    public override void Initialize()
    {
        base.Initialize();

        Name = "StorageOptionsPanel";

        var lblLogsHeader = new XNALabel(WindowManager);
        lblLogsHeader.Name = nameof(lblLogsHeader);
        lblLogsHeader.FontIndex = 1;
        lblLogsHeader.Text = "Client Logs".L10N("Client:DTAConfig:StorageLogsHeader");
        lblLogsHeader.ClientRectangle = new Rectangle(12, 14, 0, 0);

        var lblKeptLogFiles = new XNALabel(WindowManager);
        lblKeptLogFiles.Name = nameof(lblKeptLogFiles);
        lblKeptLogFiles.Text = "Keep at most:".L10N("Client:DTAConfig:StorageKeepAtMost");
        lblKeptLogFiles.ClientRectangle = new Rectangle(12, lblLogsHeader.Bottom + ROW_SPACING - 12, 0, 0);

        tbMaxKeptLogFiles = new XNATextBox(WindowManager);
        tbMaxKeptLogFiles.Name = nameof(tbMaxKeptLogFiles);
        tbMaxKeptLogFiles.MaximumTextLength = 6;
        tbMaxKeptLogFiles.ClientRectangle = new Rectangle(
            TEXT_BOX_X, lblKeptLogFiles.Y - 4, TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT);

        var lblKeptLogFilesSuffix = new XNALabel(WindowManager);
        lblKeptLogFilesSuffix.Name = nameof(lblKeptLogFilesSuffix);
        lblKeptLogFilesSuffix.Text = "old log files  (0 = no limit)".L10N("Client:DTAConfig:StorageKeepLogsAtMostSuffix");
        lblKeptLogFilesSuffix.ClientRectangle = new Rectangle(
            tbMaxKeptLogFiles.Right + 8, lblKeptLogFiles.Y, 0, 0);

        var lblLogFolderSize = new XNALabel(WindowManager);
        lblLogFolderSize.Name = nameof(lblLogFolderSize);
        lblLogFolderSize.Text = "Maximum size:".L10N("Client:DTAConfig:StorageMaxSize");
        lblLogFolderSize.ClientRectangle = new Rectangle(12, lblKeptLogFiles.Y + ROW_SPACING, 0, 0);

        tbMaxLogFolderSize = new XNATextBox(WindowManager);
        tbMaxLogFolderSize.Name = nameof(tbMaxLogFolderSize);
        tbMaxLogFolderSize.MaximumTextLength = 7;
        tbMaxLogFolderSize.ClientRectangle = new Rectangle(
            TEXT_BOX_X, lblLogFolderSize.Y - 4, TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT);

        var lblLogFolderSizeSuffix = new XNALabel(WindowManager);
        lblLogFolderSizeSuffix.Name = nameof(lblLogFolderSizeSuffix);
        lblLogFolderSizeSuffix.Text = "MB  (0 = no limit)".L10N("Client:DTAConfig:StorageMaxSizeSuffix");
        lblLogFolderSizeSuffix.ClientRectangle = new Rectangle(
            tbMaxLogFolderSize.Right + 8, lblLogFolderSize.Y, 0, 0);

        AddChild(lblLogsHeader);
        AddChild(lblKeptLogFiles);
        AddChild(tbMaxKeptLogFiles);
        AddChild(lblKeptLogFilesSuffix);
        AddChild(lblLogFolderSize);
        AddChild(tbMaxLogFolderSize);
        AddChild(lblLogFolderSizeSuffix);

        InitializeSavedGameSection(lblLogFolderSize.Y + ROW_SPACING * 2);
        int nextSectionY = lblLogFolderSize.Y + ROW_SPACING;

        if (ReplayManager.IsSupported)
            nextSectionY = InitializeReplaySections(nextSectionY);
    }

    private void InitializeSavedGameSection(int y)
    {
        var lblSavedGamesHeader = new XNALabel(WindowManager);
        lblSavedGamesHeader.Name = nameof(lblSavedGamesHeader);
        lblSavedGamesHeader.FontIndex = 1;
        lblSavedGamesHeader.Text = "Single-player Saved Games".L10N("Client:DTAConfig:StorageSavedGamesHeader");
        lblSavedGamesHeader.ClientRectangle = new Rectangle(12, y, 0, 0);

        var lblKeptSavedGames = new XNALabel(WindowManager);
        lblKeptSavedGames.Name = nameof(lblKeptSavedGames);
        lblKeptSavedGames.Text = "Keep at most:".L10N("Client:DTAConfig:StorageKeepAtMost");
        lblKeptSavedGames.ClientRectangle = new Rectangle(12, lblSavedGamesHeader.Bottom + ROW_SPACING - 12, 0, 0);

        tbMaxKeptSavedGames = new XNATextBox(WindowManager);
        tbMaxKeptSavedGames.Name = nameof(tbMaxKeptSavedGames);
        tbMaxKeptSavedGames.MaximumTextLength = 6;
        tbMaxKeptSavedGames.ClientRectangle = new Rectangle(
            TEXT_BOX_X, lblKeptSavedGames.Y - 4, TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT);

        var lblKeptSavedGamesSuffix = new XNALabel(WindowManager);
        lblKeptSavedGamesSuffix.Name = nameof(lblKeptSavedGamesSuffix);
        lblKeptSavedGamesSuffix.Text = "saved games  (0 = no limit)".L10N("Client:DTAConfig:StorageKeepSavedGamesAtMostSuffix");
        lblKeptSavedGamesSuffix.ClientRectangle = new Rectangle(
            tbMaxKeptSavedGames.Right + 8, lblKeptSavedGames.Y, 0, 0);

        var lblSavedGameFolderSize = new XNALabel(WindowManager);
        lblSavedGameFolderSize.Name = nameof(lblSavedGameFolderSize);
        lblSavedGameFolderSize.Text = "Maximum size:".L10N("Client:DTAConfig:StorageMaxSize");
        lblSavedGameFolderSize.ClientRectangle = new Rectangle(12, lblKeptSavedGames.Y + ROW_SPACING, 0, 0);

        tbMaxSavedGameFolderSize = new XNATextBox(WindowManager);
        tbMaxSavedGameFolderSize.Name = nameof(tbMaxSavedGameFolderSize);
        tbMaxSavedGameFolderSize.MaximumTextLength = 7;
        tbMaxSavedGameFolderSize.ClientRectangle = new Rectangle(
            TEXT_BOX_X, lblSavedGameFolderSize.Y - 4, TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT);

        var lblSavedGameFolderSizeSuffix = new XNALabel(WindowManager);
        lblSavedGameFolderSizeSuffix.Name = nameof(lblSavedGameFolderSizeSuffix);
        lblSavedGameFolderSizeSuffix.Text = "MB  (0 = no limit)".L10N("Client:DTAConfig:StorageMaxSizeSuffix");
        lblSavedGameFolderSizeSuffix.ClientRectangle = new Rectangle(
            tbMaxSavedGameFolderSize.Right + 8, lblSavedGameFolderSize.Y, 0, 0);

        var lblSavedGameRetentionHint = new XNALabel(WindowManager);
        lblSavedGameRetentionHint.Name = nameof(lblSavedGameRetentionHint);
        lblSavedGameRetentionHint.Text = ("Limits permanently delete oldest saves at client startup and after games.\n" +
            "The newest save is always kept, even if it exceeds the size limit.").L10N("Client:DTAConfig:StorageSavedGameRetentionHint");
        lblSavedGameRetentionHint.ClientRectangle = new Rectangle(12, lblSavedGameFolderSize.Y + ROW_SPACING, 0, 0);

        AddChild(lblSavedGamesHeader);
        AddChild(lblKeptSavedGames);
        AddChild(tbMaxKeptSavedGames);
        AddChild(lblKeptSavedGamesSuffix);
        AddChild(lblSavedGameFolderSize);
        AddChild(tbMaxSavedGameFolderSize);
        AddChild(lblSavedGameFolderSizeSuffix);
        AddChild(lblSavedGameRetentionHint);
    }

    private int InitializeReplaySections(int y)
    {
        var lblReplaysHeader = new XNALabel(WindowManager);
        lblReplaysHeader.Name = nameof(lblReplaysHeader);
        lblReplaysHeader.FontIndex = 1;
        lblReplaysHeader.Text = "Replays".L10N("Client:DTAConfig:StorageReplaysHeader");
        lblReplaysHeader.ClientRectangle = new Rectangle(12, y + ROW_SPACING, 0, 0);

        var lblKeptReplays = new XNALabel(WindowManager);
        lblKeptReplays.Name = nameof(lblKeptReplays);
        lblKeptReplays.Text = "Keep at most:".L10N("Client:DTAConfig:StorageKeepAtMost");
        lblKeptReplays.ClientRectangle = new Rectangle(12, lblReplaysHeader.Bottom + ROW_SPACING - 12, 0, 0);

        var tbMaxKeptReplays = new XNATextBox(WindowManager);
        tbMaxKeptReplays.Name = nameof(tbMaxKeptReplays);
        tbMaxKeptReplays.MaximumTextLength = 6;
        tbMaxKeptReplays.ClientRectangle = new Rectangle(
            TEXT_BOX_X, lblKeptReplays.Y - 4, TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT);
        this.tbMaxKeptReplays = tbMaxKeptReplays;

        var lblKeptReplaysSuffix = new XNALabel(WindowManager);
        lblKeptReplaysSuffix.Name = nameof(lblKeptReplaysSuffix);
        lblKeptReplaysSuffix.Text = "replays  (0 = no limit)".L10N("Client:DTAConfig:StorageKeepAtMostSuffix");
        lblKeptReplaysSuffix.ClientRectangle = new Rectangle(
            tbMaxKeptReplays.Right + 8, lblKeptReplays.Y, 0, 0);

        var lblFolderSize = new XNALabel(WindowManager);
        lblFolderSize.Name = nameof(lblFolderSize);
        lblFolderSize.Text = "Maximum size:".L10N("Client:DTAConfig:StorageMaxSize");
        lblFolderSize.ClientRectangle = new Rectangle(12, lblKeptReplays.Y + ROW_SPACING, 0, 0);

        var tbMaxReplayFolderSize = new XNATextBox(WindowManager);
        tbMaxReplayFolderSize.Name = nameof(tbMaxReplayFolderSize);
        tbMaxReplayFolderSize.MaximumTextLength = 7;
        tbMaxReplayFolderSize.ClientRectangle = new Rectangle(
            TEXT_BOX_X, lblFolderSize.Y - 4, TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT);
        this.tbMaxReplayFolderSize = tbMaxReplayFolderSize;

        var lblFolderSizeSuffix = new XNALabel(WindowManager);
        lblFolderSizeSuffix.Name = nameof(lblFolderSizeSuffix);
        lblFolderSizeSuffix.Text = "MB  (0 = no limit)".L10N("Client:DTAConfig:StorageMaxSizeSuffix");
        lblFolderSizeSuffix.ClientRectangle = new Rectangle(
            tbMaxReplayFolderSize.Right + 8, lblFolderSize.Y, 0, 0);

        var lblReplayUsage = new XNALabel(WindowManager);
        lblReplayUsage.Name = nameof(lblReplayUsage);
        lblReplayUsage.ClientRectangle = new Rectangle(12, lblFolderSize.Y + ROW_SPACING + 6, 0, 0);
        this.lblReplayUsage = lblReplayUsage;

        var lblKeyframesHeader = new XNALabel(WindowManager);
        lblKeyframesHeader.Name = nameof(lblKeyframesHeader);
        lblKeyframesHeader.FontIndex = 1;
        lblKeyframesHeader.Text = "Playback keyframes".L10N("Client:DTAConfig:StorageKeyframesHeader");
        lblKeyframesHeader.ClientRectangle = new Rectangle(12, lblReplayUsage.Y + ROW_SPACING, 0, 0);

        var lblKeyframeSize = new XNALabel(WindowManager);
        lblKeyframeSize.Name = nameof(lblKeyframeSize);
        lblKeyframeSize.Text = "Maximum size:".L10N("Client:DTAConfig:StorageKeyframeMaxSize");
        lblKeyframeSize.ClientRectangle = new Rectangle(12, lblKeyframesHeader.Bottom + ROW_SPACING - 12, 0, 0);

        var tbReplayKeyframeStorageLimit = new XNATextBox(WindowManager);
        tbReplayKeyframeStorageLimit.Name = nameof(tbReplayKeyframeStorageLimit);
        tbReplayKeyframeStorageLimit.MaximumTextLength = 7;
        tbReplayKeyframeStorageLimit.ClientRectangle = new Rectangle(
            TEXT_BOX_X, lblKeyframeSize.Y - 4, TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT);
        this.tbReplayKeyframeStorageLimit = tbReplayKeyframeStorageLimit;

        var lblKeyframeSizeSuffix = new XNALabel(WindowManager);
        lblKeyframeSizeSuffix.Name = nameof(lblKeyframeSizeSuffix);
        lblKeyframeSizeSuffix.Text = "MB  (0 = no limit)".L10N("Client:DTAConfig:StorageKeyframeMaxSizeSuffix");
        lblKeyframeSizeSuffix.ClientRectangle = new Rectangle(
            tbReplayKeyframeStorageLimit.Right + 8, lblKeyframeSize.Y, 0, 0);

        AddChild(lblReplaysHeader);
        AddChild(lblKeptReplays);
        AddChild(tbMaxKeptReplays);
        AddChild(lblKeptReplaysSuffix);
        AddChild(lblFolderSize);
        AddChild(tbMaxReplayFolderSize);
        AddChild(lblFolderSizeSuffix);
        AddChild(lblReplayUsage);
        AddChild(lblKeyframesHeader);
        AddChild(lblKeyframeSize);
        AddChild(tbReplayKeyframeStorageLimit);
        AddChild(lblKeyframeSizeSuffix);

        return lblKeyframeSize.Y + ROW_SPACING;
    }

    public override void Load()
    {
        base.Load();

        tbMaxKeptLogFiles.Text = IniSettings.MaxKeptClientLogFiles.Value.ToString();
        tbMaxLogFolderSize.Text = IniSettings.MaxClientLogFolderSizeMB.Value.ToString();
        tbMaxKeptSavedGames.Text = IniSettings.MaxKeptSavedGames.Value.ToString();
        tbMaxSavedGameFolderSize.Text = IniSettings.MaxSavedGameFolderSizeMB.Value.ToString();


        if (ReplayManager.IsSupported)
        {
            tbMaxKeptReplays!.Text = IniSettings.MaxKeptReplays.Value.ToString();
            tbMaxReplayFolderSize!.Text = IniSettings.MaxReplayFolderSizeMB.Value.ToString();
            tbReplayKeyframeStorageLimit!.Text = IniSettings.ReplayKeyframeStorageLimitMB.Value.ToString();

            RefreshUsageLabel();
        }
    }

    public override bool Save()
    {
        bool restartRequired = base.Save();

        IniSettings.MaxKeptClientLogFiles.Value =
            ParseLimit(tbMaxKeptLogFiles.Text, IniSettings.MaxKeptClientLogFiles.Value, MAX_KEPT_FILES_LIMIT);
        IniSettings.MaxClientLogFolderSizeMB.Value =
            ParseLimit(tbMaxLogFolderSize.Text, IniSettings.MaxClientLogFolderSizeMB.Value, MAX_FOLDER_SIZE_LIMIT_MB);
        IniSettings.MaxKeptSavedGames.Value =
            ParseLimit(tbMaxKeptSavedGames.Text, IniSettings.MaxKeptSavedGames.Value, MAX_KEPT_FILES_LIMIT);
        IniSettings.MaxSavedGameFolderSizeMB.Value =
            ParseLimit(tbMaxSavedGameFolderSize.Text, IniSettings.MaxSavedGameFolderSizeMB.Value, MAX_FOLDER_SIZE_LIMIT_MB);

        if (ReplayManager.IsSupported)
        {
            IniSettings.MaxKeptReplays.Value =
                ParseLimit(tbMaxKeptReplays!.Text, IniSettings.MaxKeptReplays.Value, MAX_KEPT_FILES_LIMIT);
            IniSettings.MaxReplayFolderSizeMB.Value =
                ParseLimit(tbMaxReplayFolderSize!.Text, IniSettings.MaxReplayFolderSizeMB.Value, MAX_FOLDER_SIZE_LIMIT_MB);
            IniSettings.ReplayKeyframeStorageLimitMB.Value =
                ParseLimit(tbReplayKeyframeStorageLimit!.Text,
                    IniSettings.ReplayKeyframeStorageLimitMB.Value, MAX_FOLDER_SIZE_LIMIT_MB);
        }

        return restartRequired;
    }

    public override bool RefreshPanel()
    {
        bool valuesChanged = base.RefreshPanel();

        if (ReplayManager.IsSupported)
            RefreshUsageLabel();

        return valuesChanged;
    }

    private void RefreshUsageLabel()
    {
        int count = 0;
        long bytes = 0;

        try
        {
            DirectoryInfo directory = ReplayManager.GetDirectory();
            if (directory.Exists)
            {
                foreach (FileInfo file in directory.EnumerateFiles(ReplayManager.SearchPattern))
                {
                    count++;
                    bytes += file.Length;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log("StorageOptionsPanel: could not measure the replay directory: " + ex.Message);
        }

        lblReplayUsage!.Text = string.Format(
            "Currently stored: {0} replays, {1:0.#} MB".L10N("Client:DTAConfig:StorageReplayUsage"),
            count, bytes / (1024.0 * 1024.0));
    }

    private static int ParseLimit(string? text, int previousValue, int maximum)
    {
        if (!int.TryParse(text?.Trim(), out int value) || value < 0)
            return previousValue;

        return Math.Min(value, maximum);
    }
}