using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework.Media;
using Rampastring.XNAUI.XNAControls;
using ClientGUI;
using Microsoft.Xna.Framework;
using ClientCore;

namespace DTAConfig
{
    class AudioOptionsPanel : XNAOptionsPanel
    {
        public AudioOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        XNATrackbar trbScoreVolume;
        XNATrackbar trbSoundVolume;
        XNATrackbar trbVoiceVolume;

        XNALabel lblScoreVolumeValue;
        XNALabel lblSoundVolumeValue;
        XNALabel lblVoiceVolumeValue;

        XNAClientCheckBox chkScoreShuffle;

        XNALabel lblClientVolumeValue;
        XNATrackbar trbClientVolume;

        XNAClientCheckBox chkMainMenuMusic;

        List<FileSettingCheckBox> fileSettingCheckBoxes = new List<FileSettingCheckBox>();

        public override void Initialize()
        {
            base.Initialize();

            var lblScoreVolume = new XNALabel(WindowManager);
            lblScoreVolume.Name = "lblScoreVolume";
            lblScoreVolume.ClientRectangle = new Rectangle(12, 14, 0, 0);
            lblScoreVolume.Text = "Music Volume:";

            lblScoreVolumeValue = new XNALabel(WindowManager);
            lblScoreVolumeValue.Name = "lblScoreVolumeValue";
            lblScoreVolumeValue.FontIndex = 1;
            lblScoreVolumeValue.Text = "10";
            lblScoreVolumeValue.ClientRectangle = new Rectangle(
                ClientRectangle.Width - lblScoreVolumeValue.ClientRectangle.Width - 12,
                lblScoreVolume.ClientRectangle.Y, 0, 0);

            trbScoreVolume = new XNATrackbar(WindowManager);
            trbScoreVolume.Name = "trbScoreVolume";
            trbScoreVolume.ClientRectangle = new Rectangle(
                lblScoreVolume.ClientRectangle.Right + 16,
                lblScoreVolume.ClientRectangle.Y - 2,
                lblScoreVolumeValue.ClientRectangle.X - lblScoreVolume.ClientRectangle.Right - 31,
                22);
            trbScoreVolume.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            trbScoreVolume.MinValue = 0;
            trbScoreVolume.MaxValue = 10;
            trbScoreVolume.ValueChanged += TrbScoreVolume_ValueChanged;

            var lblSoundVolume = new XNALabel(WindowManager);
            lblSoundVolume.Name = "lblSoundVolume";
            lblSoundVolume.ClientRectangle = new Rectangle(lblScoreVolume.ClientRectangle.X,
                lblScoreVolume.ClientRectangle.Bottom + 20, 0, 0);
            lblSoundVolume.Text = "Sound Volume:";

            lblSoundVolumeValue = new XNALabel(WindowManager);
            lblSoundVolumeValue.Name = "lblSoundVolumeValue";
            lblSoundVolumeValue.FontIndex = 1;
            lblSoundVolumeValue.Text = "10";
            lblSoundVolumeValue.ClientRectangle = new Rectangle(
                lblScoreVolumeValue.ClientRectangle.X,
                lblSoundVolume.ClientRectangle.Y, 0, 0);

            trbSoundVolume = new XNATrackbar(WindowManager);
            trbSoundVolume.Name = "trbSoundVolume";
            trbSoundVolume.ClientRectangle = new Rectangle(
                trbScoreVolume.ClientRectangle.X,
                lblSoundVolume.ClientRectangle.Y - 2,
                trbScoreVolume.ClientRectangle.Width,
                trbScoreVolume.ClientRectangle.Height);
            trbSoundVolume.BackgroundTexture = trbScoreVolume.BackgroundTexture;
            trbSoundVolume.MinValue = 0;
            trbSoundVolume.MaxValue = 10;
            trbSoundVolume.ValueChanged += TrbSoundVolume_ValueChanged;

            var lblVoiceVolume = new XNALabel(WindowManager);
            lblVoiceVolume.Name = "lblVoiceVolume";
            lblVoiceVolume.ClientRectangle = new Rectangle(lblScoreVolume.ClientRectangle.X,
                lblSoundVolume.ClientRectangle.Bottom + 20, 0, 0);
            lblVoiceVolume.Text = "Voice Volume:";

            lblVoiceVolumeValue = new XNALabel(WindowManager);
            lblVoiceVolumeValue.Name = "lblVoiceVolumeValue";
            lblVoiceVolumeValue.FontIndex = 1;
            lblVoiceVolumeValue.Text = "10";
            lblVoiceVolumeValue.ClientRectangle = new Rectangle(
                lblScoreVolumeValue.ClientRectangle.X,
                lblVoiceVolume.ClientRectangle.Y, 0, 0);

            trbVoiceVolume = new XNATrackbar(WindowManager);
            trbVoiceVolume.Name = "trbVoiceVolume";
            trbVoiceVolume.ClientRectangle = new Rectangle(
                trbScoreVolume.ClientRectangle.X,
                lblVoiceVolume.ClientRectangle.Y - 2,
                trbScoreVolume.ClientRectangle.Width,
                trbScoreVolume.ClientRectangle.Height);
            trbVoiceVolume.BackgroundTexture = trbScoreVolume.BackgroundTexture;
            trbVoiceVolume.MinValue = 0;
            trbVoiceVolume.MaxValue = 10;
            trbVoiceVolume.ValueChanged += TrbVoiceVolume_ValueChanged;

            chkScoreShuffle = new XNAClientCheckBox(WindowManager);
            chkScoreShuffle.Name = "chkScoreShuffle";
            chkScoreShuffle.ClientRectangle = new Rectangle(
                lblScoreVolume.ClientRectangle.X,
                trbVoiceVolume.ClientRectangle.Bottom + 12, 0, 0);
            chkScoreShuffle.Text = "Shuffle Music";

            var lblClientVolume = new XNALabel(WindowManager);
            lblClientVolume.Name = "lblClientVolume";
            lblClientVolume.ClientRectangle = new Rectangle(lblScoreVolume.ClientRectangle.X,
                chkScoreShuffle.ClientRectangle.Bottom + 40, 0, 0);
            lblClientVolume.Text = "Client Volume:";

            lblClientVolumeValue = new XNALabel(WindowManager);
            lblClientVolumeValue.Name = "lblClientVolumeValue";
            lblClientVolumeValue.FontIndex = 1;
            lblClientVolumeValue.Text = "10";
            lblClientVolumeValue.ClientRectangle = new Rectangle(
                lblScoreVolumeValue.ClientRectangle.X,
                lblClientVolume.ClientRectangle.Y, 0, 0);

            trbClientVolume = new XNATrackbar(WindowManager);
            trbClientVolume.Name = "trbClientVolume";
            trbClientVolume.ClientRectangle = new Rectangle(
                trbScoreVolume.ClientRectangle.X,
                lblClientVolume.ClientRectangle.Y - 2,
                trbScoreVolume.ClientRectangle.Width,
                trbScoreVolume.ClientRectangle.Height);
            trbClientVolume.BackgroundTexture = trbScoreVolume.BackgroundTexture;
            trbClientVolume.MinValue = 0;
            trbClientVolume.MaxValue = 10;
            trbClientVolume.ValueChanged += TrbClientVolume_ValueChanged;

            chkMainMenuMusic = new XNAClientCheckBox(WindowManager);
            chkMainMenuMusic.Name = "chkMainMenuMusic";
            chkMainMenuMusic.ClientRectangle = new Rectangle(
                lblScoreVolume.ClientRectangle.X,
                trbClientVolume.ClientRectangle.Bottom + 12, 0, 0);
            chkMainMenuMusic.Text = "Main Menu Music";

#if DTA
            var chkRABuildingCrumbleSound = new FileSettingCheckBox(WindowManager,
                "Resources\\Ecache03.mix", "MIX\\Ecache03.mix", false);
            chkRABuildingCrumbleSound.Name = "chkRABuildingCrumbleSound";
            chkRABuildingCrumbleSound.ClientRectangle = new Rectangle(
                chkMainMenuMusic.ClientRectangle.X,
                chkMainMenuMusic.ClientRectangle.Bottom + 24, 0, 0);
            chkRABuildingCrumbleSound.Text = "Use Red Alert building crumble sound";

            var chkReplaceRACannonSounds = new FileSettingCheckBox(WindowManager,
                "Resources\\Ecache02.mix", "MIX\\Ecache02.mix", false);
            chkReplaceRACannonSounds.Name = "chkReplaceRACannonSounds";
            chkReplaceRACannonSounds.ClientRectangle = new Rectangle(
                chkMainMenuMusic.ClientRectangle.X,
                chkRABuildingCrumbleSound.ClientRectangle.Bottom + 24, 0, 0);
            chkReplaceRACannonSounds.Text = "Replace Red Alert cannon sounds with Tiberian Dawn cannon sounds";

            fileSettingCheckBoxes.Add(chkRABuildingCrumbleSound);
            fileSettingCheckBoxes.Add(chkReplaceRACannonSounds);
#endif

            fileSettingCheckBoxes.ForEach(chkBox => AddChild(chkBox));

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

            lblScoreVolumeValue.Text = trbScoreVolume.Value.ToString();
            lblSoundVolumeValue.Text = trbSoundVolume.Value.ToString();
            lblVoiceVolumeValue.Text = trbVoiceVolume.Value.ToString();
            lblClientVolumeValue.Text = trbClientVolume.Value.ToString();
            AudioMaster.SetVolume(trbClientVolume.Value / 10.0f);
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
            AudioMaster.SetVolume(trbClientVolume.Value / 10.0f);
        }

