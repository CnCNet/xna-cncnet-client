using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using Rampastring.Tools;
using Rampastring.XNAUI.XNAControls;

namespace ClientCore.I18N;

public class Locale : ICloneable
{
    public static Locale Instance { get; set; }

    /// <summary>The locale metadata section name.</summary>
    public const string METADATA_SECTION = "General";

    public string LocaleCode { get; private set; } = string.Empty;

    /// <summary>The explicitly set UI name for the locale.</summary>
    private string _name = string.Empty;
    /// <summary>The UI name for the locale.</summary>
    public string Name
    {
        get => string.IsNullOrWhiteSpace(_name) ? GetLocaleName(LocaleCode) : _name;
        private set => _name = value;
    }

    /// <summary>Shows the information about the author.</summary>
    public string Author { get; private set; } = string.Empty;

    /// <summary>
    /// The key stands for a label name, and the value stands for a string that is used in System.string.Format().
    /// The value can not contain IniNewLinePattern when loading or saving via ini format.
    /// </summary>
    private Dictionary<string, string> Values { get; } = new();

    // public bool IsRightToLeft { get; set; } // TODO

    /// <summary>Contains all labels with missing translations.</summary>
    private readonly HashSet<string> MissingKeys = new HashSet<string>();

    /// <summary>Used to write missing translation table entries to a file.</summary>
    public const string MISSING_KEY_PREFIX = "; ";  // a hack but hey it works

    /// <summary>Used for hardcoded strings.</summary>
    public const string CLIENT_PREFIX = "Client";
    /// <summary>Used for INI values.</summary>
    public const string INI_PREFIX = "INI";
    /// <summary>Used for parent-agnostic INI values.</summary>
    public const string GLOBAL_PREFIX = "Global";

