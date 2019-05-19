using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI
{
    public class XNAWindowBase : XNAPanel
    {
        public XNAWindowBase(WindowManager windowManager) : base(windowManager)
        {
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.TILED;
        }

        /// <summary>
        /// The default GUI creator that is used if no custom GUI creator is specified.
        /// Static, because one instance of it is enough.
        /// </summary>
        private static readonly ClientGUICreator defaultGUICreator = new ClientGUICreator();

        /// <summary>
        /// The <see cref="Rampastring.XNAUI.GUICreator"/> to use for creating controls.
        /// If not specified, a default implementation is used.
        /// </summary>
        protected GUICreator CustomGUICreator { get; set; }


        /// <summary>
        /// Reads extra control information from a specific section of an INI file.
        /// </summary>
        /// <param name="iniFile">The INI file.</param>
        /// <param name="sectionName">The section.</param>
        protected virtual void ParseExtraControls(IniFile iniFile, string sectionName)
        {
            var section = iniFile.GetSection(sectionName);

            if (section == null)
                return;

            var guiCreator = CustomGUICreator ?? defaultGUICreator;

            foreach (var kvp in section.Keys)
            {
                string[] parts = kvp.Value.Split(':');
                if (parts.Length != 2)
                    throw new Exception("Invalid ExtraControl specified in " + Name + ": " + kvp.Value);

                if (!Children.Any(child => child.Name == parts[0]))
                {
                    var control = guiCreator.CreateControl(WindowManager, parts[1]);
                    control.Name = parts[0];
                    control.DrawOrder = -Children.Count;
                    AddChild(control);
                }
            }
        }

        protected virtual void ReadChildControlAttributes(IniFile iniFile)
        {
            foreach (XNAControl child in Children)
            {
                if (!(typeof(XNAWindowBase).IsAssignableFrom(child.GetType())))
                    child.GetAttributes(iniFile);
            }
        }

        /// <summary>
        /// Creates a control with a given name, using the specified GUI creator
        /// and control type name.
        /// </summary>
        /// <param name="guiCreator">The <see cref="GUICreator"/> to use.</param>
        /// <param name="controlTypeName">The name of the control's type.</param>
        /// <param name="controlName">The name of the created control.</param>
        /// <returns>The created control.</returns>
        protected virtual XNAControl CreateControl(GUICreator guiCreator, string controlTypeName, string controlName)
        {
            var control = guiCreator.CreateControl(WindowManager, controlTypeName);
            control.Name = controlName;
            control.DrawOrder = -Children.Count;
            AddChild(control);
            return control;
        }
    }
}
