using System;

using ClientCore.Extensions;
using ClientCore.Settings;

using ClientGUI;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels;

internal class AudioOptionsPanel : XNAOptionsPanel
{
    public AudioOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
        : base(windowManager, iniSettings)
    {
    }

    private XNATrackbar trbScoreVolume;
    private XNATrackbar trbSoundVolume;
    private XNATrackbar trbVoiceVolume;

    private XNALabel lblScoreVolumeValue;
    private XNALabel lblSoundVolumeValue;
    private XNALabel lblVoiceVolumeValue;

    private XNAClientCheckBox chkScoreShuffle;

    private XNALabel lblClientVolumeValue;
    private XNATrackbar trbClientVolume;

    private XNAClientCheckBox chkMainMenuMusic;
    private XNAClientCheckBox chkStopMusicOnMenu;

    public override void Initialize()
    {
        base.Initialize();

        Name = "AudioOptionsPanel";

        XNALabel lblScoreVolume = new(WindowManager)
        {
            Name = "lblScoreVolume",
            ClientRectangle = new Rectangle(12, 14, 0, 0),
            Text = "Music Volume:".L10N("Client:DTAConfig:MusicVolume")
        };

        lblScoreVolumeValue = new XNALabel(WindowManager)
        {
            Name = "lblScoreVolumeValue",
            FontIndex = 1,
            Text = "0"
        };
        lblScoreVolumeValue.ClientRectangle = new Rectangle(
            Width - lblScoreVolumeValue.Width - 12,
            lblScoreVolume.Y, 0, 0);

        trbScoreVolume = new XNATrackbar(WindowManager)
        {
            Name = "trbScoreVolume",
            ClientRectangle = new Rectangle(
            lblScoreVolume.Right + 16,
            lblScoreVolume.Y - 2,
            lblScoreVolumeValue.X - lblScoreVolume.Right - 31,
            22),
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2),
            MinValue = 0,
            MaxValue = 10
        };
        trbScoreVolume.ValueChanged += TrbScoreVolume_ValueChanged;

        XNALabel lblSoundVolume = new(WindowManager)
        {
            Name = "lblSoundVolume",
            ClientRectangle = new Rectangle(lblScoreVolume.X,
            lblScoreVolume.Bottom + 20, 0, 0),
            Text = "Sound Volume:".L10N("Client:DTAConfig:SoundVolume")
        };

        lblSoundVolumeValue = new XNALabel(WindowManager)
        {
            Name = "lblSoundVolumeValue",
            FontIndex = 1,
            Text = "0",
            ClientRectangle = new Rectangle(
            lblScoreVolumeValue.X,
            lblSoundVolume.Y, 0, 0)
        };

        trbSoundVolume = new XNATrackbar(WindowManager)
        {
            Name = "trbSoundVolume",
            ClientRectangle = new Rectangle(
            trbScoreVolume.X,
            lblSoundVolume.Y - 2,
            trbScoreVolume.Width,
            trbScoreVolume.Height),
            BackgroundTexture = trbScoreVolume.BackgroundTexture,
            MinValue = 0,
            MaxValue = 10
        };
        trbSoundVolume.ValueChanged += TrbSoundVolume_ValueChanged;

        XNALabel lblVoiceVolume = new(WindowManager)
        {
            Name = "lblVoiceVolume",
            ClientRectangle = new Rectangle(lblScoreVolume.X,
            lblSoundVolume.Bottom + 20, 0, 0),
            Text = "Voice Volume:".L10N("Client:DTAConfig:VoiceVolume")
        };

        lblVoiceVolumeValue = new XNALabel(WindowManager)
        {
            Name = "lblVoiceVolumeValue",
            FontIndex = 1,
            Text = "0",
            ClientRectangle = new Rectangle(
            lblScoreVolumeValue.X,
            lblVoiceVolume.Y, 0, 0)
        };

        trbVoiceVolume = new XNATrackbar(WindowManager)
        {
            Name = "trbVoiceVolume",
            ClientRectangle = new Rectangle(
            trbScoreVolume.X,
            lblVoiceVolume.Y - 2,
            trbScoreVolume.Width,
            trbScoreVolume.Height),
            BackgroundTexture = trbScoreVolume.BackgroundTexture,
            MinValue = 0,
            MaxValue = 10
        };
        trbVoiceVolume.ValueChanged += TrbVoiceVolume_ValueChanged;

        chkScoreShuffle = new XNAClientCheckBox(WindowManager)
        {
            Name = "chkScoreShuffle",
            ClientRectangle = new Rectangle(
            lblScoreVolume.X,
            trbVoiceVolume.Bottom + 12, 0, 0),
            Text = "Shuffle Music".L10N("Client:DTAConfig:ShuffleMusic")
        };

        XNALabel lblClientVolume = new(WindowManager)
        {
            Name = "lblClientVolume",
            ClientRectangle = new Rectangle(lblScoreVolume.X,
            chkScoreShuffle.Bottom + 40, 0, 0),
            Text = "Client Volume:".L10N("Client:DTAConfig:ClientVolume")
        };

