#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using ClientCore.Extensions;

using ClientGUI.Extensions;

using Rampastring.Tools;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

/// <summary>
/// Shows a control only while the controls named in its <c>VisibleWhen</c> key are in the given states,
/// and hides it otherwise.
/// </summary>
/// <remarks>
/// The value is a comma-separated list of conditions that must all hold:
/// <c>chkName</c> (the check box is checked), <c>ddName=2</c> (dropdown item 2 is selected),
/// or the name of any other control (that control is shown), each optionally prefixed with <c>!</c>
/// to negate it. For example, <c>VisibleWhen=chkRA2Mode,!chkShortGame,ddGameVersion=2,lbMapList</c>.
/// </remarks>
internal sealed class VisibleWhenRule
{
    private const string KeyName = "VisibleWhen";

    private readonly XNAControl target;
    private readonly List<Func<bool>> conditions = new();

    private VisibleWhenRule(XNAControl target)
    {
        this.target = target;
    }

    /// <summary>
    /// Sets up the <c>VisibleWhen</c> keys of a window's controls. Called once all of the window's controls exist.
    /// </summary>
    /// <param name="window">The window whose controls are set up, and in which the named controls are searched.</param>
    /// <param name="iniFile">The window's INI file.</param>
    public static void ApplyToChildren(XNAControl window, IniFile iniFile)
        => ApplyToChildren(window, window, iniFile);

    private static void ApplyToChildren(XNAControl window, XNAControl parent, IniFile iniFile)
    {
        foreach (XNAControl child in parent.Children.ToList())
        {
            string value = iniFile.GetStringValue(child.Name, KeyName, string.Empty);
            if (!string.IsNullOrWhiteSpace(value))
                Create(window, child, value);

            // A nested INI window sets up its own controls from its own INI file.
            if (child is not INItializableWindow)
                ApplyToChildren(window, child, iniFile);
        }
    }

    private static void Create(XNAControl window, XNAControl target, string value)
    {
        var rule = new VisibleWhenRule(target);

        foreach (string condition in value.SplitWithCleanup())
        {
            Func<bool>? check = rule.ParseCondition(window, condition);
            if (check == null)
            {
                Logger.Log($"{target.Name}: Invalid {KeyName} condition '{condition}'. Expected the name of a control, " +
                    "or the name of a dropdown and an item number such as ddName=2, optionally prefixed with '!'.");
                continue;
            }

            rule.conditions.Add(check);
        }

        if (rule.conditions.Count > 0)
            rule.Update();
    }

    /// <summary>
    /// Parses one condition and subscribes to changes of the control it names.
    /// </summary>
    /// <returns>A function that tells whether the condition holds, or null if the condition is invalid.</returns>
    private Func<bool>? ParseCondition(XNAControl window, string condition)
    {
        bool negate = condition.StartsWith("!", StringComparison.Ordinal);
        string text = negate ? condition.Substring(1).Trim() : condition;

        int separatorIndex = text.IndexOf('=');
        string controlName = separatorIndex < 0 ? text : text.Substring(0, separatorIndex).Trim();
        XNAControl? source = window.FindChild<XNAControl>(controlName, optional: true);

        Func<bool> check;
        if (separatorIndex < 0 && source is XNACheckBox checkBox)
        {
            checkBox.CheckedChanged += (sender, e) => Update();
            check = () => checkBox.Checked;
        }
        else if (separatorIndex >= 0 && source is XNADropDown dropDown &&
            int.TryParse(text.Substring(separatorIndex + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int itemIndex))
        {
            dropDown.SelectedIndexChanged += (sender, e) => Update();
            check = () => dropDown.SelectedIndex == itemIndex;
        }
        else if (separatorIndex < 0 && source != null && source != target)
        {
            // Any other control: holds while it is shown, for example a control that the window
            // only shows in some layouts. The event is only raised on a change, so rules that
            // depend on each other settle instead of looping.
            source.VisibleChanged += (sender, e) => Update();
            check = () => source.Visible;
        }
        else
        {
            return null;
        }

        return negate ? () => !check() : check;
    }

    private void Update()
    {
        if (conditions.All(condition => condition()))
            target.Enable();
        else
            target.Disable();
    }
}
