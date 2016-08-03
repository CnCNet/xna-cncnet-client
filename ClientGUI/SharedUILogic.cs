/// @author Rampastring
/// http://www.moddb.com/members/rampastring

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using ClientCore;
using Rampastring.Tools;
using Utilities = ClientCore.Utilities;

namespace ClientGUI
{
    /// <summary>
    /// A static class holding UI-related functions useful for both the Skirmish and the CnCNet Game lobby.
    /// </summary>
    public static class SharedUILogic
    {
        public static event Action GameProcessStarted;

        public static event Action GameProcessStarting;

        public static event Action GameProcessExited;

        public const int COOP_BRIEFING_WIDTH = 488;
        const int COOP_BRIEFING_HEIGHT = 200;

        public static Font CoopBriefingFont;

        /// <summary>
        /// Parses and applies various theme-related INI keys from DTACnCNetClient.ini.
        /// Enables editing attributes of individual controls in DTACnCNetClient.ini.
        /// </summary>
        public static void ParseClientThemeIni(Form form)
        {
            IniFile clientThemeIni = DomainController.Instance().DTACnCNetClient_ini;

            List<string> sections = clientThemeIni.GetSections();

            if (sections.Contains(form.Name))
            {
                List<string> keys = clientThemeIni.GetSectionKeys(form.Name);

                foreach (string key in keys)
                {
                    if (key == "Size")
                    {
                        string[] parts = clientThemeIni.GetStringValue(form.Name, key, "10,10").Split(',');

                        int w = Int32.Parse(parts[0]);
                        int h = Int32.Parse(parts[1]);

                        form.Size = new Size(w, h);
                    }
                }
            }

            foreach (string section in sections)
            {
                Control[] controls = form.Controls.Find(section, true);

                if (controls.Length == 0)
                    continue;

                Control control = controls[0];

                List<string> keys = clientThemeIni.GetSectionKeys(section);

                foreach (string key in keys)
                {
                    string keyValue = clientThemeIni.GetStringValue(section, key, String.Empty);

                    switch (key)
                    {
                        case "Font":
                            control.Font = SharedLogic.GetFont(keyValue);
                            break;
                        case "ForeColor":
                            control.ForeColor = GetColorFromString(keyValue);
                            break;
                        case "BackColor":
                            control.BackColor = GetColorFromString(keyValue);
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
                        case "Visible":
                            control.Visible = clientThemeIni.GetBooleanValue(section, key, true);
                            break;
                        case "DefaultImage":
                            if (control is SwitchingImageButton)
                                ((SwitchingImageButton)control).DefaultImage = SharedUILogic.LoadImage(keyValue);
                            break;
                        case "HoveredImage":
                            if (control is SwitchingImageButton)
                                ((SwitchingImageButton)control).HoveredImage = SharedUILogic.LoadImage(keyValue);
                            break;
                        case "BorderStyle":
                            BorderStyle bs = BorderStyle.FixedSingle;
                            if (keyValue.ToUpper() == "NONE")
                                bs = BorderStyle.None;
                            else if (keyValue.ToUpper() == "FIXED3D")
                                bs = BorderStyle.Fixed3D;

                            if (control is Panel)
                                ((Panel)control).BorderStyle = bs;

                            break;
                        case "Anchor":
                            if (keyValue == "Top,Left")
                                control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                            else if (keyValue == "Top,Right")
                                control.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                            else if (keyValue == "Bottom,Right")
                                control.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                            else if (keyValue == "Bottom,Left")
                                control.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                            else if (keyValue == "Top,Bottom,Left,Right")
                                control.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                            break;
                        case "BackgroundImage":
                            control.BackgroundImage = LoadImage(keyValue);
                            control.Size = control.BackgroundImage.Size;
                            break;
                    }
                }
            }
        }

        public static void InitForm(MovableForm form, IniFile iniFile)
        {
            SetControlStyle(iniFile, form);
        }

        /// <summary>
        /// Sets the visual style of a control and (recursively) its child controls.
        /// </summary>
        /// <param name="iniFile">The INI file that contains information about the controls' styles.</param>
        /// <param name="control">The control that should be styled.</param>
        public static void SetControlStyle(IniFile iniFile, Control control)
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
                                throw new InvalidDataException("Invalid BackgroundImage key for control " + control.Name + " (key valid for Panels, Labels and PictureBoxes only)");
                            break;
                        case "BackgroundImage":
                            string imagePath = keyValue;

