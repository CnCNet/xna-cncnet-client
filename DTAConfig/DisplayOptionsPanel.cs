using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ClientCore;
using Rampastring.Tools;
using ClientGUI;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

namespace DTAConfig
{
    class DisplayOptionsPanel : XNAOptionsPanel
    {
        public DisplayOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        XNAClientDropDown ddIngameResolution;
        XNAClientDropDown ddDetailLevel;
        XNAClientDropDown ddRenderer;
        XNAClientCheckBox chkWindowedMode;
        XNAClientCheckBox chkBorderlessWindowedMode;
        XNAClientCheckBox chkBackBufferInVRAM;
        XNAClientDropDown ddClientResolution;
        XNAClientCheckBox chkBorderlessClient;
        XNAClientDropDown ddClientTheme;

        List<FileSettingCheckBox> fileSettingCheckBoxes = new List<FileSettingCheckBox>();

#if !YR
        XNALabel lblCompatibilityFixes;
        XNALabel lblGameCompatibilityFix;
        XNALabel lblMapEditorCompatibilityFix;
        XNAClientButton btnGameCompatibilityFix;
        XNAClientButton btnMapEditorCompatibilityFix;

        private bool GameCompatFixInstalled = false;
        private bool FinalSunCompatFixInstalled = false;
        private bool GameCompatFixDeclined = false;
        //private bool FinalSunCompatFixDeclined = false;
#endif

#if DTA
        FileSettingCheckBox chkEnableCannonTracers;
#elif TI
        FileSettingCheckBox chkLargerInfantryGraphics;
        FileSettingCheckBox chkSmallerVehicleGraphics;
#endif



