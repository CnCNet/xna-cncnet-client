using ClientCore.Extensions;
using ClientCore;
using ClientGUI;
using DTAClient.Domain;
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
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.IO;
using ClientCore.I18N;
using ClientCore.Enums;
using System.Diagnostics;
using System.Linq;

namespace DTAClient.DXGUI.Generic.OptionPanels
{
    class DisplayOptionsPanel : XNAOptionsPanel
    {
        private const int DRAG_DISTANCE_DEFAULT = 4;
        private const int ORIGINAL_RESOLUTION_WIDTH = 640;

        private readonly DirectDrawWrapperManager directDrawWrapperManager;

        public DisplayOptionsPanel(WindowManager windowManager, UserINISettings iniSettings, DirectDrawWrapperManager directDrawWrapperManager)
            : base(windowManager, iniSettings)
        {
            this.directDrawWrapperManager = directDrawWrapperManager;
        }

        private XNAClientDropDown ddIngameResolution;
        private XNAClientDropDown ddDetailLevel;
        private XNAClientDropDown ddRenderer;
        private XNAClientCheckBox chkWindowedMode;
        private XNAClientCheckBox chkBorderlessWindowedMode;
        private XNAClientCheckBox chkBackBufferInVRAM;
        private XNAClientPreferredItemDropDown ddClientResolution;
        private XNAClientCheckBox chkBorderlessClient;
        private XNAClientCheckBox chkIntegerScaledClient;
        private XNAClientDropDown ddClientTheme;
        private XNAClientDropDown ddTranslation;

        private XNALabel lblCompatibilityFixes;
        private XNALabel lblGameCompatibilityFix;
        private XNALabel lblMapEditorCompatibilityFix;
        private XNAClientButton btnGameCompatibilityFix;
        private XNAClientButton btnMapEditorCompatibilityFix;

        private bool GameCompatFixInstalled = false;
        private bool FinalSunCompatFixInstalled = false;
        private bool GameCompatFixDeclined = false;
        //private bool FinalSunCompatFixDeclined = false;


