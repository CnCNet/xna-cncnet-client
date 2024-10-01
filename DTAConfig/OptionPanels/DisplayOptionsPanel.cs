using ClientCore.Extensions;
using ClientCore;
using ClientGUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
#if WINFORMS
using System.Windows.Forms;
#endif
#if TS
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
#endif
using System.IO;
using ClientCore.I18N;
using System.Diagnostics;
using System.Linq;

namespace DTAConfig.OptionPanels
{
    class DisplayOptionsPanel : XNAOptionsPanel
    {
        private const int DRAG_DISTANCE_DEFAULT = 4;
        private const int ORIGINAL_RESOLUTION_WIDTH = 640;
        private const string RENDERERS_INI = "Renderers.ini";

        public DisplayOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        private XNAClientDropDown ddIngameResolution;
        private XNAClientDropDown ddDetailLevel;
        private XNAClientDropDown ddRenderer;
        private XNAClientCheckBox chkWindowedMode;
        private XNAClientCheckBox chkBorderlessWindowedMode;
        private XNAClientCheckBox chkBackBufferInVRAM;
        private XNAClientPreferredItemDropDown ddClientResolution;
        private XNAClientCheckBox chkBorderlessClient;
        private XNAClientDropDown ddClientTheme;
        private XNAClientDropDown ddTranslation;

        private List<DirectDrawWrapper> renderers;

        private string defaultRenderer;
        private DirectDrawWrapper selectedRenderer = null;

#if TS
        private XNALabel lblCompatibilityFixes;
        private XNALabel lblGameCompatibilityFix;
        private XNALabel lblMapEditorCompatibilityFix;
        private XNAClientButton btnGameCompatibilityFix;
        private XNAClientButton btnMapEditorCompatibilityFix;

        private bool GameCompatFixInstalled = false;
        private bool FinalSunCompatFixInstalled = false;
        private bool GameCompatFixDeclined = false;
        //private bool FinalSunCompatFixDeclined = false;
#endif