        public override void Initialize()
        {
            base.Initialize();

            Name = "DisplayOptionsPanel";

            var lblIngameResolution = new XNALabel(WindowManager);
            lblIngameResolution.Name = "lblIngameResolution";
            lblIngameResolution.ClientRectangle = new Rectangle(12, 14, 0, 0);
            lblIngameResolution.Text = "In-game Resolution:";

            ddIngameResolution = new XNAClientDropDown(WindowManager);
            ddIngameResolution.Name = "ddIngameResolution";
            ddIngameResolution.ClientRectangle = new Rectangle(
                lblIngameResolution.ClientRectangle.Right + 12,
                lblIngameResolution.ClientRectangle.Y - 2, 120, 19);

#if TI
            var resolutions = ScreenResolutionOperations.GetScreenResolutions(800, 600, 4096, 4096, 32, false);
#else
            var resolutions = ScreenResolutionOperations.GetScreenResolutions(640, 480, 4096, 4096, 32, false);
#endif

            foreach (var res in resolutions)
                ddIngameResolution.AddItem(res);

            var  lblDetailLevel = new XNALabel(WindowManager);
            lblDetailLevel.Name = "lblDetailLevel";
            lblDetailLevel.ClientRectangle = new Rectangle(lblIngameResolution.ClientRectangle.X,
                ddIngameResolution.ClientRectangle.Bottom + 16, 0, 0);
            lblDetailLevel.Text = "Detail Level:";

            ddDetailLevel = new XNAClientDropDown(WindowManager);
            ddDetailLevel.Name = "ddDetailLevel";
            ddDetailLevel.ClientRectangle = new Rectangle(
                ddIngameResolution.ClientRectangle.X,
                lblDetailLevel.ClientRectangle.Y - 2,
                ddIngameResolution.ClientRectangle.Width, 
                ddIngameResolution.ClientRectangle.Height);
            ddDetailLevel.AddItem("Low");
            ddDetailLevel.AddItem("Medium");
            ddDetailLevel.AddItem("High");

            var  lblRenderer = new XNALabel(WindowManager);
            lblRenderer.Name = "lblRenderer";
            lblRenderer.ClientRectangle = new Rectangle(lblDetailLevel.ClientRectangle.X,
                ddDetailLevel.ClientRectangle.Bottom + 16, 0, 0);
            lblRenderer.Text = "Renderer:";

            ddRenderer = new XNAClientDropDown(WindowManager);
            ddRenderer.Name = "ddRenderer";
            ddRenderer.ClientRectangle = new Rectangle(
                ddDetailLevel.ClientRectangle.X,
                lblRenderer.ClientRectangle.Y - 2,
                ddDetailLevel.ClientRectangle.Width,
                ddDetailLevel.ClientRectangle.Height);

            ddRenderer.AddItem("Default");
            ddRenderer.AddItem("IE-DDRAW");
            ddRenderer.AddItem("TS-DDRAW");
            ddRenderer.AddItem("DDWrapper");
            ddRenderer.AddItem("DxWnd");
            if (ClientConfiguration.Instance.GetOperatingSystemVersion() == OSVersion.WINXP)
                ddRenderer.AddItem("Software");

            chkWindowedMode = new XNAClientCheckBox(WindowManager);
            chkWindowedMode.Name = "chkWindowedMode";
            chkWindowedMode.ClientRectangle = new Rectangle(lblDetailLevel.ClientRectangle.X,
                ddRenderer.ClientRectangle.Bottom + 16, 0, 0);
            chkWindowedMode.Text = "Windowed Mode";
            chkWindowedMode.CheckedChanged += ChkWindowedMode_CheckedChanged;

            chkBorderlessWindowedMode = new XNAClientCheckBox(WindowManager);
            chkBorderlessWindowedMode.Name = "chkBorderlessWindowedMode";
            chkBorderlessWindowedMode.ClientRectangle = new Rectangle(
                chkWindowedMode.ClientRectangle.X + 50,
                chkWindowedMode.ClientRectangle.Bottom + 24, 0, 0);
            chkBorderlessWindowedMode.Text = "Borderless Windowed Mode";
            chkBorderlessWindowedMode.AllowChecking = false;

            chkBackBufferInVRAM = new XNAClientCheckBox(WindowManager);
            chkBackBufferInVRAM.Name = "chkBackBufferInVRAM";
            chkBackBufferInVRAM.ClientRectangle = new Rectangle(
                lblDetailLevel.ClientRectangle.X,
                chkBorderlessWindowedMode.ClientRectangle.Bottom + 28, 0, 0);
            chkBackBufferInVRAM.Text = "Back Buffer in Video Memory" + Environment.NewLine +
                "(lower performance, but is" + Environment.NewLine + "necessary on some systems)";

            var  lblClientResolution = new XNALabel(WindowManager);
            lblClientResolution.Name = "lblClientResolution";
            lblClientResolution.ClientRectangle = new Rectangle(
                285, 14, 0, 0);
            lblClientResolution.Text = "Client Resolution:";

            ddClientResolution = new XNAClientDropDown(WindowManager);
            ddClientResolution.Name = "ddClientResolution";
            ddClientResolution.ClientRectangle = new Rectangle(
                lblClientResolution.ClientRectangle.Right + 12,
                lblClientResolution.ClientRectangle.Y - 2,
                ClientRectangle.Width - (lblClientResolution.ClientRectangle.Right + 24),
                ddIngameResolution.ClientRectangle.Height);
            ddClientResolution.AllowDropDown = false;

            var screenBounds = Screen.PrimaryScreen.Bounds;

            resolutions = ScreenResolutionOperations.GetScreenResolutions(800, 600,
                screenBounds.Width, screenBounds.Height, 32, true);

            foreach (var res in resolutions)
            {
                var item = new XNADropDownItem();
                item.Text = res;
                item.Tag = res;
                item.TextColor = UISettings.AltColor;
                ddClientResolution.AddItem(item);
            }

            int optimalWindowedResIndex = resolutions.FindIndex(res => res == "1280x800");
            if (optimalWindowedResIndex == -1)
                optimalWindowedResIndex = resolutions.FindIndex(res => res == "1280x768");

            if (optimalWindowedResIndex > -1)
            {
                var item = ddClientResolution.Items[optimalWindowedResIndex];
                item.Text = item.Text + " (recommended)";
            }

            chkBorderlessClient = new XNAClientCheckBox(WindowManager);
            chkBorderlessClient.Name = "chkBorderlessClient";
            chkBorderlessClient.ClientRectangle = new Rectangle(
                lblClientResolution.ClientRectangle.X,
                lblDetailLevel.ClientRectangle.Y, 0, 0);
            chkBorderlessClient.Text = "Fullscreen Client";
            chkBorderlessClient.CheckedChanged += ChkBorderlessMenu_CheckedChanged;
            chkBorderlessClient.Checked = true;

            var lblClientTheme = new XNALabel(WindowManager);
            lblClientTheme.Name = "lblClientTheme";
            lblClientTheme.ClientRectangle = new Rectangle(
                lblClientResolution.ClientRectangle.X,
                lblRenderer.ClientRectangle.Y, 0, 0);
            lblClientTheme.Text = "Client Theme:";

            ddClientTheme = new XNAClientDropDown(WindowManager);
            ddClientTheme.Name = "ddClientTheme";
            ddClientTheme.ClientRectangle = new Rectangle(
                ddClientResolution.ClientRectangle.X,
                ddRenderer.ClientRectangle.Y,
                ddClientResolution.ClientRectangle.Width,
                ddRenderer.ClientRectangle.Height);

            int themeCount = ClientConfiguration.Instance.ThemeCount;

            for (int i = 0; i < themeCount; i++)
                ddClientTheme.AddItem(ClientConfiguration.Instance.GetThemeInfoFromIndex(i)[0]);

#if DTA
            chkEnableCannonTracers = new FileSettingCheckBox(WindowManager,
                "Resources\\ECache91.mix", "MIX\\ECache91.mix", true);
            chkEnableCannonTracers.Name = "chkEnableCannonTracers";
            chkEnableCannonTracers.ClientRectangle = new Rectangle(
                chkBorderlessClient.ClientRectangle.X,
                chkWindowedMode.ClientRectangle.Y, 0, 0);
            chkEnableCannonTracers.Text = "Use Cannon Tracers";

            AddChild(chkEnableCannonTracers);

            fileSettingCheckBoxes.Add(chkEnableCannonTracers);
#elif TI
            chkSmallerVehicleGraphics = new FileSettingCheckBox(WindowManager,
                "Resources\\ecache02.mix", "MIX\\ecache02.mix", false);
            chkSmallerVehicleGraphics.AddFile("Resources\\expand02.mix", "MIX\\expand02.mix");
            chkSmallerVehicleGraphics.Name = "chkSmallerVehicleGraphics";
            chkSmallerVehicleGraphics.ClientRectangle = new Rectangle(
                chkBorderlessClient.ClientRectangle.X,
                chkWindowedMode.ClientRectangle.Y, 0, 0);
            chkSmallerVehicleGraphics.Text = "Smaller Vehicle Graphics";

            AddChild(chkSmallerVehicleGraphics);
            fileSettingCheckBoxes.Add(chkSmallerVehicleGraphics);

            chkLargerInfantryGraphics = new FileSettingCheckBox(WindowManager,
                "Resources\\ecache01.mix", "MIX\\ecache01.mix", false);
            chkLargerInfantryGraphics.Name = "chkLargerInfantryGraphics";
            chkLargerInfantryGraphics.ClientRectangle = new Rectangle(
                chkSmallerVehicleGraphics.ClientRectangle.X,
                chkBorderlessWindowedMode.ClientRectangle.Y, 0, 0);
            chkLargerInfantryGraphics.Text = "Larger Infantry Graphics";

            AddChild(chkLargerInfantryGraphics);
            fileSettingCheckBoxes.Add(chkLargerInfantryGraphics);
#endif

#if !YR
            lblCompatibilityFixes = new XNALabel(WindowManager);
            lblCompatibilityFixes.Name = "lblCompatibilityFixes";
            lblCompatibilityFixes.FontIndex = 1;
            lblCompatibilityFixes.Text = "Compatibility Fixes (advanced):";
            AddChild(lblCompatibilityFixes);
            lblCompatibilityFixes.CenterOnParent();
            lblCompatibilityFixes.ClientRectangle = new Rectangle(
                lblCompatibilityFixes.ClientRectangle.X,
                ClientRectangle.Height - 103,
                lblCompatibilityFixes.ClientRectangle.Width,
                lblCompatibilityFixes.ClientRectangle.Height);

            lblGameCompatibilityFix = new XNALabel(WindowManager);
            lblGameCompatibilityFix.Name = "lblGameCompatibilityFix";
            lblGameCompatibilityFix.ClientRectangle = new Rectangle(132, 
                lblCompatibilityFixes.ClientRectangle.Bottom + 20, 0, 0);
            lblGameCompatibilityFix.Text = "DTA/TI/TS Compatibility Fix:";

            btnGameCompatibilityFix = new XNAClientButton(WindowManager);
            btnGameCompatibilityFix.Name = "btnGameCompatibilityFix";
            btnGameCompatibilityFix.ClientRectangle = new Rectangle(
                lblGameCompatibilityFix.ClientRectangle.Right + 20,
                lblGameCompatibilityFix.ClientRectangle.Y - 4, 133, 23);
            btnGameCompatibilityFix.FontIndex = 1;
            btnGameCompatibilityFix.Text = "Enable";
            btnGameCompatibilityFix.LeftClick += BtnGameCompatibilityFix_LeftClick;

            lblMapEditorCompatibilityFix = new XNALabel(WindowManager);
            lblMapEditorCompatibilityFix.Name = "lblMapEditorCompatibilityFix";
            lblMapEditorCompatibilityFix.ClientRectangle = new Rectangle(
                lblGameCompatibilityFix.ClientRectangle.X,
                lblGameCompatibilityFix.ClientRectangle.Bottom + 20, 0, 0);
            lblMapEditorCompatibilityFix.Text = "FinalSun Compatibility Fix:";

            btnMapEditorCompatibilityFix = new XNAClientButton(WindowManager);
            btnMapEditorCompatibilityFix.Name = "btnMapEditorCompatibilityFix";
            btnMapEditorCompatibilityFix.ClientRectangle = new Rectangle(
                btnGameCompatibilityFix.ClientRectangle.X,
                lblMapEditorCompatibilityFix.ClientRectangle.Y - 4,
                btnGameCompatibilityFix.ClientRectangle.Width,
                btnGameCompatibilityFix.ClientRectangle.Height);
            btnMapEditorCompatibilityFix.FontIndex = 1;
            btnMapEditorCompatibilityFix.Text = "Enable";
            btnMapEditorCompatibilityFix.LeftClick += BtnMapEditorCompatibilityFix_LeftClick;

            AddChild(lblGameCompatibilityFix);
            AddChild(btnGameCompatibilityFix);
            AddChild(lblMapEditorCompatibilityFix);
            AddChild(btnMapEditorCompatibilityFix);
#endif

            AddChild(chkWindowedMode);
            AddChild(chkBorderlessWindowedMode);
            AddChild(chkBackBufferInVRAM);
            AddChild(chkBorderlessClient);
            AddChild(lblClientTheme);
            AddChild(ddClientTheme);
            AddChild(lblClientResolution);
            AddChild(ddClientResolution);
            AddChild(lblRenderer);
            AddChild(ddRenderer);
            AddChild(lblDetailLevel);
            AddChild(ddDetailLevel);
            AddChild(lblIngameResolution);
            AddChild(ddIngameResolution);
        }

#if !YR

