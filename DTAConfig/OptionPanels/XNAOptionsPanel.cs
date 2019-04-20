using ClientCore;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System.Collections.Generic;

namespace DTAConfig.OptionPanels
{
    /// <summary>
    /// A base class for all option panels.
    /// Handles custom game-specific panel options
    /// defined in INI files.
    /// </summary>
    internal abstract class XNAOptionsPanel : XNAWindowBase
    {
        public XNAOptionsPanel(WindowManager windowManager, 
            UserINISettings iniSettings) : base(windowManager)
        {
            IniSettings = iniSettings;
            CustomGUICreator = optionsGUICreator;
        }

        private static readonly OptionsGUICreator optionsGUICreator = new OptionsGUICreator();

        private List<FileSettingCheckBox> fileSettingCheckBoxes = new List<FileSettingCheckBox>();

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(12, 47,
                Parent.Width - 24,
                Parent.Height - 94);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            base.Initialize();
        }

        /// <summary>
        /// Parses user-defined game options from an INI file.
        /// </summary>
        /// <param name="iniFile">The INI file.</param>
        public void ParseUserOptions(IniFile iniFile)
        {
            ParseExtraControls(iniFile, Name + "ExtraControls");
            ReadChildControlAttributes(iniFile);
        }

        protected override void ParseExtraControls(IniFile iniFile, string sectionName)
        {
            base.ParseExtraControls(iniFile, sectionName);

            foreach (var control in Children)
            {
                if (!(control is FileSettingCheckBox controlAsFileSettingCheckBox))
                    continue;

                fileSettingCheckBoxes.Add(controlAsFileSettingCheckBox);
            }
        }

        protected UserINISettings IniSettings { get; private set; }

        /// <summary>
        /// Saves the options of this panel.
        /// Returns a bool that determines whether the 
        /// client needs to restart for changes to apply.
        /// </summary>
        public virtual bool Save()
        {
            foreach (var checkBox in fileSettingCheckBoxes)
                checkBox.Save();

            return false;
        }

        /// <summary>
        /// Loads the options of this panel.
        /// </summary>
        public virtual void Load()
        {
            foreach (var checkBox in fileSettingCheckBoxes)
                checkBox.Load();
        }
    }
}
