using ClientCore.I18N;

using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

public static class TranslationGUIExtensions
{
    /// <summary>
    /// Looks up the translated value that corresponds to the given INI-defined control attribute.
    /// </summary>
    /// <param name="control">The control to look up the attribute value for.</param>
    /// <param name="attributeName">The attribute name as written in the INI.</param>
    /// <param name="defaultValue">The value to fall back to in case there's no translated value.</param>
    /// <param name="notificationLevel">The notification level of this key-value pair.</param>
    /// <returns>The translated value or a default value.</returns>
    public static string LookUp(this Translation @this, XNAControl control, string attributeName, string defaultValue,
        TranslationNotificationLevel notificationLevel = TranslationNotificationLevel.Default)
    {
        string key = $"INI:Controls:{control.Parent?.Name ?? "Global"}:{control.Name}:{attributeName}";
        string globalKey = $"INI:Controls:Global:{control.Name}:{attributeName}";

        return @this.LookUp(key, fallbackKey: globalKey, defaultValue, notificationLevel);
    }
}