        public override void Initialize()
        {
            base.Initialize();

            Name = "DisplayOptionsPanel";

            var lblIngameResolution = new XNALabel(WindowManager);
            lblIngameResolution.Name = "lblIngameResolution";
            lblIngameResolution.ClientRectangle = new Rectangle(12, 14, 0, 0);
            lblIngameResolution.Text = "In-game Resolution:".L10N("Client:DTAConfig:InGameResolution");

            ddIngameResolution = new XNAClientDropDown(WindowManager);
            ddIngameResolution.Name = "ddIngameResolution";
            ddIngameResolution.ClientRectangle = new Rectangle(
                lblIngameResolution.Right + 12,
                lblIngameResolution.Y - 2, 120, 19);

            // Add in-game resolutions
            {
                var maximumIngameResolution = new ScreenResolution(ClientConfiguration.Instance.MaximumIngameWidth, ClientConfiguration.Instance.MaximumIngameHeight);

#if XNA
                if (!ScreenResolution.HiDefLimitResolution.Fit(maximumIngameResolution))
                    maximumIngameResolution = ScreenResolution.HiDefLimitResolution;
#endif

                SortedSet<ScreenResolution> resolutions = ScreenResolution.GetFullScreenResolutions(
                    ClientConfiguration.Instance.MinimumIngameWidth, ClientConfiguration.Instance.MinimumIngameHeight,
                    maximumIngameResolution.Width, maximumIngameResolution.Height);

                foreach (var res in resolutions)
                    ddIngameResolution.AddItem(res.ToString());
            }

            var lblDetailLevel = new XNALabel(WindowManager);
            lblDetailLevel.Name = "lblDetailLevel";
            lblDetailLevel.ClientRectangle = new Rectangle(lblIngameResolution.X,
                ddIngameResolution.Bottom + 16, 0, 0);
            lblDetailLevel.Text = "Detail Level:".L10N("Client:DTAConfig:DetailLevel");

            ddDetailLevel = new XNAClientDropDown(WindowManager);
            ddDetailLevel.Name = "ddDetailLevel";
            ddDetailLevel.ClientRectangle = new Rectangle(
                ddIngameResolution.X,
                lblDetailLevel.Y - 2,
                ddIngameResolution.Width,
                ddIngameResolution.Height);
            ddDetailLevel.AddItem("Low".L10N("Client:DTAConfig:DetailLevelLow"));
            ddDetailLevel.AddItem("Medium".L10N("Client:DTAConfig:DetailLevelMedium"));
            ddDetailLevel.AddItem("High".L10N("Client:DTAConfig:DetailLevelHigh"));

            var lblRenderer = new XNALabel(WindowManager);
            lblRenderer.Name = "lblRenderer";
            lblRenderer.ClientRectangle = new Rectangle(lblDetailLevel.X,
                ddDetailLevel.Bottom + 16, 0, 0);
            lblRenderer.Text = "Renderer:".L10N("Client:DTAConfig:Renderer");

            ddRenderer = new XNAClientDropDown(WindowManager);
            ddRenderer.Name = "ddRenderer";
            ddRenderer.ClientRectangle = new Rectangle(
                ddDetailLevel.X,
                lblRenderer.Y - 2,
                ddDetailLevel.Width,
                ddDetailLevel.Height);

            GetRenderers();

            var localOS = ClientConfiguration.Instance.GetOperatingSystemVersion();

            foreach (var renderer in renderers)
            {
                if (renderer.IsCompatibleWithOS(localOS) && !renderer.Hidden)
                {
                    ddRenderer.AddItem(new XNADropDownItem()
                    {
                        Text = renderer.UIName,
                        Tag = renderer
                    });
                }
            }

            chkWindowedMode = new XNAClientCheckBox(WindowManager);
            chkWindowedMode.Name = "chkWindowedMode";
            chkWindowedMode.ClientRectangle = new Rectangle(lblDetailLevel.X,
                ddRenderer.Bottom + 16, 0, 0);
            chkWindowedMode.Text = "Windowed Mode".L10N("Client:DTAConfig:WindowedMode");
            chkWindowedMode.CheckedChanged += ChkWindowedMode_CheckedChanged;

            chkBorderlessWindowedMode = new XNAClientCheckBox(WindowManager);
            chkBorderlessWindowedMode.Name = "chkBorderlessWindowedMode";
            chkBorderlessWindowedMode.ClientRectangle = new Rectangle(
                chkWindowedMode.X + 50,
                chkWindowedMode.Bottom + 24, 0, 0);
            chkBorderlessWindowedMode.Text = "Borderless Windowed Mode".L10N("Client:DTAConfig:BorderlessWindowedMode");
            chkBorderlessWindowedMode.AllowChecking = false;

            chkBackBufferInVRAM = new XNAClientCheckBox(WindowManager);
            chkBackBufferInVRAM.Name = "chkBackBufferInVRAM";
            chkBackBufferInVRAM.ClientRectangle = new Rectangle(
                lblDetailLevel.X,
                chkBorderlessWindowedMode.Bottom + 28, 0, 0);
            chkBackBufferInVRAM.Text = ("Back Buffer in Video Memory\n(lower performance, but is\nnecessary on some systems)").L10N("Client:DTAConfig:BackBuffer");

            var lblClientResolution = new XNALabel(WindowManager);
            lblClientResolution.Name = "lblClientResolution";
            lblClientResolution.ClientRectangle = new Rectangle(
                285, 14, 0, 0);
            lblClientResolution.Text = "Client Resolution:".L10N("Client:DTAConfig:ClientResolution");

            ddClientResolution = new XNAClientPreferredItemDropDown(WindowManager);
            ddClientResolution.Name = "ddClientResolution";
            ddClientResolution.ClientRectangle = new Rectangle(
                lblClientResolution.Right + 12,
                lblClientResolution.Y - 2,
                Width - (lblClientResolution.Right + 24),
                ddIngameResolution.Height);
            ddClientResolution.AllowDropDown = false;
            ddClientResolution.PreferredItemLabel = "(recommended)".L10N("Client:DTAConfig:Recommended");

            // Add client resolutions
            {
                List<ScreenResolution> recommendedResolutions = ClientConfiguration.Instance.RecommendedResolutions.Select(resolution => (ScreenResolution)resolution).ToList();
                SortedSet<ScreenResolution> scaledRecommendedResolutions = [.. recommendedResolutions.SelectMany(resolution => resolution.GetIntegerScaledResolutions())];

                SortedSet<ScreenResolution> resolutions = [
                    .. ScreenResolution.GetFullScreenResolutions(minWidth: 800, minHeight: 600),
                    .. ScreenResolution.GetWindowedResolutions(minWidth: 800, minHeight: 600),
                    .. scaledRecommendedResolutions
                ];
                List<ScreenResolution> resolutionList = resolutions.ToList();

                foreach (ScreenResolution res in resolutionList)
                {
                    var item = new XNADropDownItem();
                    item.Text = res.ToString();
                    item.Tag = res.ToString();
                    ddClientResolution.AddItem(item);
                }

                // So we add the optimal resolutions to the list, sort it and then find
                // out the optimal resolution index - it's inefficient, but works
                // Note: ddClientResolution.PreferredItemIndexes is assumed in ascending order

                foreach (ScreenResolution scaledRecommendedResolution in scaledRecommendedResolutions)
                {
                    int index = resolutionList.FindIndex(res => res == scaledRecommendedResolution);
                    if (index > -1)
                        ddClientResolution.PreferredItemIndexes.Add(index);
                }
            }

            chkBorderlessClient = new XNAClientCheckBox(WindowManager);
            chkBorderlessClient.Name = "chkBorderlessClient";
            chkBorderlessClient.ClientRectangle = new Rectangle(
                lblClientResolution.X,
                lblDetailLevel.Y, 0, 0);
            chkBorderlessClient.Text = "Fullscreen Client".L10N("Client:DTAConfig:FullscreenClient");
            chkBorderlessClient.CheckedChanged += ChkBorderlessMenu_CheckedChanged;
            chkBorderlessClient.Checked = true;

            var lblClientTheme = new XNALabel(WindowManager);
            lblClientTheme.Name = "lblClientTheme";
            lblClientTheme.ClientRectangle = new Rectangle(
                lblClientResolution.X,
                lblRenderer.Y, 0, 0);
            lblClientTheme.Text = "Client Theme:".L10N("Client:DTAConfig:ClientTheme");

            ddClientTheme = new XNAClientDropDown(WindowManager);
            ddClientTheme.Name = "ddClientTheme";
            ddClientTheme.ClientRectangle = new Rectangle(
                ddClientResolution.X,
                ddRenderer.Y,
                ddClientResolution.Width,
                ddRenderer.Height);

            int themeCount = ClientConfiguration.Instance.ThemeCount;

            for (int i = 0; i < themeCount; i++)
            {
                string themeName = ClientConfiguration.Instance.GetThemeInfoFromIndex(i).Name;

                string displayName = themeName.L10N($"INI:Themes:{themeName}");
                ddClientTheme.AddItem(new XNADropDownItem { Text = displayName, Tag = themeName });
            }

            var lblTranslation = new XNALabel(WindowManager);
            lblTranslation.Name = nameof(lblTranslation);
            lblTranslation.ClientRectangle = new Rectangle(
                lblClientTheme.X,
                ddClientTheme.Bottom + 16, 0, 0);
            lblTranslation.Text = "Language:".L10N("Client:DTAConfig:Language");

            ddTranslation = new XNAClientDropDown(WindowManager);
            ddTranslation.Name = nameof(ddTranslation);
            ddTranslation.ClientRectangle = new Rectangle(
                ddClientTheme.X,
                lblTranslation.Y - 2,
                ddClientTheme.Width,
                ddClientTheme.Height);

            foreach (var (translation, name) in Translation.GetTranslations())
                ddTranslation.AddItem(new XNADropDownItem { Text = name, Tag = translation });

#if TS
            lblCompatibilityFixes = new XNALabel(WindowManager);
            lblCompatibilityFixes.Name = "lblCompatibilityFixes";
            lblCompatibilityFixes.FontIndex = 1;
            lblCompatibilityFixes.Text = "Compatibility Fixes (advanced):".L10N("Client:DTAConfig:TSCompatibilityFixAdv");
            AddChild(lblCompatibilityFixes);
            lblCompatibilityFixes.CenterOnParent();
            lblCompatibilityFixes.Y = Height - 103;

            lblGameCompatibilityFix = new XNALabel(WindowManager);
            lblGameCompatibilityFix.Name = "lblGameCompatibilityFix";
            lblGameCompatibilityFix.ClientRectangle = new Rectangle(132,
                lblCompatibilityFixes.Bottom + 20, 0, 0);
            lblGameCompatibilityFix.Text = "DTA/TI/TS Compatibility Fix:".L10N("Client:DTAConfig:TSCompatibilityFix");

            btnGameCompatibilityFix = new XNAClientButton(WindowManager);
            btnGameCompatibilityFix.Name = "btnGameCompatibilityFix";
            btnGameCompatibilityFix.ClientRectangle = new Rectangle(
                lblGameCompatibilityFix.Right + 20,
                lblGameCompatibilityFix.Y - 4, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnGameCompatibilityFix.FontIndex = 1;
            btnGameCompatibilityFix.Text = "Enable".L10N("Client:DTAConfig:Enable");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                btnGameCompatibilityFix.LeftClick += BtnGameCompatibilityFix_LeftClick;
            else
                btnGameCompatibilityFix.AllowClick = false;

            lblMapEditorCompatibilityFix = new XNALabel(WindowManager);
            lblMapEditorCompatibilityFix.Name = "lblMapEditorCompatibilityFix";
            lblMapEditorCompatibilityFix.ClientRectangle = new Rectangle(
                lblGameCompatibilityFix.X,
                lblGameCompatibilityFix.Bottom + 20, 0, 0);
            lblMapEditorCompatibilityFix.Text = "FinalSun Compatibility Fix:".L10N("Client:DTAConfig:TSFinalSunFix");

            btnMapEditorCompatibilityFix = new XNAClientButton(WindowManager);
            btnMapEditorCompatibilityFix.Name = "btnMapEditorCompatibilityFix";
            btnMapEditorCompatibilityFix.ClientRectangle = new Rectangle(
                btnGameCompatibilityFix.X,
                lblMapEditorCompatibilityFix.Y - 4,
                btnGameCompatibilityFix.Width,
                btnGameCompatibilityFix.Height);
            btnMapEditorCompatibilityFix.FontIndex = 1;
            btnMapEditorCompatibilityFix.Text = "Enable".L10N("Client:DTAConfig:TSButtonEnable");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                btnMapEditorCompatibilityFix.LeftClick += BtnMapEditorCompatibilityFix_LeftClick;
            else
                btnMapEditorCompatibilityFix.AllowClick = false;

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
            AddChild(lblTranslation);
            AddChild(ddTranslation);
            AddChild(lblClientResolution);
            AddChild(ddClientResolution);
            AddChild(lblRenderer);
            AddChild(ddRenderer);
            AddChild(lblDetailLevel);
            AddChild(ddDetailLevel);
            AddChild(lblIngameResolution);
            AddChild(ddIngameResolution);
        }

        public static ScreenResolution GetBestRecommendedResolution()
        {
            List<ScreenResolution> recommendedResolutions = ClientConfiguration.Instance.RecommendedResolutions.Select(resolution => (ScreenResolution)resolution).ToList();
            SortedSet<ScreenResolution> scaledRecommendedResolutions = [.. recommendedResolutions.SelectMany(resolution => resolution.GetIntegerScaledResolutions())];
            return scaledRecommendedResolutions.Max();
        }

        private void GetRenderers()
        {
            renderers = new List<DirectDrawWrapper>();

            var renderersIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), RENDERERS_INI));