        public override void Initialize()
        {
            base.Initialize();

            Name = "DisplayOptionsPanel";

            var lblIngameResolution = new XNALabel(WindowManager);
            lblIngameResolution.Name = nameof(lblIngameResolution);
            lblIngameResolution.ClientRectangle = new Rectangle(12, 14, 0, 0);
            lblIngameResolution.Text = "In-game Resolution:".L10N("Client:DTAConfig:InGameResolution");

            ddIngameResolution = new XNAClientDropDown(WindowManager);
            ddIngameResolution.Name = nameof(ddIngameResolution);
            ddIngameResolution.ClientRectangle = new Rectangle(
                lblIngameResolution.Right + 12,
                lblIngameResolution.Y - 2, 120, 19);

            // Add in-game resolutions
            {
                var maximumIngameResolution = new ScreenResolution(ClientConfiguration.Instance.MaximumIngameWidth, ClientConfiguration.Instance.MaximumIngameHeight);

#if XNA
                if (!ScreenResolution.HiDefLimitResolution.Fits(maximumIngameResolution))
                    maximumIngameResolution = ScreenResolution.HiDefLimitResolution;
#endif

                SortedSet<ScreenResolution> resolutions = ScreenResolution.GetFullScreenResolutions(
                    ClientConfiguration.Instance.MinimumIngameWidth, ClientConfiguration.Instance.MinimumIngameHeight,
                    maximumIngameResolution.Width, maximumIngameResolution.Height);

                foreach (var res in resolutions)
                    ddIngameResolution.AddItem(res.ToString());
            }

            var lblDetailLevel = new XNALabel(WindowManager);
            lblDetailLevel.Name = nameof(lblDetailLevel);
            lblDetailLevel.ClientRectangle = new Rectangle(lblIngameResolution.X,
                ddIngameResolution.Bottom + 16, 0, 0);
            lblDetailLevel.Text = "Detail Level:".L10N("Client:DTAConfig:DetailLevel");

            ddDetailLevel = new XNAClientDropDown(WindowManager);
            ddDetailLevel.Name = nameof(ddDetailLevel);
            ddDetailLevel.ClientRectangle = new Rectangle(
                ddIngameResolution.X,
                lblDetailLevel.Y - 2,
                ddIngameResolution.Width,
                ddIngameResolution.Height);
            ddDetailLevel.AddItem("Low".L10N("Client:DTAConfig:DetailLevelLow"));
            ddDetailLevel.AddItem("Medium".L10N("Client:DTAConfig:DetailLevelMedium"));
            ddDetailLevel.AddItem("High".L10N("Client:DTAConfig:DetailLevelHigh"));

            var lblRenderer = new XNALabel(WindowManager);
            lblRenderer.Name = nameof(lblRenderer);
            lblRenderer.ClientRectangle = new Rectangle(lblDetailLevel.X,
                ddDetailLevel.Bottom + 16, 0, 0);
            lblRenderer.Text = "Renderer:".L10N("Client:DTAConfig:Renderer");

            ddRenderer = new XNAClientDropDown(WindowManager);
            ddRenderer.Name = nameof(ddRenderer);
            ddRenderer.ClientRectangle = new Rectangle(
                ddDetailLevel.X,
                lblRenderer.Y - 2,
                ddDetailLevel.Width,
                ddDetailLevel.Height);

            foreach (var renderer in directDrawWrapperManager.GetRenderers(ClientConfiguration.Instance.GetOperatingSystemVersion()))
            {
                ddRenderer.AddItem(new XNADropDownItem()
                {
                    Text = renderer.UIName,
                    Tag = renderer
                });
            }

            chkWindowedMode = new XNAClientCheckBox(WindowManager);
            chkWindowedMode.Name = nameof(chkWindowedMode);
            chkWindowedMode.ClientRectangle = new Rectangle(lblDetailLevel.X,
                ddRenderer.Bottom + 16, 0, 0);
            chkWindowedMode.Text = "Windowed Mode".L10N("Client:DTAConfig:WindowedMode");
            chkWindowedMode.CheckedChanged += ChkWindowedMode_CheckedChanged;

            chkBorderlessWindowedMode = new XNAClientCheckBox(WindowManager);
            chkBorderlessWindowedMode.Name = nameof(chkBorderlessWindowedMode);
            chkBorderlessWindowedMode.ClientRectangle = new Rectangle(
                chkWindowedMode.X + 50,
                chkWindowedMode.Bottom + 24, 0, 0);
            chkBorderlessWindowedMode.Text = "Borderless Windowed Mode".L10N("Client:DTAConfig:BorderlessWindowedMode");
            chkBorderlessWindowedMode.AllowChecking = false;

            chkBackBufferInVRAM = new XNAClientCheckBox(WindowManager);
            chkBackBufferInVRAM.Name = nameof(chkBackBufferInVRAM);
            chkBackBufferInVRAM.ClientRectangle = new Rectangle(
                lblDetailLevel.X,
                chkBorderlessWindowedMode.Bottom + 28, 0, 0);
            chkBackBufferInVRAM.Text = ("Back Buffer in Video Memory\n(lower performance, but is\nnecessary on some systems)").L10N("Client:DTAConfig:BackBuffer");

            var lblClientResolution = new XNALabel(WindowManager);
            lblClientResolution.Name = nameof(lblClientResolution);
            lblClientResolution.ClientRectangle = new Rectangle(
                285, 14, 0, 0);
            lblClientResolution.Text = "Client Resolution:".L10N("Client:DTAConfig:ClientResolution");

            ddClientResolution = new XNAClientPreferredItemDropDown(WindowManager);
            ddClientResolution.Name = nameof(ddClientResolution);
            ddClientResolution.ClientRectangle = new Rectangle(
                lblClientResolution.Right + 12,
                lblClientResolution.Y - 2,
                Width - (lblClientResolution.Right + 24),
                ddIngameResolution.Height);
            ddClientResolution.AllowDropDown = false;
            ddClientResolution.PreferredItemLabel = "(recommended)".L10N("Client:DTAConfig:Recommended");

            // Add client resolutions
            {
                SortedSet<ScreenResolution> scaledRecommendedResolutions = ScreenResolution.GetRecommendedResolutions();

                SortedSet<ScreenResolution> resolutions = [
                    .. ScreenResolution.GetFullScreenResolutions(minWidth: 800, minHeight: 600),
                    .. ScreenResolution.GetWindowedResolutions(minWidth: 800, minHeight: 600),
                    .. scaledRecommendedResolutions,
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
            chkBorderlessClient.Name = nameof(chkBorderlessClient);
            chkBorderlessClient.ClientRectangle = new Rectangle(
                lblClientResolution.X,
                lblDetailLevel.Y, 0, 0);
            chkBorderlessClient.Text = "Fullscreen Client".L10N("Client:DTAConfig:FullscreenClient");
            chkBorderlessClient.CheckedChanged += ChkBorderlessMenu_CheckedChanged;
            chkBorderlessClient.Checked = true;

            chkIntegerScaledClient = new XNAClientCheckBox(WindowManager);
            chkIntegerScaledClient.Name = nameof(chkIntegerScaledClient);
            chkIntegerScaledClient.ClientRectangle = new Rectangle(
                lblClientResolution.X,
                lblRenderer.Y, 0, 0);
            chkIntegerScaledClient.Text = "Integer Scaled Client".L10N("Client:DTAConfig:IntegerScaledClient");
            chkIntegerScaledClient.Checked = IniSettings.IntegerScaledClient.Value;
            chkIntegerScaledClient.ToolTipText =
                """
                Enable integer scaling for the client. This will cause the client to use
                the closest fitting resolution that is required to maintain sharp graphics,
                at the expense of black borders that may appear at some resolutions.

                Additionally, enabling this option will also allow the client window 
                to be resized (does not affect the selected client resolution).
                """
                .L10N("Client:DTAConfig:IntegerScaledClientToolTip");

            var lblClientTheme = new XNALabel(WindowManager);
            lblClientTheme.Name = nameof(lblClientTheme);
            lblClientTheme.ClientRectangle = new Rectangle(
                lblClientResolution.X,
                chkWindowedMode.Y, 0, 0);
            lblClientTheme.Text = "Client Theme:".L10N("Client:DTAConfig:ClientTheme");

            ddClientTheme = new XNAClientDropDown(WindowManager);
            ddClientTheme.Name = nameof(ddClientTheme);
            ddClientTheme.ClientRectangle = new Rectangle(
                ddClientResolution.X,
                chkWindowedMode.Y,
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

            if (ClientConfiguration.Instance.ClientGameType == ClientType.TS && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AddCompatibilityFixControls();
            }

            AddChild(chkWindowedMode);
            AddChild(chkBorderlessWindowedMode);
            AddChild(chkBackBufferInVRAM);
            AddChild(chkBorderlessClient);
            AddChild(chkIntegerScaledClient);
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

        [SupportedOSPlatform("windows")]
        private void AddCompatibilityFixControls()
        {
            lblCompatibilityFixes = new XNALabel(WindowManager);
            lblCompatibilityFixes.Name = "lblCompatibilityFixes";
            lblCompatibilityFixes.FontIndex = 1;
            lblCompatibilityFixes.Text = "Legacy Compatibility Fixes:";
            AddChild(lblCompatibilityFixes);
            lblCompatibilityFixes.CenterOnParent();
            lblCompatibilityFixes.Y = Height - 97;

            lblGameCompatibilityFix = new XNALabel(WindowManager);
            lblGameCompatibilityFix.Name = "lblGameCompatibilityFix";
            lblGameCompatibilityFix.ClientRectangle = new Rectangle(132,
                lblCompatibilityFixes.Bottom + 20, 0, 0);
            lblGameCompatibilityFix.Text = "DTA/TI/TS Compatibility Fix:";

            btnGameCompatibilityFix = new XNAClientButton(WindowManager);
            btnGameCompatibilityFix.Name = "btnGameCompatibilityFix";
            btnGameCompatibilityFix.ClientRectangle = new Rectangle(
                lblGameCompatibilityFix.Right + 20,
                lblGameCompatibilityFix.Y - 4, 133, 23);
            btnGameCompatibilityFix.FontIndex = 1;
            btnGameCompatibilityFix.Text = "Disable";
            btnGameCompatibilityFix.LeftClick += BtnGameCompatibilityFix_LeftClick;

            lblMapEditorCompatibilityFix = new XNALabel(WindowManager);
            lblMapEditorCompatibilityFix.Name = "lblMapEditorCompatibilityFix";
            lblMapEditorCompatibilityFix.ClientRectangle = new Rectangle(
                lblGameCompatibilityFix.X,
                lblGameCompatibilityFix.Bottom + 20, 0, 0);
            lblMapEditorCompatibilityFix.Text = "FinalSun Compatibility Fix:";

            btnMapEditorCompatibilityFix = new XNAClientButton(WindowManager);
            btnMapEditorCompatibilityFix.Name = "btnMapEditorCompatibilityFix";
            btnMapEditorCompatibilityFix.ClientRectangle = new Rectangle(
                btnGameCompatibilityFix.X,
                lblMapEditorCompatibilityFix.Y - 4,
                btnGameCompatibilityFix.Width,
                btnGameCompatibilityFix.Height);
            btnMapEditorCompatibilityFix.FontIndex = 1;
            btnMapEditorCompatibilityFix.Text = "Disable";
            btnMapEditorCompatibilityFix.LeftClick += BtnMapEditorCompatibilityFix_LeftClick;

            AddChild(lblGameCompatibilityFix);
            AddChild(btnGameCompatibilityFix);
            AddChild(lblMapEditorCompatibilityFix);
            AddChild(btnMapEditorCompatibilityFix);

            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Tiberian Sun Client");

            if (regKey == null)
                return;

            object tsCompatFixValue = regKey.GetValue("TSCompatFixInstalled", "No");
            string tsCompatFixString = (string)tsCompatFixValue;

            if (tsCompatFixString == "Yes")
            {
                GameCompatFixInstalled = true;
            }

            object fsCompatFixValue = regKey.GetValue("FSCompatFixInstalled", "No");
            string fsCompatFixString = (string)fsCompatFixValue;

            if (fsCompatFixString == "Yes")
            {
                FinalSunCompatFixInstalled = true;
            }

            // These compatibility fixes from 2015 are no longer necessary on modern systems.
            // They are only offered for uninstallation; if they are not installed, hide them.
            if (!FinalSunCompatFixInstalled)
            {
                lblMapEditorCompatibilityFix.Disable();
                btnMapEditorCompatibilityFix.Disable();
            }

            if (!GameCompatFixInstalled)
            {
                lblGameCompatibilityFix.Disable();
                btnGameCompatibilityFix.Disable();
            }

            if (!FinalSunCompatFixInstalled && !GameCompatFixInstalled)
            {
                lblCompatibilityFixes.Disable();
            }
        }

        /// <summary>
        /// Asks the user whether they want to install the DTA/TI/TS compatibility fix.
        /// </summary>
        public void PostInit()
        {
            Load();
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

                    GameCompatFixInstalled = false;

                    lblGameCompatibilityFix.Disable();
                    btnGameCompatibilityFix.Disable();

                    if (!FinalSunCompatFixInstalled)
                        lblCompatibilityFixes.Disable();
                }
                catch (Exception ex)
                {
                    Logger.Log("Uninstalling DTA/TI/TS Compatibility Fix failed. Error message: " + ex.ToString());
                    XNAMessageBox.Show(WindowManager, "Uninstalling Compatibility Fix Failed".L10N("Client:DTAConfig:TSFixUninstallFailTitle"),
                        "Uninstalling DTA/TI/TS Compatibility Fix failed. Returned error:".L10N("Client:DTAConfig:TSFixUninstallFailText") + " " + ex.Message);
                }

                return;
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

                    lblMapEditorCompatibilityFix.Disable();
                    btnMapEditorCompatibilityFix.Disable();

                    if (!GameCompatFixInstalled)
                        lblCompatibilityFixes.Disable();
                }
                catch (Exception ex)
                {
                    Logger.Log("Uninstalling FinalSun Compatibility Fix failed. Error message: " + ex.ToString());
                    XNAMessageBox.Show(WindowManager, "Uninstalling Compatibility Fix Failed".L10N("Client:DTAConfig:TSFinalSunFixUninstallFailedTitle"),
                        "Uninstalling FinalSun Compatibility Fix failed. Error message:".L10N("Client:DTAConfig:TSFinalSunFixUninstallFailedText") + " " + ex.Message);
                }

                return;
            }
        }

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
                           r => ((DirectDrawWrapper)r.Tag).InternalName == directDrawWrapperManager.SelectedRenderer.InternalName);

