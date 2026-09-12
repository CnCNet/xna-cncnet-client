#nullable enable

using System;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using Microsoft.Xna.Framework;

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
    private const int MAX_AGE_DAYS_LIMIT = 3650;

    /// <summary>Exposes the content panel, which holds everything that scrolls.</summary>
    private sealed class StorageScrollPanel : XNAScrollPanel
    {
        public StorageScrollPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public XNAPanel Content => ContentPanel;
    }

    public StorageOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
        : base(windowManager, iniSettings)
    {
    }

    private StorageScrollPanel scrollPanel = null!;

    private XNATextBox tbMaxKeptLogFiles = null!;
    private XNATextBox tbMaxLogFolderSize = null!;
    private XNATextBox tbMaxKeptSavedGames = null!;
    private XNATextBox tbMaxSavedGameFolderSize = null!;

    private XNATextBox? tbMaxGameLogAge;
    private XNATextBox? tbMaxGameLogFolderSize;

    public override void Initialize()
    {
        base.Initialize();

        Name = "StorageOptionsPanel";

        // The sections outgrow the panel once every feature is enabled, so they scroll.
        scrollPanel = new StorageScrollPanel(WindowManager);
        scrollPanel.Name = nameof(scrollPanel);
        scrollPanel.ClientRectangle = new Rectangle(0, 0, Width, Height);
        scrollPanel.DrawBorders = false;
        scrollPanel.AllowScroll = (false, true);
        // Arrow keys belong to the text boxes.
        scrollPanel.AllowKeyboardInput = false;
        scrollPanel.ScrollStep = ROW_SPACING;
        AddChild(scrollPanel);

        int nextSectionY = InitializeClientLogSection(14);

        if (GameLogManager.IsSupported)
            nextSectionY = InitializeGameLogSection(nextSectionY);

        nextSectionY = InitializeSavedGameSection(nextSectionY);

        // A spacer so the last row does not sit flush against the bottom edge when scrolled down.
        var bottomMargin = new XNAPanel(WindowManager);
        bottomMargin.Name = nameof(bottomMargin);
        bottomMargin.DrawBorders = false;
        bottomMargin.ClientRectangle = new Rectangle(0, nextSectionY, 1, 1);
        AddContent(bottomMargin);
    }

    private void AddContent(params XNAControl[] controls)
    {
        foreach (XNAControl control in controls)
            scrollPanel.Content.AddChild(control);
    }

    private int InitializeClientLogSection(int y)
    {
        var lblLogsHeader = new XNALabel(WindowManager);
        lblLogsHeader.Name = nameof(lblLogsHeader);
        lblLogsHeader.FontIndex = 1;
        lblLogsHeader.Text = "Client Logs".L10N("Client:DTAConfig:StorageLogsHeader");
        lblLogsHeader.ClientRectangle = new Rectangle(12, y, 0, 0);

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

        AddContent(lblLogsHeader, lblKeptLogFiles, tbMaxKeptLogFiles, lblKeptLogFilesSuffix,
            lblLogFolderSize, tbMaxLogFolderSize, lblLogFolderSizeSuffix);

        return lblLogFolderSize.Y + ROW_SPACING;
    }

    private int InitializeGameLogSection(int y)
    {
        var lblGameLogsHeader = new XNALabel(WindowManager);
        lblGameLogsHeader.Name = nameof(lblGameLogsHeader);
        lblGameLogsHeader.FontIndex = 1;
        lblGameLogsHeader.Text = "Game Logs".L10N("Client:DTAConfig:StorageGameLogsHeader");
        lblGameLogsHeader.ClientRectangle = new Rectangle(12, y, 0, 0);

        var lblGameLogAge = new XNALabel(WindowManager);
        lblGameLogAge.Name = nameof(lblGameLogAge);
        lblGameLogAge.Text = "Delete after:".L10N("Client:DTAConfig:StorageDeleteAfter");
        lblGameLogAge.ClientRectangle = new Rectangle(12, lblGameLogsHeader.Bottom + ROW_SPACING - 12, 0, 0);

        var tbMaxGameLogAge = new XNATextBox(WindowManager);
        tbMaxGameLogAge.Name = nameof(tbMaxGameLogAge);
        tbMaxGameLogAge.MaximumTextLength = 4;
        tbMaxGameLogAge.ClientRectangle = new Rectangle(
            TEXT_BOX_X, lblGameLogAge.Y - 4, TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT);
        this.tbMaxGameLogAge = tbMaxGameLogAge;

        var lblGameLogAgeSuffix = new XNALabel(WindowManager);
        lblGameLogAgeSuffix.Name = nameof(lblGameLogAgeSuffix);
        lblGameLogAgeSuffix.Text = "days  (0 = never)".L10N("Client:DTAConfig:StorageDeleteAfterDaysSuffix");
        lblGameLogAgeSuffix.ClientRectangle = new Rectangle(
            tbMaxGameLogAge.Right + 8, lblGameLogAge.Y, 0, 0);

        var lblGameLogFolderSize = new XNALabel(WindowManager);
        lblGameLogFolderSize.Name = nameof(lblGameLogFolderSize);
        lblGameLogFolderSize.Text = "Maximum size:".L10N("Client:DTAConfig:StorageMaxSize");
        lblGameLogFolderSize.ClientRectangle = new Rectangle(12, lblGameLogAge.Y + ROW_SPACING, 0, 0);

        var tbMaxGameLogFolderSize = new XNATextBox(WindowManager);
        tbMaxGameLogFolderSize.Name = nameof(tbMaxGameLogFolderSize);
        tbMaxGameLogFolderSize.MaximumTextLength = 7;
        tbMaxGameLogFolderSize.ClientRectangle = new Rectangle(
            TEXT_BOX_X, lblGameLogFolderSize.Y - 4, TEXT_BOX_WIDTH, TEXT_BOX_HEIGHT);
        this.tbMaxGameLogFolderSize = tbMaxGameLogFolderSize;

        var lblGameLogFolderSizeSuffix = new XNALabel(WindowManager);
        lblGameLogFolderSizeSuffix.Name = nameof(lblGameLogFolderSizeSuffix);
        lblGameLogFolderSizeSuffix.Text = "MB  (0 = no limit)".L10N("Client:DTAConfig:StorageMaxSizeSuffix");
        lblGameLogFolderSizeSuffix.ClientRectangle = new Rectangle(
            tbMaxGameLogFolderSize.Right + 8, lblGameLogFolderSize.Y, 0, 0);

        var lblGameLogRetentionHint = new XNALabel(WindowManager);
        lblGameLogRetentionHint.Name = nameof(lblGameLogRetentionHint);
        lblGameLogRetentionHint.ClientRectangle = new Rectangle(12, lblGameLogFolderSize.Y + ROW_SPACING, 0, 0);
        lblGameLogRetentionHint.Text = ("The game's debug folder: its logs and the snapshots saved for crashes and desyncs.\n" +
            "Applied at client startup; the newest is always kept.").L10N("Client:DTAConfig:StorageGameLogRetentionHint");

        AddContent(lblGameLogsHeader, lblGameLogAge, tbMaxGameLogAge, lblGameLogAgeSuffix,
            lblGameLogFolderSize, tbMaxGameLogFolderSize, lblGameLogFolderSizeSuffix, lblGameLogRetentionHint);

        return lblGameLogRetentionHint.Bottom + 12;
    }

    private int InitializeSavedGameSection(int y)
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
        lblSavedGameRetentionHint.ClientRectangle = new Rectangle(12, lblSavedGameFolderSize.Y + ROW_SPACING, 0, 0);
        lblSavedGameRetentionHint.Text = ("Limits permanently delete oldest saves at client startup and after games.\n" +
            "The newest save is always kept, even if it exceeds the size limit.").L10N("Client:DTAConfig:StorageSavedGameRetentionHint");

        AddContent(lblSavedGamesHeader, lblKeptSavedGames, tbMaxKeptSavedGames, lblKeptSavedGamesSuffix,
            lblSavedGameFolderSize, tbMaxSavedGameFolderSize, lblSavedGameFolderSizeSuffix, lblSavedGameRetentionHint);

        return lblSavedGameRetentionHint.Bottom + 12;
    }

    public override void Load()
    {
        base.Load();

        tbMaxKeptLogFiles.Text = IniSettings.MaxKeptClientLogFiles.Value.ToString();
        tbMaxLogFolderSize.Text = IniSettings.MaxClientLogFolderSizeMB.Value.ToString();
        tbMaxKeptSavedGames.Text = IniSettings.MaxKeptSavedGames.Value.ToString();
        tbMaxSavedGameFolderSize.Text = IniSettings.MaxSavedGameFolderSizeMB.Value.ToString();

        if (GameLogManager.IsSupported)
        {
            tbMaxGameLogAge!.Text = IniSettings.MaxGameLogAgeDays.Value.ToString();
            tbMaxGameLogFolderSize!.Text = IniSettings.MaxGameLogFolderSizeMB.Value.ToString();
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

        if (GameLogManager.IsSupported)
        {
            IniSettings.MaxGameLogAgeDays.Value =
                ParseLimit(tbMaxGameLogAge!.Text, IniSettings.MaxGameLogAgeDays.Value, MAX_AGE_DAYS_LIMIT);
            IniSettings.MaxGameLogFolderSizeMB.Value =
                ParseLimit(tbMaxGameLogFolderSize!.Text, IniSettings.MaxGameLogFolderSizeMB.Value, MAX_FOLDER_SIZE_LIMIT_MB);
        }

        return restartRequired;
    }

    private static int ParseLimit(string? text, int previousValue, int maximum)
    {
        if (!int.TryParse(text?.Trim(), out int value) || value < 0)
            return previousValue;

        return Math.Min(value, maximum);
    }
}