                            Image image = SharedUILogic.LoadImage(imagePath);

                            if (control is PictureBox)
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
                        case "RepeatingImage":
                            if (iniFile.GetBooleanValue(control.Name, "RepeatingImage", true))
                            {
                                control.BackgroundImage = ((PictureBox)control).Image;
                                ((PictureBox)control).Image = null;
                            }
                            //    SetImageRepeating((PictureBox)control, ((PictureBox)control).Image);
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
                            control.Visible = iniFile.GetBooleanValue(control.Name, key, false);
                            break;
                        case "FormBorderStyle":
                            if (control is Form)
                            {
                                FormBorderStyle fbs = FormBorderStyle.Sizable;
                                switch (keyValue)
                                {
                                    case "None":
                                        fbs = FormBorderStyle.None;
                                        break;
                                    case "SizableToolWindow":
                                        fbs = FormBorderStyle.SizableToolWindow;
                                        break;
                                    case "Fixed3D":
                                        fbs = FormBorderStyle.Fixed3D;
                                        break;
                                    case "FixedSingle":
                                        fbs = FormBorderStyle.FixedSingle;
                                        break;
                                    case "FixedDialog":
                                        fbs = FormBorderStyle.FixedDialog;
                                        break;
                                    case "FixedToolWindow":
                                        fbs = FormBorderStyle.FixedToolWindow;
                                        break;
                                    case "Sizable":
                                    default:
                                        break;
                                }

                                ((Form)control).FormBorderStyle = fbs;
                            }
                            else
                                Logger.Log("SetControlStyle: Control " + control.Name + " isn't a form - FormBorderStyle doesn't apply!");
                            break;
                        case "DistanceFromRightBorder":
                            int distance = iniFile.GetIntValue(control.Name, key, 50);
                            control.Location = new Point(control.Parent.Size.Width - distance - control.Width, control.Location.Y);
                            break;
                        case "DistanceFromBottomBorder":
                            int bDistance = iniFile.GetIntValue(control.Name, key, 50);
                            control.Location = new Point(control.Location.X, control.Parent.Size.Height - bDistance - control.Height);
                            break;
                        case "FillHeight":
                            control.Size = new Size(control.Size.Width, control.Parent.Size.Height - iniFile.GetIntValue(control.Name, "FillHeight", 0));
                            break;
                        case "FillWidth":
                            control.Size = new Size(control.Parent.Size.Width - iniFile.GetIntValue(control.Name, "FillWidth", 0), control.Size.Height);
                            break;
                        case "Anchor":
                            switch (keyValue)
                            {
                                case "Top":
                                    control.Anchor = AnchorStyles.Top;
                                    break;
                                case "Bottom":
                                    control.Anchor = AnchorStyles.Bottom;
                                    break;
                                case "Top,Left":
                                    control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                                    break;
                                case "Top,Right":
                                    control.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                                    break;
                                case "Bottom,Left":
                                    control.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                                    break;
                                case "Bottom,Right":
                                    control.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                                    break;
                                case "Top,Left,Right":
                                case "Top,Right,Left":
                                    control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                                    break;
                                case "Bottom,Left,Right":
                                case "Bottom,Right,Left":
                                    control.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                                    break;
                                case "All":
                                case "Top,Bottom,Left,Right":
                                case "Top,Bottom,Right,Left":
                                case "Bottom,Top,Left,Right":
                                case "Bottom,Top,Right,Left":
                                    control.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                                    break;
                            }
                            break;
                    }
                }
            }

            foreach (Control c in control.Controls)
                SetControlStyle(iniFile, c);
        }

        /// <summary>
        /// Makes an image repeat in a picturebox.
        /// </summary>
        /// <param name="pb">The picturebox.</param>
        /// <param name="image">The image.</param>
        private static void SetImageRepeating(PictureBox pb, Image image)
        {
            Bitmap bm = new Bitmap(pb.Width, pb.Height);
            Graphics gp = Graphics.FromImage(bm);
            gp.DrawImage(image, new Point(0, 0));
            //if (pb.Image.Width < pb.Width)
            //{
            //    for (int x = image.Width; x <= bm.Width - image.Width; x += image.Width)
            //    {
            //        gp.DrawImage(image, new Point(x, 0));
            //    }
            //}
            //if (pb.Image.Height < pb.Height)
            //{
            //    for (int y = image.Height; y <= bm.Height - image.Height; y += image.Height)
            //    {
            //        gp.DrawImage(image, new Point(0, y));
            //    }
            //}
            pb.Image = bm;
        }

        /// <summary>
        /// Gets a color from a RGB color string (example: 255,255,255)
        /// </summary>
        /// <param name="colorString">The color string.</param>
        /// <returns>The color.</returns>
        public static Color GetColorFromString(string colorString)
        {
            string[] colorArray = colorString.Split(',');
            Color color = Color.FromArgb(Convert.ToByte(colorArray[0]), Convert.ToByte(colorArray[1]), Convert.ToByte(colorArray[2]));
            return color;
        }

        /// <summary>
        /// Starts the main game process.
        /// </summary>
        /// <param name="processId">The index of the game process to start (for RA2 support;
        /// GameOptions.ini -> GameExecutableNames= allows multiple names).</param>
        public static void StartGameProcess(int processId)
        {
            string gameExecutableName = DomainController.Instance().GetGameExecutableName(processId);

            string extraCommandLine = DomainController.Instance().GetExtraCommandLineParameters();

            File.Delete(ProgramConstants.GamePath + "DTA.LOG");

            GameProcessStarting?.Invoke();

            if (UserINISettings.Instance.WindowedMode)
            {
                Logger.Log("Windowed mode is enabled - using QRes.");
                Process QResProcess = new Process();
                QResProcess.StartInfo.FileName = ProgramConstants.QRES_EXECUTABLE;
                QResProcess.StartInfo.UseShellExecute = false;
                if (!string.IsNullOrEmpty(extraCommandLine))
                    QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + ProgramConstants.GamePath + gameExecutableName + "\" " + extraCommandLine + " -SPAWN";
                else
                    QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + ProgramConstants.GamePath + gameExecutableName + "\" " + "-SPAWN";
                QResProcess.EnableRaisingEvents = true;
                QResProcess.Exited += new EventHandler(Process_Exited);
                try
                {
                    QResProcess.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log("Error launching QRes: " + ex.Message);
                    MessageBox.Show("Error launching " + ProgramConstants.QRES_EXECUTABLE + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message,
                        "Error launching game", MessageBoxButtons.OK);
                    Process_Exited(QResProcess, EventArgs.Empty);
                    return;
                }

                if (Environment.ProcessorCount > 1)
                    QResProcess.ProcessorAffinity = (IntPtr)2;
            }
            else
            {
                Process DtaProcess = new Process();
                DtaProcess.StartInfo.FileName = gameExecutableName;
                DtaProcess.StartInfo.UseShellExecute = false;
                if (!string.IsNullOrEmpty(extraCommandLine))
                    DtaProcess.StartInfo.Arguments = " " + extraCommandLine + " -SPAWN";
                else
                    DtaProcess.StartInfo.Arguments = "-SPAWN";
                DtaProcess.EnableRaisingEvents = true;
                DtaProcess.Exited += new EventHandler(Process_Exited);
                try
                {
                    DtaProcess.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log("Error launching " + gameExecutableName + ": " + ex.Message);
                    MessageBox.Show("Error launching " + gameExecutableName + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." + 
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message,
                        "Error launching game", MessageBoxButtons.OK);
                    Process_Exited(DtaProcess, EventArgs.Empty);
                    return;
                }

                if (Environment.ProcessorCount > 1)
                    DtaProcess.ProcessorAffinity = (IntPtr)2;
            }

            GameProcessStarted?.Invoke();

            Logger.Log("Waiting for qres.dat or " + gameExecutableName + " to exit.");
        }

        static void Process_Exited(object sender, EventArgs e)
        {
            Process proc = (Process)sender;
            proc.Exited -= Process_Exited;
            proc.Dispose();
            if (GameProcessExited != null)
                GameProcessExited();
        }

        /// <summary>
        /// Loads icons used for displaying sides in the game lobby.
        /// </summary>
        /// <returns>An array of side images.</returns>
        public static Image[] LoadSideImages()
        {
            string[] sides = DomainController.Instance().GetSides().Split(',');
            Image[] returnValue = new Image[sides.Length + 2];

            returnValue[0] = Image.FromFile(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "randomicon.png");

            for (int i = 1; i <= sides.Length; i++)
            {
                returnValue[i] = Image.FromFile(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "" + sides[i - 1] + "icon.png");
            }

            returnValue[sides.Length + 1] = Image.FromFile(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "spectatoricon.png");

            return returnValue;
        }

        /// <summary>
        /// Loads starting location indicator icons for the game lobby.
        /// </summary>
        /// <returns>An array of starting location indicator images.</returns>
        public static Image[] LoadStartingLocationIndicators()
        {
            Image[] startingLocationIndicators = new Image[8];
            startingLocationIndicators[0] = SharedUILogic.LoadImage("slocindicator1.png");
            startingLocationIndicators[1] = SharedUILogic.LoadImage("slocindicator2.png");
            startingLocationIndicators[2] = SharedUILogic.LoadImage("slocindicator3.png");
            startingLocationIndicators[3] = SharedUILogic.LoadImage("slocindicator4.png");
            startingLocationIndicators[4] = SharedUILogic.LoadImage("slocindicator5.png");
            startingLocationIndicators[5] = SharedUILogic.LoadImage("slocindicator6.png");
            startingLocationIndicators[6] = SharedUILogic.LoadImage("slocindicator7.png");
            startingLocationIndicators[7] = SharedUILogic.LoadImage("slocindicator8.png");

            return startingLocationIndicators;
        }

        /// <summary>
        /// Sets the background image layout of a form based on the client's settings.
        /// </summary>
        /// <param name="form">The form.</param>
        public static void SetBackgroundImageLayout(Form form)
        {
            string backgroundImageLayout = DomainController.Instance().GetGameLobbyBackgroundImageLayout();
            switch (backgroundImageLayout)
            {
                case "Center":
                    form.BackgroundImageLayout = ImageLayout.Center;
                    break;
                case "Stretch":
                    form.BackgroundImageLayout = ImageLayout.Stretch;
                    break;
                case "Zoom":
                    form.BackgroundImageLayout = ImageLayout.Zoom;
                    break;
                default:
                case "Tile":
                    form.BackgroundImageLayout = ImageLayout.Tile;
                    break;
            }
        }

        /// <summary>
        /// Sets the colors of a specific control and (recursively) all of its child controls.
        /// </summary>
        /// <param name="cLabelColor">The color of labels in the UI.</param>
        /// <param name="cBackColor">The background color of list boxes and combo boxes in the UI.</param>
        /// <param name="cAltUiColor">The foreground color of list boxes, buttons and combo boxes in the UI.</param>
        /// <param name="cListBoxFocusColor">The background color of highlighted list box and combo box items.</param>
        /// <param name="control">The control. Usually you'll want to have a form in this parameter.</param>
        public static void SetControlColor(Color cLabelColor, Color cBackColor, Color cAltUiColor,
            Color cListBoxFocusColor, Control control)
        {
            SetControlColors(cLabelColor, cBackColor, cAltUiColor, cListBoxFocusColor, control);

            foreach (Control child in control.Controls)
                SetControlColor(cLabelColor, cBackColor, cAltUiColor, cListBoxFocusColor, child);
        }

        /// <summary>
        /// Sets the colors of a single control.
        /// </summary>
        /// <param name="cLabelColor">The color of labels in the UI.</param>
        /// <param name="cBackColor">The background color of list boxes and combo boxes in the UI.</param>
        /// <param name="cAltUiColor">The foreground color of list boxes, buttons and combo boxes in the UI.</param>
        /// <param name="cListBoxFocusColor">The background color of highlighted list box and combo box items.</param>
        /// <param name="control">The control.</param>
        private static void SetControlColors(Color cLabelColor, Color cBackColor, Color cAltUiColor,
            Color cListBoxFocusColor, Control control)
        {
            if (control is Button || control is TextBox)
            {
                control.ForeColor = cAltUiColor;
                control.BackColor = cBackColor;
            }
            else if (control is Label)
            {
                control.ForeColor = cLabelColor;
            }
        }

        public static Image LoadImage(string resourceName)
        {
            if (File.Exists(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + resourceName))
                return Image.FromStream(new MemoryStream(File.ReadAllBytes(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + resourceName)));
            else if (File.Exists(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "" + resourceName))
                return Image.FromStream(new MemoryStream(File.ReadAllBytes(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "" + resourceName)));

            return Properties.Resources.hotbutton;
        }
    }
}
