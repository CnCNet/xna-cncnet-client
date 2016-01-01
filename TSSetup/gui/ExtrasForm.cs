using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Media;
using System.Diagnostics;
using ClientCore;
using ClientGUI;
using dtasetup.domain;

namespace dtasetup.gui
{
    public partial class ExtrasForm : Form
    {
        public ExtrasForm()
        {
            InitializeComponent();
        }

        private void ExtrasForm_Load(object sender, EventArgs e)
        {
            SetStyle();
        }

        private void btnExtraCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnExtraMapEditor_Click(object sender, EventArgs e)
        {
            Process.Start(ProgramConstants.gamepath + MCDomainController.Instance().GetMapEditorExePath());
        }

        private void btnExtraStatistics_Click(object sender, EventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(ProgramConstants.gamepath + "statistics.dat");
            startInfo.Arguments = "\"-RESDIR=" + ProgramConstants.RESOURCES_DIR.Remove(ProgramConstants.RESOURCES_DIR.Length - 1) + "\"";
            startInfo.UseShellExecute = false;

            Logger.Log("Starting DTAScore viewer.");

            Process process = new Process();
            process.StartInfo = startInfo;
            this.Hide();

            process.Start();
        }

        private void btnExtraCredits_Click(object sender, EventArgs e)
        {
            Process.Start(MainClientConstants.CREDITS_URL);
        }

        /// <summary>
        /// Initializes the visual style of the main menu.
        /// </summary>
        private void SetStyle()
        {
            IniFile clientThemeIni = new IniFile(MainClientConstants.gamepath + ProgramConstants.RESOURCES_DIR + "ExtrasMenu.ini");

            this.BackgroundImage = SharedUILogic.LoadImage("extrasMenu.png");
            this.Size = this.BackgroundImage.Size;

            SoundPlayer sPlayer = new SoundPlayer(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "MainMenu\\button.wav");
            btnExtraMapEditor.DefaultImage = SharedUILogic.LoadImage("133pxbtn.png");
            btnExtraMapEditor.HoveredImage = SharedUILogic.LoadImage("133pxbtn_c.png");
            btnExtraMapEditor.RefreshSize();
            btnExtraMapEditor.HoverSound = sPlayer;

            btnExtraStatistics.DefaultImage = btnExtraMapEditor.DefaultImage;
            btnExtraStatistics.HoveredImage = btnExtraMapEditor.HoveredImage;
            btnExtraStatistics.RefreshSize();
            btnExtraStatistics.HoverSound = sPlayer;

            btnExtraCancel.DefaultImage = btnExtraMapEditor.DefaultImage;
            btnExtraCancel.HoveredImage = btnExtraMapEditor.HoveredImage;
            btnExtraCancel.RefreshSize();
            btnExtraCancel.HoverSound = sPlayer;

            btnExtraCredits.DefaultImage = btnExtraMapEditor.DefaultImage;
            btnExtraCredits.HoveredImage = btnExtraMapEditor.HoveredImage;
            btnExtraCredits.RefreshSize();
            btnExtraCredits.HoverSound = sPlayer;

            this.ForeColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetUIAltColor());

            string pbSectionName = "ExtraPictureBoxes";

            List<string> pbKeys = clientThemeIni.GetSectionKeys(pbSectionName);

            if (pbKeys != null)
            {
                foreach (string keyName in pbKeys)
                {
                    string name = clientThemeIni.GetStringValue(pbSectionName, keyName, null);

                    if (name == null)
                        throw new InvalidDataException("ExtrasMenu.ini: Invalid data in section " + pbSectionName);

                    PictureBox pb = new PictureBox();
                    pb.Name = name;
                    pb.BorderStyle = BorderStyle.None;
                    pb.BackColor = Color.Transparent;
                    this.Controls.Add(pb);
                }
            }

            SetControlStyle(clientThemeIni, this);

            if (this.ParentForm == null)
                return;