        lblClientVolumeValue = new XNALabel(WindowManager)
        {
            Name = "lblClientVolumeValue",
            FontIndex = 1,
            Text = "0",
            ClientRectangle = new Rectangle(
            lblScoreVolumeValue.X,
            lblClientVolume.Y, 0, 0)
        };

        trbClientVolume = new XNATrackbar(WindowManager)
        {
            Name = "trbClientVolume",
            ClientRectangle = new Rectangle(
            trbScoreVolume.X,
            lblClientVolume.Y - 2,
            trbScoreVolume.Width,
            trbScoreVolume.Height),
            BackgroundTexture = trbScoreVolume.BackgroundTexture,
            MinValue = 0,
            MaxValue = 10
        };
        trbClientVolume.ValueChanged += TrbClientVolume_ValueChanged;

        chkMainMenuMusic = new XNAClientCheckBox(WindowManager)
        {
            Name = "chkMainMenuMusic",
            ClientRectangle = new Rectangle(
            lblScoreVolume.X,
            trbClientVolume.Bottom + 12, 0, 0),
            Text = "Main menu music".L10N("Client:DTAConfig:MainMenuMusic")
        };
        chkMainMenuMusic.CheckedChanged += ChkMainMenuMusic_CheckedChanged;

        chkStopMusicOnMenu = new XNAClientCheckBox(WindowManager)
        {
            Name = "chkStopMusicOnMenu",
            ClientRectangle = new Rectangle(
            lblScoreVolume.X, chkMainMenuMusic.Bottom + 24, 0, 0),
            Text = "Don't play main menu music in lobbies".L10N("Client:DTAConfig:NoLobbiesMusic")
        };

        AddChild(lblScoreVolume);
        AddChild(lblScoreVolumeValue);
        AddChild(trbScoreVolume);
        AddChild(lblSoundVolume);
        AddChild(lblSoundVolumeValue);
        AddChild(trbSoundVolume);
        AddChild(lblVoiceVolume);
        AddChild(lblVoiceVolumeValue);
        AddChild(trbVoiceVolume);

        AddChild(chkScoreShuffle);

        AddChild(lblClientVolume);
        AddChild(lblClientVolumeValue);
        AddChild(trbClientVolume);

        AddChild(chkMainMenuMusic);
        AddChild(chkStopMusicOnMenu);

        WindowManager.SoundPlayer.SetVolume(trbClientVolume.Value / 10.0f);
    }

    private void ChkMainMenuMusic_CheckedChanged(object sender, EventArgs e)
    {
        chkStopMusicOnMenu.AllowChecking = chkMainMenuMusic.Checked;
        chkStopMusicOnMenu.Checked = chkMainMenuMusic.Checked;
    }

    private void TrbScoreVolume_ValueChanged(object sender, EventArgs e)
    {
        lblScoreVolumeValue.Text = trbScoreVolume.Value.ToString();
    }

    private void TrbSoundVolume_ValueChanged(object sender, EventArgs e)
    {
        lblSoundVolumeValue.Text = trbSoundVolume.Value.ToString();
    }

    private void TrbVoiceVolume_ValueChanged(object sender, EventArgs e)
    {
        lblVoiceVolumeValue.Text = trbVoiceVolume.Value.ToString();
    }

    private void TrbClientVolume_ValueChanged(object sender, EventArgs e)
    {
        lblClientVolumeValue.Text = trbClientVolume.Value.ToString();
        WindowManager.SoundPlayer.SetVolume(trbClientVolume.Value / 10.0f);
    }

    public override void Load()
    {
        base.Load();

        trbScoreVolume.Value = (int)(IniSettings.ScoreVolume * 10);
        trbSoundVolume.Value = (int)(IniSettings.SoundVolume * 10);
        trbVoiceVolume.Value = (int)(IniSettings.VoiceVolume * 10);

        chkScoreShuffle.Checked = IniSettings.IsScoreShuffle;

        trbClientVolume.Value = (int)(IniSettings.ClientVolume * 10);

        chkMainMenuMusic.Checked = IniSettings.PlayMainMenuMusic;
        chkStopMusicOnMenu.Checked = IniSettings.StopMusicOnMenu;
    }

    public override bool Save()
    {
        bool restartRequired = base.Save();

        IniSettings.ScoreVolume.Value = trbScoreVolume.Value / 10.0;
        IniSettings.SoundVolume.Value = trbSoundVolume.Value / 10.0;
        IniSettings.VoiceVolume.Value = trbVoiceVolume.Value / 10.0;

        IniSettings.IsScoreShuffle.Value = chkScoreShuffle.Checked;

        IniSettings.ClientVolume.Value = trbClientVolume.Value / 10.0;

        IniSettings.PlayMainMenuMusic.Value = chkMainMenuMusic.Checked;
        IniSettings.StopMusicOnMenu.Value = chkStopMusicOnMenu.Checked;

        return restartRequired;
    }
}