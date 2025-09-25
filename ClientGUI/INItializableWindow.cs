using ClientCore;
using ClientCore.I18N;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClientGUI
{
    public class INItializableWindow : XNAPanel
    {
        public INItializableWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        protected CCIniFile ConfigIni { get; private set; }

        private bool hasCloseButton = false;
        private bool _initialized = false;

        /// <summary>
        /// If not null, the client will read an INI file with this name
        /// instead of the window's name.
        /// </summary>
        protected string IniNameOverride { get; set; }

        public T FindChild<T>(string childName, bool optional = false) where T : XNAControl
        {
            T child = FindChild<T>(Children, childName);
            if (child == null && !optional)
                throw new KeyNotFoundException("Could not find required child control: " + childName);

            return child;
        }

        private T FindChild<T>(IEnumerable<XNAControl> list, string controlName) where T : XNAControl
        {
            foreach (XNAControl child in list)
            {
                if (child.Name == controlName)
                    return (T)child;

                T childOfChild = FindChild<T>(child.Children, controlName);
                if (childOfChild != null)
                    return childOfChild;
            }

            return null;
        }

        /// <summary>
        /// Attempts to locate the ini config file for the current control.
        /// Only return a config path if it exists.
        /// </summary>
        /// <returns>The ini config file path</returns>
        protected string GetConfigPath()
        {
            string iniFileName = string.IsNullOrWhiteSpace(IniNameOverride) ? Name : IniNameOverride;

            // get theme specific path
            FileInfo configIniPath = SafePath.GetFile(ProgramConstants.GetResourcePath(), FormattableString.Invariant($"{iniFileName}.ini"));
            if (configIniPath.Exists)
                return configIniPath.FullName;

            // get base path
            configIniPath = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), FormattableString.Invariant($"{iniFileName}.ini"));
            if (configIniPath.Exists)
                return configIniPath.FullName;

            if (iniFileName == Name)
                return null; // IniNameOverride must be null, no need to continue

            iniFileName = Name;

            // get theme specific path
            configIniPath = SafePath.GetFile(ProgramConstants.GetResourcePath(), FormattableString.Invariant($"{iniFileName}.ini"));
            if (configIniPath.Exists)
                return configIniPath.FullName;

            // get base path
            configIniPath = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), FormattableString.Invariant($"{iniFileName}.ini"));
            return configIniPath.Exists ? configIniPath.FullName : null;
        }

        public override void Initialize()
        {
            if (_initialized)
                throw new InvalidOperationException("INItializableWindow cannot be initialized twice.");

            string configIniPath = GetConfigPath();

            if (string.IsNullOrEmpty(configIniPath))
            {
                base.Initialize();
                return;
            }

            ConfigIni = new CCIniFile(configIniPath);

            if (Parser.Instance == null)
                _ = new Parser(WindowManager); // Note: Parser.Instance will be set by calling new Parser()

            Parser.Instance.SetPrimaryControl(this);
            ReadINIForControl(this);
            ReadLateAttributesForControl(this);

            ParseExtraControls();

            base.Initialize();

            _initialized = true;
        }

        private void ParseExtraControls()
        {
            var section = ConfigIni.GetSection("$ExtraControls");

            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (!kvp.Key.StartsWith("$CC"))
                    continue;

                string[] parts = kvp.Value.Split(':');
                if (parts.Length != 2)
                    throw new ClientConfigurationException("Invalid $ExtraControl specified in " + Name + ": " + kvp.Value);

                if (!Children.Any(child => child.Name == parts[0]))
                {
                    var control = CreateChildControl(this, kvp.Value);
                    control.Name = parts[0];
                    control.DrawOrder = -Children.Count;
                    ReadINIForControl(control);
                }
            }
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "HasCloseButton")
                hasCloseButton = iniFile.GetBooleanValue(Name, key, hasCloseButton);

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        protected void ReadINIForControl(XNAControl control)
        {
            var section = ConfigIni.GetSection(control.Name);
            if (section == null)
                return;

            Parser.Instance.SetPrimaryControl(this);

            // shorthand for localization function
            static string Localize(XNAControl control, string attributeName, string defaultValue, bool notify = true)
                => Translation.Instance.LookUp(control, attributeName, defaultValue, notify);

            foreach (var kvp in section.Keys)
            {
                if (kvp.Key.StartsWith("$CC"))
                {
                    var child = CreateChildControl(control, kvp.Value);
                    ReadINIForControl(child);
                    child.Initialize();

                    if (child is ICompositeControl composite)
                    {
                        foreach (var sc in composite.SubControls)
                        {
                            ReadINIForControl(sc);
                            sc.Initialize();
                        }
                    }
                }
                else if (kvp.Key == "$X")
                {
                    control.X = Parser.Instance.GetExprValue(
                        Localize(control, kvp.Key, kvp.Value, notify: false), control);
                }
                else if (kvp.Key == "$Y")
                {
                    control.Y = Parser.Instance.GetExprValue(
                        Localize(control, kvp.Key, kvp.Value, notify: false), control);
                }
                else if (kvp.Key == "$Width")
                {
                    control.Width = Parser.Instance.GetExprValue(
                        Localize(control, kvp.Key, kvp.Value, notify: false), control);
                }
                else if (kvp.Key == "$Height")
                {
                    control.Height = Parser.Instance.GetExprValue(
                        Localize(control, kvp.Key, kvp.Value, notify: false), control);
                }
                else if (kvp.Key == "$TextAnchor" && control is XNALabel)
                {
                    // TODO refactor these to be more object-oriented
                    ((XNALabel)control).TextAnchor = (LabelTextAnchorInfo)Enum.Parse(typeof(LabelTextAnchorInfo), kvp.Value);
                }
                else if (kvp.Key == "$AnchorPoint" && control is XNALabel)
                {
                    string[] parts = kvp.Value.Split(',');
                    if (parts.Length != 2)
                        throw new FormatException("Invalid format for AnchorPoint: " + kvp.Value);
                    ((XNALabel)control).AnchorPoint = new Vector2(Parser.Instance.GetExprValue(parts[0], control), Parser.Instance.GetExprValue(parts[1], control));
                }
                else if (kvp.Key == "$LeftClickAction")
                {
                    if (kvp.Value == "Disable")
                        control.LeftClick += (s, e) => Disable();
                }
                else
                {
                    control.ParseINIAttribute(ConfigIni, kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Reads a second set of attributes for a control's child controls.
        /// Enables linking controls to controls that are defined after them.
        /// </summary>
        private void ReadLateAttributesForControl(XNAControl control)
        {
            var section = ConfigIni.GetSection(control.Name);
            if (section == null)
                return;

            var children = Children.ToList();
            foreach (var child in children)
            {
                // This logic should also be enabled for other types in the future,
                // but it requires changes in XNAUI
                if (!(child is XNATextBox))
                    continue;

                var childSection = ConfigIni.GetSection(child.Name);
                if (childSection == null)
                    continue;

                string nextControl = childSection.GetStringValue("NextControl", null);
                if (!string.IsNullOrWhiteSpace(nextControl))
                {
                    var otherChild = children.Find(c => c.Name == nextControl);
                    if (otherChild != null)
                        ((XNATextBox)child).NextControl = otherChild;
                }

                string previousControl = childSection.GetStringValue("PreviousControl", null);
                if (!string.IsNullOrWhiteSpace(previousControl))
                {
                    var otherChild = children.Find(c => c.Name == previousControl);
                    if (otherChild != null)
                        ((XNATextBox)child).PreviousControl = otherChild;
                }
            }
        }

        private XNAControl CreateChildControl(XNAControl parent, string keyValue)
        {
            string[] parts = keyValue.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                throw new INIConfigException("Invalid child control definition " + keyValue);

            string childName = parts[0];
            if (string.IsNullOrEmpty(childName))
                throw new INIConfigException("Empty name in child control definition for " + parent.Name);

            XNAControl childControl = ClientGUICreator.GetXnaControl(parts[1]);

            if (Array.Exists(childName.ToCharArray(), c => !char.IsLetterOrDigit(c) && c != '_'))
                throw new INIConfigException("Names of INItializableWindow child controls must consist of letters, digits and underscores only. Offending name: " + parts[0]);

            childControl.Name = childName;
            parent.AddChildWithoutInitialize(childControl);
            return childControl;
        }
    }
}