    /// <summary>
    /// Initializes a new instance of the <see cref="Locale"/> class.
    /// </summary>
    public Locale(string localeCode)
    {
        if (string.IsNullOrWhiteSpace(localeCode))
            throw new ArgumentException($"'{nameof(localeCode)}' cannot be null or whitespace.", nameof(localeCode));

        LocaleCode = localeCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Locale"/> class
    /// that loads the locale from an ini file.
    /// </summary>
    /// <param name="ini">An ini file to be read.</param>
    public Locale(IniFile ini, string localeCode)
    {
        if (string.IsNullOrWhiteSpace(localeCode))
            throw new ArgumentException($"'{nameof(localeCode)}' cannot be null or whitespace.", nameof(localeCode));
        
        LocaleCode = localeCode;

        if (ini == null)
            throw new ArgumentNullException(nameof(ini));

        IniSection metadataSection = ini.GetSection(METADATA_SECTION);
        Name = metadataSection?.GetStringValue(nameof(Name), string.Empty);
        Author = metadataSection?.GetStringValue(nameof(Author), string.Empty);

        IniSection valuesSection = ini.GetSection(nameof(Values));
        foreach (var (label, value) in valuesSection.Keys)
            Values.Add(label, UnescapeIniValue(value));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Locale"/> class
    /// that is a copy of the given class.
    /// </summary>
    /// <param name="other">An object to copy from.</param>
    public Locale(Locale other)
    {
        LocaleCode = other.LocaleCode;
        _name = other._name;
        Author = other.Author;

        foreach (var (label, value) in other.Values)
            Values.Add(label, value);
    }

    public Locale Clone() => new Locale(this);
    object ICloneable.Clone() => Clone();

    public static Locale LoadFromIniFile(string iniPath, string localeCode)
    {
        CCIniFile iniFile = new(iniPath);
        return new Locale(iniFile, localeCode);
    }

    public static string GetLocaleName(string localeCode)
    {
        if (string.IsNullOrWhiteSpace(localeCode))
            throw new ArgumentException($"'{nameof(localeCode)}' cannot be null or whitespace.", nameof(localeCode));

        string result = null;

        string iniPath = SafePath.CombineFilePath(
            ClientConfiguration.Instance.LocalesFolderPath, localeCode, ClientConfiguration.Instance.LocaleIniName);

        if (SafePath.GetFile(iniPath).Exists)
        {
            // This parses only the metadata section content so that we don't parse
            // the bazillion of localized values just to read the locale name.
            // The only issue is that inheritance would break. Rampa pls fix IniFile
            IniFile ini = new();
            ini.AddSection(METADATA_SECTION);
            ini.FileName = iniPath;
            ini.AllowNewSections = false;

            ini.Parse();

            // Overridden name first
            IniSection metadataSection = ini.GetSection(METADATA_SECTION);
            result = metadataSection?.GetStringValue(nameof(Name), null);
        }

        if (string.IsNullOrWhiteSpace(result))
            result = new CultureInfo(localeCode).DisplayName;

        if (string.IsNullOrWhiteSpace(result))
            result = localeCode;

        return result;
    }

    /// <summary>
    /// Checks the current UI culture and finds the closest match from supported locales.
    /// </summary>
    /// <returns>Locale code.</returns>
    public static string GetDefaultLocaleCode()
    {
        Dictionary<string, string> localizations = ClientConfiguration.Instance.GetLocales();

        for (var culture = CultureInfo.CurrentUICulture;
            culture != CultureInfo.InvariantCulture;
            culture = culture.Parent)
        {
            string locale = culture.Name;

            if (localizations.ContainsKey(locale))
                return locale;
        }

        return ProgramConstants.HARDCODED_LOCALE_CODE;
    }

    /// <summary>
    /// Dump the translation table to an ini file.
    /// </summary>
    /// <returns>An ini file that contains the translation table.</returns>
    public IniFile DumpIni(bool saveOnlyMissingValues = false)
    {
        IniFile ini = new IniFile();

        ini.AddSection(METADATA_SECTION);
        IniSection general = ini.GetSection(METADATA_SECTION);

        if (!string.IsNullOrWhiteSpace(_name))
            general.AddKey(nameof(Name), _name);

        general.AddKey(nameof(Author), Author);

        ini.AddSection(nameof(Values));
        IniSection translation = ini.GetSection(nameof(Values));

        foreach (var (label, value) in Values.OrderBy(kvp => kvp.Key))
        {
            bool valueMissing = MissingKeys.Contains(label);
            if (!saveOnlyMissingValues || valueMissing)
            {
                translation.AddKey(valueMissing
                        ? MISSING_KEY_PREFIX + label
                        : label,
                    EscapeIniValue(value));
            }
        }

        return ini;
    }

    public static string EscapeIniValue(string raw)
    {
        if (raw.Contains(ProgramConstants.INI_NEWLINE_PATTERN))
            throw new Exception($"Pattern {ProgramConstants.INI_NEWLINE_PATTERN} is forbidden as this pattern is used to represent the new line.");

        if (raw.Contains(';'))
            throw new Exception("The semicolon (;) is forbidden as it is used to represent a comment line.");

        return raw
            .Replace(Environment.NewLine, "\n")
            .Replace("\n", ProgramConstants.INI_NEWLINE_PATTERN);
    }

    public static string UnescapeIniValue(string escaped)
        => escaped.Replace(ProgramConstants.INI_NEWLINE_PATTERN, Environment.NewLine);

    private bool HandleMissing(string label, string defaultValue)
    {
        if (MissingKeys.Add(label))
        {
            Values[label] = defaultValue;
            return true;
        }

        return false;
    }

    public string Localize(string label, string defaultValue, bool notify = true)
    {
        if (Values.ContainsKey(label))
        {
            return Values[label];
        }
        else
        {
            if (notify)
                HandleMissing(label, defaultValue);

            return defaultValue;
        }
    }

    public string Localize(XNAControl control, string attributeName, string defaultValue, bool notify = true)
    {
        string label = $"{INI_PREFIX}:{control.Parent?.Name ?? GLOBAL_PREFIX}:{control.Name}:{attributeName}";
        string globalLabel = $"{INI_PREFIX}:{GLOBAL_PREFIX}:{control.Name}:{attributeName}";

        string result;
        if (Values.ContainsKey(label))
        {
            result = Values[label];
        }
        else if (label != globalLabel && Values.ContainsKey(globalLabel))
        {
            result = Values[globalLabel];
        }
        else
        {
            result = defaultValue;

            if (notify)
                HandleMissing(label, defaultValue);
        }

        return result;
    }
}