            var keys = renderersIni.GetSectionKeys("Renderers");
            if (keys == null)
                throw new ClientConfigurationException("[Renderers] not found from Renderers.ini!");

            foreach (string key in keys)
            {
                string internalName = renderersIni.GetStringValue("Renderers", key, string.Empty);

                var ddWrapper = new DirectDrawWrapper(internalName, renderersIni);
                renderers.Add(ddWrapper);
            }

            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            defaultRenderer = renderersIni.GetStringValue("DefaultRenderer", osVersion.ToString(), string.Empty);

            if (defaultRenderer == null)
                throw new ClientConfigurationException("Invalid or missing default renderer for operating system: " + osVersion);

            string renderer = UserINISettings.Instance.Renderer;

            selectedRenderer = renderers.Find(r => r.InternalName == renderer);

            if (selectedRenderer == null)
                selectedRenderer = renderers.Find(r => r.InternalName == defaultRenderer);

            if (selectedRenderer == null)
                throw new ClientConfigurationException("Missing renderer: " + renderer);

            GameProcessLogic.UseQres = selectedRenderer.UseQres;
            GameProcessLogic.SingleCoreAffinity = selectedRenderer.SingleCoreAffinity;
        }
#if TS

        /// <summary>
        /// Asks the user whether they want to install the DTA/TI/TS compatibility fix.
        /// </summary>
        public void PostInit()
        {
            Load();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            if (!GameCompatFixInstalled && !GameCompatFixDeclined)
            {
                string defaultGame = ClientConfiguration.Instance.LocalGame;

                var messageBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "New Compatibility Fix".L10N("Client:DTAConfig:TSFixTitle"),
                    string.Format("A performance-enhancing compatibility fix for modern Windows versions\n" +
                        "has been included in this version of {0}. Enabling it requires\n" +
                        "administrative priveleges. Would you like to install the compatibility fix?\n\n" +
                        "You'll always be able to install or uninstall the compatibility fix later from the options menu.".L10N("Client:DTAConfig:TSFixText"),
                        defaultGame));
                messageBox.YesClickedAction = MessageBox_YesClicked;
                messageBox.NoClickedAction = MessageBox_NoClicked;
            }
        }

        [SupportedOSPlatform("windows")]
        private void MessageBox_NoClicked(XNAMessageBox messageBox)
        {
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
                    Logger.Log("Setting TSCompatFixDeclined failed! Returned error: " + ex.ToString());
                }
            }
            catch { }
        }

        [SupportedOSPlatform("windows")]
        private void MessageBox_YesClicked(XNAMessageBox messageBox)
        {
            BtnGameCompatibilityFix_LeftClick(messageBox, EventArgs.Empty);
        }

        [SupportedOSPlatform("windows")]
        private void BtnGameCompatibilityFix_LeftClick(object sender, EventArgs e)
        {
            if (GameCompatFixInstalled)
            {
                try
                {
                    Process sdbinst = Process.Start("sdbinst.exe", "-q -n \"TS Compatibility Fix\"");

                    sdbinst.WaitForExit();

                    Logger.Log("DTA/TI/TS Compatibility Fix succesfully uninstalled.");
                    XNAMessageBox.Show(WindowManager, "Compatibility Fix Uninstalled".L10N("Client:DTAConfig:TSFixUninstallTitle"),
                        "The DTA/TI/TS Compatibility Fix has been succesfully uninstalled.".L10N("Client:DTAConfig:TSFixUninstallText"));

                    RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                    regKey = regKey.CreateSubKey("Tiberian Sun Client");
                    regKey.SetValue("TSCompatFixInstalled", "No");

                    btnGameCompatibilityFix.Text = "Enable";

                    GameCompatFixInstalled = false;
                }
                catch (Exception ex)
                {
                    Logger.Log("Uninstalling DTA/TI/TS Compatibility Fix failed. Error message: " + ex.ToString());
                    XNAMessageBox.Show(WindowManager, "Uninstalling Compatibility Fix Failed".L10N("Client:DTAConfig:TSFixUninstallFailTitle"),
                        "Uninstalling DTA/TI/TS Compatibility Fix failed. Returned error:".L10N("Client:DTAConfig:TSFixUninstallFailText") + " " + ex.Message);
                }

                return;
            }

            try
            {
                Process sdbinst = Process.Start("sdbinst.exe", "-q \"" + ProgramConstants.GamePath + "Resources/compatfix.sdb\"");

                sdbinst.WaitForExit();

                Logger.Log("DTA/TI/TS Compatibility Fix succesfully installed.");
                XNAMessageBox.Show(WindowManager, "Compatibility Fix Installed".L10N("Client:DTAConfig:TSFixInstallSuccessTitle"),
                    "The DTA/TI/TS Compatibility Fix has been succesfully installed.".L10N("Client:DTAConfig:TSFixInstallSuccessText"));

                RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                regKey = regKey.CreateSubKey("Tiberian Sun Client");
                regKey.SetValue("TSCompatFixInstalled", "Yes");

                btnGameCompatibilityFix.Text = "Disable";

                GameCompatFixInstalled = true;
            }
            catch (Exception ex)
            {
                Logger.Log("Installing DTA/TI/TS Compatibility Fix failed. Error message: " + ex.ToString());
                XNAMessageBox.Show(WindowManager, "Installing Compatibility Fix Failed".L10N("Client:DTAConfig:TSFixInstallFailTitle"),
                    "Installing DTA/TI/TS Compatibility Fix failed. Error message:".L10N("Client:DTAConfig:TSFixInstallFailText") + " " + ex.Message);
            }
        }

        [SupportedOSPlatform("windows")]
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

                    btnMapEditorCompatibilityFix.Text = "Enable".L10N("Client:DTAConfig:TSFEnable");

                    Logger.Log("FinalSun Compatibility Fix succesfully uninstalled.");
                    XNAMessageBox.Show(WindowManager, "Compatibility Fix Uninstalled".L10N("Client:DTAConfig:TSFinalSunFixUninstallTitle"),
                        "The FinalSun Compatibility Fix has been succesfully uninstalled.".L10N("Client:DTAConfig:TSFinalSunFixUninstallText"));

                    FinalSunCompatFixInstalled = false;
                }
                catch (Exception ex)
                {
                    Logger.Log("Uninstalling FinalSun Compatibility Fix failed. Error message: " + ex.ToString());
                    XNAMessageBox.Show(WindowManager, "Uninstalling Compatibility Fix Failed".L10N("Client:DTAConfig:TSFinalSunFixUninstallFailedTitle"),
                        "Uninstalling FinalSun Compatibility Fix failed. Error message:".L10N("Client:DTAConfig:TSFinalSunFixUninstallFailedText") + " " + ex.Message);
                }

                return;
            }

            try
            {
                Process sdbinst = Process.Start("sdbinst.exe", "-q \"" + ProgramConstants.GamePath + "Resources/FSCompatFix.sdb\"");

                sdbinst.WaitForExit();

                RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                regKey = regKey.CreateSubKey("Tiberian Sun Client");
                regKey.SetValue("FSCompatFixInstalled", "Yes");

                btnMapEditorCompatibilityFix.Text = "Disable".L10N("Client:DTAConfig:TSDisable");

                Logger.Log("FinalSun Compatibility Fix succesfully installed.");
                XNAMessageBox.Show(WindowManager, "Compatibility Fix Installed".L10N("Client:DTAConfig:TSFinalSunCompatibilityFixInstalledTitle"),
                    "The FinalSun Compatibility Fix has been succesfully installed.".L10N("Client:DTAConfig:TSFinalSunCompatibilityFixInstalledText"));

                FinalSunCompatFixInstalled = true;
            }
            catch (Exception ex)
            {
                Logger.Log("Installing FinalSun Compatibility Fix failed. Error message: " + ex.ToString());
                XNAMessageBox.Show(WindowManager, "Installing Compatibility Fix Failed".L10N("Client:DTAConfig:TSFinalSunCompatibilityFixInstalledFailedTitle"),
                    "Installing FinalSun Compatibility Fix failed. Error message:".L10N("Client:DTAConfig:TSFinalSunCompatibilityFixInstalledFailedText") + " " + ex.Message);
            }
        }
