#nullable enable
using System;
using System.Collections.Generic;

using Rampastring.XNAUI.XNAControls;

namespace ClientGUI.Extensions;

/// <summary>
/// Contains extension methods for <see cref="XNAControl"/>.
/// </summary>
public static class XNAControlExtensions
{
    /// <summary>
    /// Checks if any child control in the given list matches the specified condition.
    /// </summary>
    /// <param name="list">The list of child controls to check.</param>
    /// <param name="isTargetControl">The condition to check against each child control.</param>
    /// <param name="recursive">Indicates whether to check child controls recursively.</param>
    /// <returns></returns>
    private static bool AnyChildMatches(IEnumerable<XNAControl> list, Func<XNAControl, bool> isTargetControl, bool recursive)
    {
        foreach (XNAControl child in list)
        {
            bool matched = isTargetControl(child);

            if (matched)
                return true;

            if (recursive)
            {
                matched = AnyChildMatches(child.Children, isTargetControl, recursive);
                if (matched)
                    return true;
            }
        }

        return false;
    }

    extension(XNAControl thisControl)
    {
        /// <summary>
        /// Finds a child control by its name.
        /// </summary>
        /// <typeparam name="T">Type of the child control to find.</typeparam>
        /// <param name="childName">Name of the child control to find.</param>
        /// <param name="comparisonType">The string comparison type to use when matching the prefix.</param>
        /// <param name="optional">Indicates whether the child control is optional. On true, the method will return null if the child is not found. On false, the method will throw if the child is not found.</param>
        /// <param name="recursive">Indicates whether to check child controls recursively.</param>
        /// <returns>Child control if found, otherwise type default value.</returns>
        public T? FindChild<T>(string childName, StringComparison comparisonType = StringComparison.Ordinal, bool optional = false, bool recursive = true) where T : XNAControl
        {
            XNAControl? result = null;

            AnyChildMatches(new List<XNAControl>() { thisControl }, control =>
            {
                if (!childName.Equals(control.Name, comparisonType))
                    return false;

                result = control;
                return true;
            }, recursive: recursive);

            if (result == null && !optional)
                throw new KeyNotFoundException("Could not find required child control: " + childName);

            return (T?)result;
        }

        /// <summary>
        /// Finds all child controls whose names start with the specified prefix.
        /// </summary>
        /// <typeparam name="T">The type of the child controls to find.</typeparam>
        /// <param name="prefix">The prefix to match.</param>
        /// <param name="comparisonType">The string comparison type to use when matching the prefix.</param>
        /// <param name="recursive">Indicates whether to check child controls recursively.</param>
        /// <returns>A list of child controls whose names start with the specified prefix.</returns>
        public List<T> FindChildrenStartWith<T>(string prefix, StringComparison comparisonType = StringComparison.Ordinal, bool recursive = true) where T : XNAControl
        {
            List<T> result = new List<T>();

            AnyChildMatches(new List<XNAControl>() { thisControl }, control =>
            {
                if (string.IsNullOrEmpty(prefix) ||
                    !string.IsNullOrEmpty(control.Name) && control.Name.StartsWith(prefix, comparisonType))
                    result.Add((T)control);

                return false;
            }, recursive: recursive);

            return result;
        }
    }

    /// <summary>
    /// Finds control's parent window (instance of XNAWindow or INItializableWindow)
    /// </summary>
    /// <param name="control">Control to find the parent window for.</param>
    /// <returns>Control's parent window if found, otherwise null</returns>
    public static XNAControl? FindParentWindow(this XNAControl control)
    {
        if (control == null || control.Parent == null)
            return null;

        if (control.Parent is INItializableWindow or XNAWindow)
            return control.Parent;

        return control.Parent.FindParentWindow();
    }
}