            this.Location = new Point(ParentForm.Location.X + (ParentForm.Size.Width - this.Size.Width) / 2,
                ParentForm.Location.Y + (ParentForm.Size.Height - this.Size.Height / 2));
        }

        /// <summary>
        /// Sets the visual style of a control and (recursively) its child controls.
        /// </summary>
        /// <param name="iniFile">The INI file that contains information about the controls' styles.</param>
        /// <param name="control">The control that should be styled.</param>
        private void SetControlStyle(IniFile iniFile, Control control)
        {
            List<string> sections = iniFile.GetSections();

            if (sections.Contains(control.Name))
            {
                List<string> keys = iniFile.GetSectionKeys(control.Name);

                foreach (string key in keys)
                {
                    string keyValue = iniFile.GetStringValue(control.Name, key, String.Empty);

                    if (keyValue == String.Empty)
                        continue;

                    switch (key)
                    {
                        case "Font":
                            control.Font = Utilities.GetFont(keyValue);
                            break;
                        case "ForeColor":
                            control.ForeColor = Utilities.GetColorFromString(keyValue);
                            break;
                        case "BackColor":
                            control.BackColor = Utilities.GetColorFromString(keyValue);
                            break;
                        case "Size":
                            string[] sizeArray = keyValue.Split(',');
                            control.Size = new Size(Convert.ToInt32(sizeArray[0]), Convert.ToInt32(sizeArray[1]));
                            break;
                        case "Location":
                            string[] locationArray = keyValue.Split(',');
                            control.Location = new Point(Convert.ToInt32(locationArray[0]), Convert.ToInt32(locationArray[1]));
                            break;
                        case "Text":
                            control.Text = keyValue.Replace("@", Environment.NewLine);
                            break;
                        case "BorderStyle":
                            BorderStyle bs = BorderStyle.None;

                            if (keyValue == "FixedSingle")
                                bs = BorderStyle.FixedSingle;
                            else if (keyValue == "Fixed3D")
                                bs = BorderStyle.Fixed3D;
                            else if (keyValue == "None")
                                bs = BorderStyle.None;

                            if (control is Panel)
                            {
                                ((Panel)control).BorderStyle = bs;
                            }
                            else if (control is PictureBox)
                            {
                                ((PictureBox)control).BorderStyle = bs;
                            }
                            else if (control is Label)
                            {
                                ((Label)control).BorderStyle = bs;
                                ((Label)control).AutoSize = true;
                            }
                            else
                                throw new InvalidDataException("Invalid BackgroundImage key for control " + control.Name + " (key valid for Panels and PictureBoxes only)");
                            break;
                        case "BackgroundImage":
                            string imagePath = "MainMenu\\" + keyValue;

                            Image image = SharedUILogic.LoadImage(imagePath);

                            if (control is Panel)
                            {
                                ((Panel)control).BackgroundImage = image;
                            }
                            else if (control is PictureBox)
                            {
                                ((PictureBox)control).Image = image;

                                control.Size = image.Size;
                            }
                            else //if (control is Form)
                            {
                                control.BackgroundImage = image;
                            }
                            //else
                                //throw new InvalidDataException("Invalid BackgroundImage key for control " + control.Name + " (key valid for Panels and PictureBoxes only)");
                            break;
                        case "DefaultImage":
                            string imgPath = "MainMenu\\" + keyValue;

                            Image img = SharedUILogic.LoadImage(imgPath);

                            if (control is SwitchingImageButton)
                                ((SwitchingImageButton)control).DefaultImage = img;

                            break;
                        case "HoveredImage":
                            string hImgPath = "MainMenu\\" + keyValue;

                            Image hImg = SharedUILogic.LoadImage(hImgPath);

                            if (control is SwitchingImageButton)
                                ((SwitchingImageButton)control).HoveredImage = hImg;

                            break;
                        case "Visible":
                            control.Visible = Convert.ToBoolean(Convert.ToInt32(keyValue));
                            break;
                    }
                }
            }

            foreach (Control c in control.Controls)
                SetControlStyle(iniFile, c);
        }
    }
}
