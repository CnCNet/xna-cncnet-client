using System;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

/// <summary>
/// Contains helper methods for UI / UI control-related functionality
/// </summary>
public static class UIHelpers
{
    /// <summary>
    /// Finds a child control matching a name and optionally a type.
    /// </summary>
    /// <typeparam name="T">Type of the child control to find.</typeparam>
    /// <param name="parent">Parent control.</param>
    /// <param name="controlName">Name of the child control.</param>
    /// <param name="recursive">Whether or not to look for children recursively.</param>
    /// <returns>Child control matching the given name if found, otherwise type default value.</returns>
    public static T FindMatchingChild<T>(XNAControl parent, string controlName, bool recursive = false)
    {
        if (parent == null || string.IsNullOrEmpty(controlName))
            return default;

        foreach (var child in parent.Children)
        {
            if (controlName.Equals(child.Name, StringComparison.Ordinal) && child is T returnValue)
            {
                return returnValue;
            }
            else if (recursive)
            {
                var match = FindMatchingChild<T>(child, controlName, recursive);

                if (match != null && child is T)
                    return match;
            }
        }

        return default;
    }

    /// <summary>
    /// Finds control's parent window (instance of XNAWindow or INItializableWindow)
    /// </summary>
    /// <param name="control">Control to find the parent window for.</param>
    /// <returns>Control's parent window if found, otherwise null</returns>
    public static XNAControl FindParentWindow(XNAControl control)
    {
        if (control == null || control.Parent == null)
            return null;

        if (control.Parent is INItializableWindow or XNAWindow)
            return control.Parent;

        return FindParentWindow(control.Parent);
    }
}