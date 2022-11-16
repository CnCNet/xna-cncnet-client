using System;

namespace Localization;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "To explicitly define all extension methods in a single file.")]
public static class StringTranslationLabelExtensions
{
    /// <summary>
    /// Mark this string to be translated, with the given label name.
    /// </summary>
    /// <param name="defaultValue">The default string value as a fallback.</param>
    /// <param name="label">The unique label name.</param>
    /// <returns>The translated string value.</returns>
    public static string L10N(this String defaultValue, string label)
        => TranslationTable.Instance.GetTableValue(label, defaultValue);
}