        public override void Load()
        {
            trbScoreVolume.Value = (int)(IniSettings.ScoreVolume * 10);
            trbSoundVolume.Value = (int)(IniSettings.SoundVolume * 10);
            trbVoiceVolume.Value = (int)(IniSettings.VoiceVolume * 10);

            chkScoreShuffle.Checked = IniSettings.IsScoreShuffle;

            trbClientVolume.Value = (int)(IniSettings.ClientVolume * 10);

            chkMainMenuMusic.Checked = IniSettings.PlayMainMenuMusic;

            fileSettingCheckBoxes.ForEach(chkBox => chkBox.Load());
        }

        public override bool Save()
        {
            IniSettings.ScoreVolume.Value = trbScoreVolume.Value / 10.0;
            IniSettings.SoundVolume.Value = trbSoundVolume.Value / 10.0;
            IniSettings.VoiceVolume.Value = trbVoiceVolume.Value / 10.0;
            
            IniSettings.IsScoreShuffle.Value = chkScoreShuffle.Checked;

            IniSettings.ClientVolume.Value = trbClientVolume.Value / 10.0;

            IniSettings.PlayMainMenuMusic.Value = chkMainMenuMusic.Checked;

            fileSettingCheckBoxes.ForEach(chkBox => chkBox.Save());

            return false;
        }
    }
}