        /// <summary>
        /// Asks the user whether they want to install the DTA/TI/TS compatibility fix.
        /// </summary>
        public void PostInit()
        {
            Load();

            if (!GameCompatFixInstalled && !GameCompatFixDeclined)
            {
                string defaultGame = ClientConfiguration.Instance.LocalGame;

                var messageBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "New Compatibility Fix",
                    "A performance-enhancing compatibility fix for Windows 8 and 10" + Environment.NewLine +
                    "has been included in this version of " + defaultGame + ". Enabling it requires" + Environment.NewLine +
                    "administrative priveleges. Would you like to install the compatibility fix?" + Environment.NewLine + Environment.NewLine + 
                    "You'll always be able to install or uninstall the compatibility fix later from the options menu.");
                messageBox.YesClicked += MessageBox_YesClicked;
                messageBox.NoClicked += MessageBox_NoClicked;
            }
        }

        private void MessageBox_NoClicked(object sender, EventArgs e)
        {
            var messageBox = (XNAMessageBox)sender;
            messageBox.YesClicked -= MessageBox_YesClicked;
            messageBox.NoClicked -= MessageBox_NoClicked;

            // Set compatibility fix declined flag in registry
            try
            {
                RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Tiberian Sun Client");

                try
                {
                    regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                    regKey = regKey.CreateSubKey("Tiberian Sun Client");
                    regKey.SetValue("TSCompatFixDeclined", "Yes");
                }
                catch (Exception ex)
                {
                    Logger.Log("Setting TSCompatFixDeclined failed! Returned error: " + ex.Message);
                }
            }
            catch { }
        }