#endif

        private void ChkBorderlessMenu_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBorderlessClient.Checked)
            {
                ddClientResolution.AllowDropDown = false;

                string nativeRes = ScreenResolution.SafeFullScreenResolution;

                int nativeResIndex = ddClientResolution.Items.FindIndex(i => (string)i.Tag == nativeRes);
                if (nativeResIndex > -1)
                    ddClientResolution.SelectedIndex = nativeResIndex;
            }
            else
            {
                ddClientResolution.AllowDropDown = true;

                if (ddClientResolution.PreferredItemIndexes.Count > 0)
                {
                    // Note: ddClientResolution.PreferredItemIndexes is assumed in ascending order
                    int optimalWindowedResIndex = ddClientResolution.PreferredItemIndexes[^1];
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

        /// <summary>
        /// Loads the user's preferred renderer.
        /// </summary>
        private void LoadRenderer()
        {
            int index = ddRenderer.Items.FindIndex(
                           r => ((DirectDrawWrapper)r.Tag).InternalName == selectedRenderer.InternalName);

            if (index < 0 && selectedRenderer.Hidden)
            {
                ddRenderer.AddItem(new XNADropDownItem()
                {
                    Text = selectedRenderer.UIName,
                    Tag = selectedRenderer
                });
                index = ddRenderer.Items.Count - 1;
            }

            ddRenderer.SelectedIndex = index;
        }

        public override void Load()
        {
            base.Load();

            LoadRenderer();
            ddDetailLevel.SelectedIndex = UserINISettings.Instance.DetailLevel;

            string currentRes = UserINISettings.Instance.IngameScreenWidth.Value +
                "x" + UserINISettings.Instance.IngameScreenHeight.Value;

            int index = ddIngameResolution.Items.FindIndex(i => i.Text == currentRes);

            ddIngameResolution.SelectedIndex = index > -1 ? index : 0;

            // Wonder what this "Win8CompatMode" actually does..
            // Disabling it used to be TS-DDRAW only, but it was never enabled after 
            // you had tried TS-DDRAW once, so most players probably have it always
            // disabled anyway
            IniSettings.Win8CompatMode.Value = "No";

            var renderer = (DirectDrawWrapper)ddRenderer.SelectedItem.Tag;

            if (renderer.UsesCustomWindowedOption())
            {
                // For renderers that have their own windowed mode implementation
                // enabled through their own config INI file
                // (for example DxWnd and CnC-DDRAW)

                IniFile rendererSettingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, renderer.ConfigFileName));

                chkWindowedMode.Checked = rendererSettingsIni.GetBooleanValue(renderer.WindowedModeSection,
                    renderer.WindowedModeKey, false);

                if (!string.IsNullOrEmpty(renderer.BorderlessWindowedModeKey))
                {
                    bool setting = rendererSettingsIni.GetBooleanValue(renderer.WindowedModeSection,
                        renderer.BorderlessWindowedModeKey, false);
                    chkBorderlessWindowedMode.Checked = renderer.IsBorderlessWindowedModeKeyReversed ? !setting : setting;
                }
                else
                {
                    chkBorderlessWindowedMode.Checked = UserINISettings.Instance.BorderlessWindowedMode;
                }
            }
            else
            {
                chkWindowedMode.Checked = UserINISettings.Instance.WindowedMode;
                chkBorderlessWindowedMode.Checked = UserINISettings.Instance.BorderlessWindowedMode;
            }

            string currentClientRes = IniSettings.ClientResolutionX.Value + "x" + IniSettings.ClientResolutionY.Value;

            int clientResIndex = ddClientResolution.Items.FindIndex(i => (string)i.Tag == currentClientRes);

            ddClientResolution.SelectedIndex = clientResIndex > -1 ? clientResIndex : 0;

            chkBorderlessClient.Checked = UserINISettings.Instance.BorderlessWindowedClient;

            int selectedThemeIndex = ddClientTheme.Items.FindIndex(
                ddi => (string)ddi.Tag == UserINISettings.Instance.ClientTheme);
            ddClientTheme.SelectedIndex = selectedThemeIndex > -1 ? selectedThemeIndex : 0;

            foreach (string localeCode in new string[] { UserINISettings.Instance.Translation, Translation.GetDefaultTranslationLocaleCode(), ProgramConstants.HARDCODED_LOCALE_CODE })
            {
                int selectedTranslationIndex = ddTranslation.Items.FindIndex(
                    ddi => localeCode.Equals((string)ddi.Tag, StringComparison.InvariantCultureIgnoreCase));

                if (selectedTranslationIndex > -1)
                {
                    ddTranslation.SelectedIndex = selectedTranslationIndex;
                    break;
                }
            }

            Debug.Assert(ddTranslation.SelectedIndex > -1, "No translation was selected");

#if TS
            chkBackBufferInVRAM.Checked = !UserINISettings.Instance.BackBufferInVRAM;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Tiberian Sun Client");

            if (regKey == null)
                return;

            object tsCompatFixValue = regKey.GetValue("TSCompatFixInstalled", "No");
            string tsCompatFixString = (string)tsCompatFixValue;

            if (tsCompatFixString == "Yes")
            {
                GameCompatFixInstalled = true;
                btnGameCompatibilityFix.Text = "Disable".L10N("Client:DTAConfig:TSDisable");
            }

            object fsCompatFixValue = regKey.GetValue("FSCompatFixInstalled", "No");
            string fsCompatFixString = (string)fsCompatFixValue;

            if (fsCompatFixString == "Yes")
            {
                FinalSunCompatFixInstalled = true;
                btnMapEditorCompatibilityFix.Text = "Disable".L10N("Client:DTAConfig:TSDisable");
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
#else
            chkBackBufferInVRAM.Checked = UserINISettings.Instance.BackBufferInVRAM;
#endif
        }

        public override bool Save()
        {
            bool restartRequired = base.Save();

            IniSettings.DetailLevel.Value = ddDetailLevel.SelectedIndex;

            ScreenResolution ingameRes = ddIngameResolution.SelectedItem.Text;

            (IniSettings.IngameScreenWidth.Value, IniSettings.IngameScreenHeight.Value) = ingameRes;

            // Calculate drag selection distance, scale it with resolution width
            int dragDistance = ingameRes.Width / ORIGINAL_RESOLUTION_WIDTH * DRAG_DISTANCE_DEFAULT;
            IniSettings.DragDistance.Value = dragDistance;

            DirectDrawWrapper originalRenderer = selectedRenderer;
            selectedRenderer = (DirectDrawWrapper)ddRenderer.SelectedItem.Tag;

            IniSettings.WindowedMode.Value = chkWindowedMode.Checked &&
                !selectedRenderer.UsesCustomWindowedOption();

            IniSettings.BorderlessWindowedMode.Value = chkBorderlessWindowedMode.Checked &&
                string.IsNullOrEmpty(selectedRenderer.BorderlessWindowedModeKey);

            ScreenResolution clientRes = (string)ddClientResolution.SelectedItem.Tag;

            if (clientRes.Width != IniSettings.ClientResolutionX.Value ||
                clientRes.Height != IniSettings.ClientResolutionY.Value)
                restartRequired = true;

            (IniSettings.ClientResolutionX.Value, IniSettings.ClientResolutionY.Value) = clientRes;

            if (IniSettings.BorderlessWindowedClient.Value != chkBorderlessClient.Checked)
                restartRequired = true;

            IniSettings.BorderlessWindowedClient.Value = chkBorderlessClient.Checked;

            restartRequired = restartRequired || IniSettings.ClientTheme != (string)ddClientTheme.SelectedItem.Tag;

            IniSettings.ClientTheme.Value = (string)ddClientTheme.SelectedItem.Tag;

            restartRequired = restartRequired || !IniSettings.Translation.ToString().Equals((string)ddTranslation.SelectedItem.Tag, StringComparison.InvariantCultureIgnoreCase);

            IniSettings.Translation.Value = (string)ddTranslation.SelectedItem.Tag;

            // copy translation files to the game directory
            foreach (TranslationGameFile tgf in ClientConfiguration.Instance.TranslationGameFiles)
            {
                string sourcePath = SafePath.CombineFilePath(IniSettings.TranslationFolderPath, tgf.Source);
                string targetPath = SafePath.CombineFilePath(ProgramConstants.GamePath, tgf.Target);

                if (File.Exists(sourcePath))
                {
                    string sourceHash = Utilities.CalculateSHA1ForFile(sourcePath);
                    string destinationHash = Utilities.CalculateSHA1ForFile(targetPath);

                    if (sourceHash != destinationHash)
                        File.Copy(sourcePath, targetPath, true);
                }
                else
                {
                    if (File.Exists(targetPath))
                        File.Delete(targetPath);
                }
            }

#if TS
            IniSettings.BackBufferInVRAM.Value = !chkBackBufferInVRAM.Checked;
#else
            IniSettings.BackBufferInVRAM.Value = chkBackBufferInVRAM.Checked;
#endif

            if (selectedRenderer != originalRenderer ||
                !SafePath.GetFile(ProgramConstants.GamePath, selectedRenderer.ConfigFileName).Exists)
            {
                foreach (var renderer in renderers)
                {
                    if (renderer != selectedRenderer)
                        renderer.Clean();
                }
            }

            selectedRenderer.Apply();

            GameProcessLogic.UseQres = selectedRenderer.UseQres;
            GameProcessLogic.SingleCoreAffinity = selectedRenderer.SingleCoreAffinity;

            if (selectedRenderer.UsesCustomWindowedOption())
            {
                IniFile rendererSettingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, selectedRenderer.ConfigFileName));

                rendererSettingsIni.SetBooleanValue(selectedRenderer.WindowedModeSection,
                    selectedRenderer.WindowedModeKey, chkWindowedMode.Checked);

                if (!string.IsNullOrEmpty(selectedRenderer.BorderlessWindowedModeKey))
                {
                    bool borderlessModeIniValue = chkBorderlessWindowedMode.Checked;
                    if (selectedRenderer.IsBorderlessWindowedModeKeyReversed)
                        borderlessModeIniValue = !borderlessModeIniValue;

                    rendererSettingsIni.SetBooleanValue(selectedRenderer.WindowedModeSection,
                        selectedRenderer.BorderlessWindowedModeKey, borderlessModeIniValue);
                }

                rendererSettingsIni.WriteIniFile();
            }

            IniSettings.Renderer.Value = selectedRenderer.InternalName;

#if TS
            if (ClientConfiguration.Instance.CopyResolutionDependentLanguageDLL)
            {
                string languageDllDestinationPath = SafePath.CombineFilePath(ProgramConstants.GamePath, "Language.dll");

                SafePath.DeleteFileIfExists(languageDllDestinationPath);

                if (ingameRes.Width >= 1024 && ingameRes.Height >= 720)
                    System.IO.File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources", "language_1024x720.dll"), languageDllDestinationPath);
                else if (ingameRes.Width >= 800 && ingameRes.Height >= 600)
                    System.IO.File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources", "language_800x600.dll"), languageDllDestinationPath);
                else
                    System.IO.File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources", "language_640x480.dll"), languageDllDestinationPath);
            }
#endif

            return restartRequired;
        }

    }
}