            if (index < 0 && directDrawWrapperManager.SelectedRenderer.Hidden)
            {
                ddRenderer.AddItem(new XNADropDownItem()
                {
                    Text = directDrawWrapperManager.SelectedRenderer.UIName,
                    Tag = directDrawWrapperManager.SelectedRenderer
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

            if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
            {
                chkBackBufferInVRAM.Checked = !UserINISettings.Instance.BackBufferInVRAM;
            }
            else
            {
                chkBackBufferInVRAM.Checked = UserINISettings.Instance.BackBufferInVRAM;
            }
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

            var newSelectedRenderer = (DirectDrawWrapper)ddRenderer.SelectedItem.Tag;
            bool isChangingRenderer = newSelectedRenderer != directDrawWrapperManager.SelectedRenderer;

            IniSettings.WindowedMode.Value = chkWindowedMode.Checked &&
                !newSelectedRenderer.UsesCustomWindowedOption();

            IniSettings.BorderlessWindowedMode.Value = chkBorderlessWindowedMode.Checked &&
                string.IsNullOrEmpty(newSelectedRenderer.BorderlessWindowedModeKey);

            ScreenResolution clientRes = (string)ddClientResolution.SelectedItem.Tag;

            if (clientRes.Width != IniSettings.ClientResolutionX.Value ||
                clientRes.Height != IniSettings.ClientResolutionY.Value)
                restartRequired = true;

            // TODO: since DTAConfig must not rely on DXMainClient, we can't notify the client to dynamically change the resolution or togging borderless windowed mode. Thus, we need to restart the client as a workaround.

            (IniSettings.ClientResolutionX.Value, IniSettings.ClientResolutionY.Value) = clientRes;

            if (IniSettings.BorderlessWindowedClient.Value != chkBorderlessClient.Checked)
                restartRequired = true;

            IniSettings.BorderlessWindowedClient.Value = chkBorderlessClient.Checked;

            if (IniSettings.IntegerScaledClient.Value != chkIntegerScaledClient.Checked)
                restartRequired = true;

            IniSettings.IntegerScaledClient.Value = chkIntegerScaledClient.Checked;

            restartRequired = restartRequired || IniSettings.ClientTheme != (string)ddClientTheme.SelectedItem.Tag;

            IniSettings.ClientTheme.Value = (string)ddClientTheme.SelectedItem.Tag;

            restartRequired = restartRequired || !IniSettings.Translation.ToString().Equals((string)ddTranslation.SelectedItem.Tag, StringComparison.InvariantCultureIgnoreCase);

            IniSettings.Translation.Value = (string)ddTranslation.SelectedItem.Tag;

            ClientConfiguration.Instance.RefreshTranslationGameFiles();

            // copy translation files to the game directory
            foreach (var tgf in ClientConfiguration.Instance.TranslationGameFiles)
            {
                string sourcePath = SafePath.CombineFilePath(IniSettings.TranslationFolderPath, tgf.Source);
                string targetPath = SafePath.CombineFilePath(ProgramConstants.GamePath, tgf.Target);

                if (File.Exists(sourcePath))
                {
                    string sourceHash = Utilities.CalculateSHA1ForFile(sourcePath);
                    string destinationHash = Utilities.CalculateSHA1ForFile(targetPath);

                    if (sourceHash != destinationHash)
                    {
                        FileExtensions.CreateHardLinkFromSource(sourcePath, targetPath);
                        new FileInfo(targetPath).IsReadOnly = true;
                    }
                }
                else
                {
                    if (File.Exists(targetPath))
                    {
                        new FileInfo(targetPath).IsReadOnly = false;
                        File.Delete(targetPath);
                    }
                }
            }

            if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
                IniSettings.BackBufferInVRAM.Value = !chkBackBufferInVRAM.Checked;
            else
                IniSettings.BackBufferInVRAM.Value = chkBackBufferInVRAM.Checked;

            directDrawWrapperManager.Save(newSelectedRenderer);

            if (directDrawWrapperManager.SelectedRenderer.UsesCustomWindowedOption())
            {
                IniFile rendererSettingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, directDrawWrapperManager.SelectedRenderer.ConfigFileName));

                rendererSettingsIni.SetBooleanValue(directDrawWrapperManager.SelectedRenderer.WindowedModeSection,
                    directDrawWrapperManager.SelectedRenderer.WindowedModeKey, chkWindowedMode.Checked);

                if (!string.IsNullOrEmpty(directDrawWrapperManager.SelectedRenderer.BorderlessWindowedModeKey))
                {
                    bool borderlessModeIniValue = chkBorderlessWindowedMode.Checked;
                    if (directDrawWrapperManager.SelectedRenderer.IsBorderlessWindowedModeKeyReversed)
                        borderlessModeIniValue = !borderlessModeIniValue;

                    rendererSettingsIni.SetBooleanValue(directDrawWrapperManager.SelectedRenderer.WindowedModeSection,
                        directDrawWrapperManager.SelectedRenderer.BorderlessWindowedModeKey, borderlessModeIniValue);
                }

                rendererSettingsIni.WriteIniFile();
            }

            if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
            {
                if (ClientConfiguration.Instance.CopyResolutionDependentLanguageDLL)
                {
                    string languageDllDestinationPath = SafePath.CombineFilePath(ProgramConstants.GamePath, "Language.dll");

                    FileInfo fileInfo = SafePath.GetFile(languageDllDestinationPath);
                    if (fileInfo.Exists)
                    {
                        fileInfo.IsReadOnly = false;
                        fileInfo.Delete();
                    }

                    if (ingameRes.Width >= 1024 && ingameRes.Height >= 720)
                        File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources", "language_1024x720.dll"), languageDllDestinationPath);
                    else if (ingameRes.Width >= 800 && ingameRes.Height >= 600)
                        File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources", "language_800x600.dll"), languageDllDestinationPath);
                    else
                        File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources", "language_640x480.dll"), languageDllDestinationPath);
                }
            }

#if ISWINDOWS
            // Since `CheckAndPromptFix` method might restart the client if the admin rights are required, we do this at the end of the Save() method
            if (isChangingRenderer && !directDrawWrapperManager.SelectedRenderer.IsDummy)
                DirectDrawCompatibilityChecker.CheckAndPromptFix(WindowManager);
#endif

            return restartRequired;
        }

    }
}