        private void MessageBox_YesClicked(object sender, EventArgs e)
        {
            var messageBox = (XNAMessageBox)sender;
            messageBox.YesClicked -= MessageBox_YesClicked;
            messageBox.NoClicked -= MessageBox_NoClicked;
            BtnGameCompatibilityFix_LeftClick(sender, EventArgs.Empty);
        }

        private void BtnGameCompatibilityFix_LeftClick(object sender, EventArgs e)
        {
            if (GameCompatFixInstalled)
            {
                try
                {
                    Process sdbinst = Process.Start("sdbinst.exe", "-q -n \"TS Compatibility Fix\"");

                    sdbinst.WaitForExit();

                    Logger.Log("DTA/TI/TS Compatibility Fix succesfully uninstalled.");
                    XNAMessageBox.Show(WindowManager, "Compatibility Fix Uninstalled",
                        "The DTA/TI/TS Compatibility Fix has been succesfully uninstalled.");

                    RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                    regKey = regKey.CreateSubKey("Tiberian Sun Client");
                    regKey.SetValue("TSCompatFixInstalled", "No");

                    btnGameCompatibilityFix.Text = "Enable";

                    GameCompatFixInstalled = false;
                }
                catch (Exception ex)
                {
                    Logger.Log("Uninstalling DTA/TI/TS Compatibility Fix failed. Error message: " + ex.Message);
                    XNAMessageBox.Show(WindowManager, "Uninstalling Compatibility Fix Failed",
                        "Uninstalling DTA/TI/TS Compatibility Fix failed. Returned error: " + ex.Message);
                }

                return;
            }

            try
            {
                Process sdbinst = Process.Start("sdbinst.exe", "-q \"" + ProgramConstants.GamePath + "Resources\\compatfix.sdb\"");

                sdbinst.WaitForExit();

                Logger.Log("DTA/TI/TS Compatibility Fix succesfully installed.");
                XNAMessageBox.Show(WindowManager, "Compatibility Fix Installed",
                    "The DTA/TI/TS Compatibility Fix has been succesfully installed.");

                RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                regKey = regKey.CreateSubKey("Tiberian Sun Client");
                regKey.SetValue("TSCompatFixInstalled", "Yes");

                btnGameCompatibilityFix.Text = "Disable";

                GameCompatFixInstalled = true;
            }
            catch (Exception ex)
            {
                Logger.Log("Installing DTA/TI/TS Compatibility Fix failed. Error message: " + ex.Message);
                XNAMessageBox.Show(WindowManager, "Installing Compatibility Fix Failed",
                    "Installing DTA/TI/TS Compatibility Fix failed. Error message: " + ex.Message);
            }
        }

