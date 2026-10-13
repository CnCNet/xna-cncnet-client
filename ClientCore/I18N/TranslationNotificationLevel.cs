#nullable enable
namespace ClientCore.I18N;

/// <summary>
/// Controls which missing translation keys are added to the generated translation stub.
/// </summary>
public enum TranslationNotificationLevel : int
{
    /// <summary>
    /// Record default localizable values.
    /// </summary>
    Default = 1,

    /// <summary>
    /// Record additional low-priority values.
    /// </summary>
    Verbose = 2
}
