#nullable enable

using System;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic.OptionPanels;

/// <summary>Configures how much disk space client logs and (if supported) replays are allowed to use.</summary>
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
    }

    public override void Load()
    {
        base.Load();

        tbMaxKeptLogFiles.Text = IniSettings.MaxKeptClientLogFiles.Value.ToString();
        tbMaxLogFolderSize.Text = IniSettings.MaxClientLogFolderSizeMB.Value.ToString();
    }

    public override bool Save()
    {
        bool restartRequired = base.Save();

        IniSettings.MaxKeptClientLogFiles.Value =
            ParseLimit(tbMaxKeptLogFiles.Text, IniSettings.MaxKeptClientLogFiles.Value, MAX_KEPT_FILES_LIMIT);
        IniSettings.MaxClientLogFolderSizeMB.Value =
            ParseLimit(tbMaxLogFolderSize.Text, IniSettings.MaxClientLogFolderSizeMB.Value, MAX_FOLDER_SIZE_LIMIT_MB);

        return restartRequired;
    }

    private static int ParseLimit(string? text, int previousValue, int maximum)
    {
        if (!int.TryParse(text?.Trim(), out int value) || value < 0)
            return previousValue;

        return Math.Min(value, maximum);
    }
}