        private void BtnMapEditorCompatibilityFix_LeftClick(object sender, EventArgs e)
        {
            if (FinalSunCompatFixInstalled)
            {
                try
                {
                    Process sdbinst = Process.Start("sdbinst.exe", "-q -n \"Final Sun Compatibility Fix\"");

                    sdbinst.WaitForExit();

                    RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                    regKey = regKey.CreateSubKey("Tiberian Sun Client");
                    regKey.SetValue("FSCompatFixInstalled", "No");

                    btnMapEditorCompatibilityFix.Text = "Enable";

                    Logger.Log("FinalSun Compatibility Fix succesfully uninstalled.");
                    XNAMessageBox.Show(WindowManager, "Compatibility Fix Uninstalled",
                        "The FinalSun Compatibility Fix has been succesfully uninstalled.");

                    FinalSunCompatFixInstalled = false;
                }
                catch (Exception ex)
                {
                    Logger.Log("Uninstalling FinalSun Compatibility Fix failed. Error message: " + ex.Message);
                    XNAMessageBox.Show(WindowManager, "Uninstalling Compatibility Fix Failed",
                        "Uninstalling FinalSun Compatibility Fix failed. Error message: " + ex.Message);
                }

                return;
            }


            try
            {
                Process sdbinst = Process.Start("sdbinst.exe", "-q \"" + ProgramConstants.GamePath + "Resources\\FSCompatFix.sdb\"");

                sdbinst.WaitForExit();

                RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                regKey = regKey.CreateSubKey("Tiberian Sun Client");
                regKey.SetValue("FSCompatFixInstalled", "Yes");

                btnMapEditorCompatibilityFix.Text = "Disable";

                Logger.Log("FinalSun Compatibility Fix succesfully installed.");
                XNAMessageBox.Show(WindowManager, "Compatibility Fix Installed",
                    "The FinalSun Compatibility Fix has been succesfully installed.");

                FinalSunCompatFixInstalled = true;
            }
            catch (Exception ex)
            {
                Logger.Log("Installing FinalSun Compatibility Fix failed. Error message: " + ex.Message);
                XNAMessageBox.Show(WindowManager, "Installing Compatibility Fix Failed",
                    "Installing FinalSun Compatibility Fix failed. Error message: " + ex.Message);
            }
        }

#endif

        private void ChkBorderlessMenu_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBorderlessClient.Checked)
            {
                ddClientResolution.AllowDropDown = false;
                string nativeRes = Screen.PrimaryScreen.Bounds.Width +
                    "x" + Screen.PrimaryScreen.Bounds.Height;

                int nativeResIndex = ddClientResolution.Items.FindIndex(i => (string)i.Tag == nativeRes);
                if (nativeResIndex > -1)
                    ddClientResolution.SelectedIndex = nativeResIndex;
            }
            else
            {
                ddClientResolution.AllowDropDown = true;

                int optimalWindowedResIndex = ddClientResolution.Items.FindIndex(i => (string)i.Tag == "1280x800");

                if (optimalWindowedResIndex == -1)
                    optimalWindowedResIndex = ddClientResolution.Items.FindIndex(i => (string)i.Tag == "1280x768");

                if (optimalWindowedResIndex > -1)
                {
                    ddClientResolution.SelectedIndex = optimalWindowedResIndex;
                }
            }
        }

        private void ChkWindowedMode_CheckedChanged(object sender, EventArgs e)
        {
            if (chkWindowedMode.Checked)
            {
                chkBorderlessWindowedMode.AllowChecking = true;
                return;
            }

            chkBorderlessWindowedMode.AllowChecking = false;
            chkBorderlessWindowedMode.Checked = false;
        }

        public override void Load()
        {
            GetRenderer();
            ddDetailLevel.SelectedIndex = UserINISettings.Instance.DetailLevel;

            string currentRes = UserINISettings.Instance.IngameScreenWidth.Value +
                "x" + UserINISettings.Instance.IngameScreenHeight.Value;

            int index = ddIngameResolution.Items.FindIndex(i => i.Text == currentRes);

            ddIngameResolution.SelectedIndex = index > -1 ? index : 0;

            if (ddRenderer.SelectedItem.Text != "DxWnd")
            {
                chkWindowedMode.Checked = UserINISettings.Instance.WindowedMode;
                chkBorderlessWindowedMode.Checked = UserINISettings.Instance.BorderlessWindowedMode;
            }
            else
            {
                // DxWnd needs to have the game's integrated windowed mode disabled
                // Instead it has its own controls in its INI file
                IniFile dxWndIni = new IniFile(ProgramConstants.GamePath + "dxwnd.ini");
                chkWindowedMode.Checked = dxWndIni.GetBooleanValue("DxWnd", "RunInWindow", false);
                chkBorderlessWindowedMode.Checked = dxWndIni.GetBooleanValue("DxWnd", "NoWindowFrame", false);
            }

            string currentClientRes = WindowManager.ResolutionWidth + "x" + WindowManager.ResolutionHeight;

            int clientResIndex = ddClientResolution.Items.FindIndex(i => (string)i.Tag == currentClientRes);

            ddClientResolution.SelectedIndex = clientResIndex > -1 ? clientResIndex : 0;

            chkBorderlessClient.Checked = UserINISettings.Instance.BorderlessWindowedClient;

            ddClientTheme.SelectedIndex = UserINISettings.Instance.ClientTheme;

            fileSettingCheckBoxes.ForEach(chkBox => chkBox.Load());

#if YR
            chkBackBufferInVRAM.Checked = UserINISettings.Instance.BackBufferInVRAM;
#else
            chkBackBufferInVRAM.Checked = !UserINISettings.Instance.BackBufferInVRAM;

            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Tiberian Sun Client");

            if (regKey == null)
                return;

            object tsCompatFixValue = regKey.GetValue("TSCompatFixInstalled", "No");
            string tsCompatFixString = (string)tsCompatFixValue;

            if (tsCompatFixString == "Yes")
            {
                GameCompatFixInstalled = true;
                btnGameCompatibilityFix.Text = "Disable";
            }

            object fsCompatFixValue = regKey.GetValue("FSCompatFixInstalled", "No");
            string fsCompatFixString = (string)fsCompatFixValue;

            if (fsCompatFixString == "Yes")
            {
                FinalSunCompatFixInstalled = true;
                btnMapEditorCompatibilityFix.Text = "Disable";
            }

            object tsCompatFixDeclinedValue = regKey.GetValue("TSCompatFixDeclined", "No");

            if (((string)tsCompatFixDeclinedValue) == "Yes")
            {
                GameCompatFixDeclined = true;
            }

            //object fsCompatFixDeclinedValue = regKey.GetValue("FSCompatFixDeclined", "No");

            //if (((string)fsCompatFixDeclinedValue) == "Yes")
            //{
            //    FinalSunCompatFixDeclined = true;
            //}
#endif
        }

        public override bool Save()
        {
            bool restartRequired = false;

            IniSettings.DetailLevel.Value = ddDetailLevel.SelectedIndex;

            string[] resolution = ddIngameResolution.SelectedItem.Text.Split('x');

            int[] ingameRes = new int[2] { int.Parse(resolution[0]), int.Parse(resolution[1]) };

            IniSettings.IngameScreenWidth.Value = ingameRes[0];
            IniSettings.IngameScreenHeight.Value = ingameRes[1];

            IniSettings.WindowedMode.Value = chkWindowedMode.Checked &&
                ddRenderer.SelectedItem.Text != "DxWnd";

            IniSettings.BorderlessWindowedMode.Value = chkBorderlessWindowedMode.Checked &&
                ddRenderer.SelectedItem.Text != "DxWnd";

            string[] clientResolution = ((string)ddClientResolution.SelectedItem.Tag).Split('x');

            int[] clientRes = new int[2] { int.Parse(clientResolution[0]), int.Parse(clientResolution[1]) };

            IniSettings.ClientResolutionX.Value = clientRes[0];
            IniSettings.ClientResolutionY.Value = clientRes[1];

            if (clientRes[0] != WindowManager.ResolutionWidth ||
                clientRes[1] != WindowManager.ResolutionHeight)
                restartRequired = true;

            if (IniSettings.BorderlessWindowedClient.Value != chkBorderlessClient.Checked)
                restartRequired = true;

            IniSettings.BorderlessWindowedClient.Value = chkBorderlessClient.Checked;

            if (IniSettings.ClientTheme != ddClientTheme.SelectedIndex)
                restartRequired = true;

            IniSettings.ClientTheme.Value = ddClientTheme.SelectedIndex;

#if YR
            IniSettings.BackBufferInVRAM.Value = chkBackBufferInVRAM.Checked;
#else
            IniSettings.BackBufferInVRAM.Value = !chkBackBufferInVRAM.Checked;
#endif

            fileSettingCheckBoxes.ForEach(chkBox => chkBox.Save());

            string renderer = "Default";

            File.Delete(ProgramConstants.GamePath + "ddraw.dll");
            File.Delete(ProgramConstants.GamePath + "libwine.dll");
            File.Delete(ProgramConstants.GamePath + "wined3d.dll");
            File.Delete(ProgramConstants.GamePath + "dxwnd.dll");

#if !YR
            File.Delete(ProgramConstants.GamePath + "Language.dll");

            if (ingameRes[0] >= 1024 && ingameRes[1] >= 720)
                File.Copy(ProgramConstants.GamePath + "Resources\\language_1024x720.dll", ProgramConstants.GamePath + "Language.dll");
            else if (ingameRes[0] >= 800 && ingameRes[1] >= 600)
                File.Copy(ProgramConstants.GamePath + "Resources\\language_800x600.dll", ProgramConstants.GamePath + "Language.dll");
            else
                File.Copy(ProgramConstants.GamePath + "Resources\\language_640x480.dll", ProgramConstants.GamePath + "Language.dll");
#endif

            switch (ddRenderer.SelectedIndex)
            {
                case 5:
                    renderer = "Software";
                    File.Copy(ProgramConstants.GamePath + "Resources\\ddraw_nohw.dll", ProgramConstants.GamePath + "ddraw.dll");
                    break;
                case 4:
                    renderer = "DxWnd";
                    File.Copy(ProgramConstants.GamePath + "Resources\\ddraw_dxwnd.dll", ProgramConstants.GamePath + "ddraw.dll");
                    File.Copy(ProgramConstants.GamePath + "Resources\\dxwnd.dll", ProgramConstants.GamePath + "dxwnd.dll");
                    if (!File.Exists(ProgramConstants.GamePath + "dxwnd.ini"))
                    {
                        File.Copy(ProgramConstants.GamePath + "Resources\\dxwnd.ini", ProgramConstants.GamePath + "dxwnd.ini");
                    }

                    IniFile dxWndIni = new IniFile(ProgramConstants.GamePath + "dxwnd.ini");
                    dxWndIni.SetBooleanValue("DxWnd", "RunInWindow", chkWindowedMode.Checked);
                    dxWndIni.SetBooleanValue("DxWnd", "NoWindowFrame", chkBorderlessWindowedMode.Checked);
                    dxWndIni.WriteIniFile();

                    break;
                case 3:
                    renderer = "DDWrapper"; ;
                    File.Copy(ProgramConstants.GamePath + "Resources\\ddwrapper.dll", ProgramConstants.GamePath + "ddraw.dll");
                    if (!File.Exists(ProgramConstants.GamePath + "aqrit.cfg"))
                        File.Copy(ProgramConstants.GamePath + "Resources\\aqrit.cfg", ProgramConstants.GamePath + "aqrit.cfg");
                    break;
                case 2:
                    renderer = "TS_DDRAW";
                    File.Copy(ProgramConstants.GamePath + "Resources\\ts_ddraw.dll", ProgramConstants.GamePath + "ddraw.dll");
                    break;
                case 1:
                    renderer = "IE_DDRAW";
                    File.Copy(ProgramConstants.GamePath + "Resources\\ie_ddraw.dll", ProgramConstants.GamePath + "ddraw.dll");
                    File.Copy(ProgramConstants.GamePath + "Resources\\libwine.dll", ProgramConstants.GamePath + "libwine.dll");
                    File.Copy(ProgramConstants.GamePath + "Resources\\wined3d.dll", ProgramConstants.GamePath + "wined3d.dll");
                    break;
            }

            IniSettings.Renderer.Value = renderer;

            return restartRequired;
        }

        /// <summary>
        /// Loads the user's preferred renderer.
        /// </summary>
        private void GetRenderer()
        {
            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            string renderer = UserINISettings.Instance.Renderer;

            if (string.IsNullOrEmpty(renderer))
            {
                // Use defaults
                if (osVersion == OSVersion.WIN810)
                    renderer = "TS_DDRAW";
                else
                    renderer = "Default";
            }

            switch (renderer)
            {
                case "IE_DDRAW":
                    ddRenderer.SelectedIndex = 1;
                    break;
                case "TS_DDRAW":
                    ddRenderer.SelectedIndex = 2;
                    break;
                case "DDWrapper":
                    ddRenderer.SelectedIndex = 3;
                    break;
                case "DxWnd":
                    ddRenderer.SelectedIndex = 4;
                    break;
                case "Software":
                    if (osVersion == OSVersion.WIN7 ||
                        osVersion == OSVersion.WIN810 ||
                        osVersion == OSVersion.WINVISTA)
                        goto case "TS_DDRAW";
                    else
                        ddRenderer.SelectedIndex = 5;
                    break;
                case "Default":
                default:
                    ddRenderer.SelectedIndex = 0;
                    break;
            }
        }

        /// <summary>
        /// A single screen resolution.
        /// </summary>
        sealed class ScreenResolution : IComparable<ScreenResolution>
        {
            public ScreenResolution(int width, int height)
            {
                Width = width;
                Height = height;
            }

            /// <summary>
            /// The width of the resolution in pixels.
            /// </summary>
            public int Width { get; set; }

            /// <summary>
            /// The height of the resolution in pixels.
            /// </summary>
            public int Height { get; set; }

            public override string ToString()
            {
                return Width + "x" + Height;
            }

            public int CompareTo(ScreenResolution res2)
            {
                if (this.Width < res2.Width)
                    return -1;
                else if (this.Width > res2.Width)
                    return 1;
                else // equal
                {
                    if (this.Height < res2.Height)
                        return -1;
                    else if (this.Height > res2.Height)
                        return 1;
                    else return 0;
                }
            }
        }

        /// <summary>
        ///     Code by Vimvq1987, from http://stackoverflow.com/questions/744541/how-to-list-available-video-modes-using-c
        ///     See also http://msdn.microsoft.com/en-us/library/dd162612(VS.85).aspx
        /// </summary>
        sealed class ScreenResolutionOperations
        {
            [DllImport("user32.dll")]
            public static extern bool EnumDisplaySettings(
                  string deviceName, int modeNum, ref DEVMODE devMode);

            [DllImport("user32.dll")]
            public static extern long ChangeDisplaySettings(
                ref DEVMODE devMode, int flags);

            [DllImport("user32.dll")]
            private static extern bool EnumDisplayDevices(
                IntPtr lpDevice, int iDevNum,
                ref DISPLAY_DEVICE lpDisplayDevice, int dwFlags);

            const int ENUM_CURRENT_SETTINGS = -1;

            const int ENUM_REGISTRY_SETTINGS = -2;

            private DEVMODE GetDevmode(int devNum, int modeNum)
            { //populates DEVMODE for the specified device and mode
                DEVMODE devMode = new DEVMODE();
                string devName = GetDeviceName(devNum);
                EnumDisplaySettings(devName, modeNum, ref devMode);
                return devMode;
            }

            private string GetDeviceName(int devNum)
            {
                DISPLAY_DEVICE d = new DISPLAY_DEVICE(0);
                bool result = EnumDisplayDevices(IntPtr.Zero,
                    devNum, ref d, 0);
                return (result ? d.DeviceName.Trim() : "#error#");
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DISPLAY_DEVICE
            {
                public int cb;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string DeviceName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string DeviceString;
                public int StateFlags;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string DeviceID;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string DeviceKey;

                public DISPLAY_DEVICE(int flags)
                {
                    cb = 0;
                    StateFlags = flags;
                    DeviceName = new string((char)32, 32);
                    DeviceString = new string((char)32, 128);
                    DeviceID = new string((char)32, 128);
                    DeviceKey = new string((char)32, 128);
                    cb = Marshal.SizeOf(this);
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DEVMODE
            {
                private const int CCHDEVICENAME = 0x20;
                private const int CCHFORMNAME = 0x20;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string dmDeviceName;
                public short dmSpecVersion;
                public short dmDriverVersion;
                public short dmSize;
                public short dmDriverExtra;
                public int dmFields;
                public int dmPositionX;
                public int dmPositionY;
                public ScreenOrientation dmDisplayOrientation;
                public int dmDisplayFixedOutput;
                public short dmColor;
                public short dmDuplex;
                public short dmYResolution;
                public short dmTTOption;
                public short dmCollate;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string dmFormName;
                public short dmLogPixels;
                public int dmBitsPerPel;
                public int dmPelsWidth;
                public int dmPelsHeight;
                public int dmDisplayFlags;
                public int dmDisplayFrequency;
                public int dmICMMethod;
                public int dmICMIntent;
                public int dmMediaType;
                public int dmDitherType;
                public int dmReserved1;
                public int dmReserved2;
                public int dmPanningWidth;
                public int dmPanningHeight;
            }


            private static List<DEVMODE> getScreenResDevModes()
            {
                List<DEVMODE> devmodes = new List<DEVMODE>();
                DEVMODE vDevMode = new DEVMODE();
                int i = 0;
                while (EnumDisplaySettings(null, i, ref vDevMode))
                {
                    devmodes.Add(vDevMode);
                    i++;
                }
                return devmodes;
            }

            public static List<string> GetScreenResolutions(int minWidth,
                int minHeight, int maxWidth, int maxHeight, int colordepth, bool addOptimalResolutions)
            {
                List<ScreenResolution> screenresolutions = new List<ScreenResolution>();
                try
                {
                    foreach (DEVMODE devmode in getScreenResDevModes())
                    {
                        ScreenResolution mode = new ScreenResolution(devmode.dmPelsWidth, devmode.dmPelsHeight);

                        // "does not exist in list" condition, implemented using IComparable :)
                        Boolean notInList = screenresolutions.FindIndex(
                           delegate (ScreenResolution res)
                           {
                               return res.CompareTo(mode) == 0; // 'x.CompareTo(y)==0' means 'equals'
                           })
                            == -1; // check if index is -1 (meaning item is not found in list)

                        if (devmode.dmBitsPerPel == colordepth
                            && devmode.dmPelsWidth >= minWidth
                            && devmode.dmPelsHeight >= minHeight
                            && devmode.dmPelsWidth <= maxWidth
                            && devmode.dmPelsHeight <= maxHeight
                            && notInList)
                        {
                            screenresolutions.Add(mode);
                        }
                    }
                }
                catch
                {
                }

                if (addOptimalResolutions && maxWidth >= 1280 && maxHeight >= 800)
                {
                    // Add our "optimal resolutions" to the list if they don't exist,
                    // but the screen is big enough for them

                    if (screenresolutions.Find(res => res.Width == 1280 && res.Height == 800) == null)
                        screenresolutions.Add(new ScreenResolution(1280, 800));

                    if (screenresolutions.Find(res => res.Width == 1280 && res.Height == 768) == null)
                        screenresolutions.Add(new ScreenResolution(1280, 768));
                }

                // sort, using ScreenResolution's CompareTo method.
                screenresolutions.Sort();

                // make resolutions string list (in correct order)
                List<String> screenResList = new List<String>();
                foreach (ScreenResolution res in screenresolutions)
                    screenResList.Add(res.ToString());

                if (screenResList.Count == 0)
                    screenResList.Add(string.Format("{0}x{1}",GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
                                                    GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height
                                                    ));

                return screenResList;
            }
        }
    }
}
