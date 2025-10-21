using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using ClientCore.Extensions;
using ClientCore.PlatformShim;

using Rampastring.Tools;

namespace ClientCore.I18N;

public class Translation : ICloneable
{
    public static Translation Instance { get; set; } = new Translation(ProgramConstants.HARDCODED_LOCALE_CODE);

    /// <summary>The translation metadata section name.</summary>
    public const string METADATA_SECTION = "General";

    private static CultureInfo _initialUICulture;
    /// <summary>The UI culture that the application was started with. Must be initialized as early as possible.</summary>
    public static CultureInfo InitialUICulture
    {
        get => _initialUICulture;
        set => _initialUICulture = _initialUICulture is null ? value
            : throw new InvalidOperationException($"{nameof(InitialUICulture)} is already set!");
    }

    /// <summary>AKA name of the folder, used to look up <see cref="Culture"/> and select a language</summary>
    public string LocaleCode { get; private set; } = string.Empty;

    /// <summary>The explicitly set UI name for the translation.</summary>
    private string _name = string.Empty;
    /// <summary>The UI name for the translation.</summary>
    public string Name
    {
        get => string.IsNullOrWhiteSpace(_name) ? GetLanguageName(LocaleCode) : _name;
        private set => _name = value;
    }

    /// <summary>The explicitly set UI culture for the translation.</summary>
    /// <remarks>Not accounted when selecting the translation automatically.</remarks>
    private CultureInfo _culture;
    /// <summary>The UI culture for the translation.</summary>
    public CultureInfo Culture
    {
        get => _culture is null ? new CultureInfo(LocaleCode) : _culture;
        private set => _culture = value;
    }

    /// <summary>The author(s) of the translation.</summary>
    public string Author { get; private set; } = string.Empty;

    /// <summary>Override the default encoding used for reading/writing map files. Null ("Auto") means detecting the encoding from each file (sometimes unreliable). </summary>
    public Encoding MapEncoding = EncodingExt.UTF8NoBOM;

    /// <summary>Stores the translation values (including default values for missing strings).</summary>
    private Dictionary<string, string> Values { get; } = new();

    // public bool IsRightToLeft { get; set; } // TODO

    /// <summary>Contains all keys within <see cref="Values"/> with missing translations.</summary>
    private readonly HashSet<string> MissingKeys = new();

    /// <summary>Used to write missing translation table entries to a file.</summary>
    public const string MISSING_KEY_PREFIX = "; ";  // a hack but hey it works

    /// <summary>
    /// Initializes a new instance of the <see cref="Translation"/> class.
    /// </summary>
    /// <param name="localeCode">A locale code for this translation.</param>
    public Translation(string localeCode)
    {
        LocaleCode = localeCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Translation"/> class
    /// that loads the translation from an INI file.
    /// </summary>
    /// <param name="ini">An INI file to read from.</param>
    /// <param name="localeCode">A locale code for this translation.</param>
    public Translation(IniFile ini, string localeCode)
        : this(localeCode)
    {
        if (ini is null)
            throw new ArgumentNullException(nameof(ini));

        IniSection metadataSection = ini.GetSection(METADATA_SECTION);
        Name = metadataSection?.GetStringValue(nameof(Name), string.Empty);
        Author = metadataSection?.GetStringValue(nameof(Author), string.Empty);

        MapEncoding = EncodingExt.GetEncodingWithAuto(metadataSection?.GetStringValue(nameof(MapEncoding), null));

        string cultureName = metadataSection?.GetStringValue(nameof(Culture), null);
        if (cultureName is not null)
            Culture = new(cultureName);

        AppendValuesFromIniFile(ini);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Translation"/> class
    /// that loads the translation from an INI file.
    /// </summary>
    /// <param name="iniPath">A path to an INI file to read from.</param>
    /// <param name="localeCode">A locale code for this translation.</param>
    public Translation(string iniPath, string localeCode)
        : this(new CCIniFile(iniPath), localeCode) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Translation"/> class
    /// that is a copy of the given instance.
    /// </summary>
    /// <param name="other">An object to copy from.</param>
    public Translation(Translation other)
    {
        LocaleCode = other.LocaleCode;
        _name = other._name;
        _culture = other._culture;
        Author = other.Author;
        MapEncoding = other.MapEncoding;

        foreach (var (key, value) in other.Values)
            Values.Add(key, value);
    }

    public Translation Clone() => new Translation(this);
    object ICloneable.Clone() => Clone();

    /// <summary>
    /// Reads <see cref="Values"/> from an INI file, overriding possibly existing ones.
    /// </summary>
    /// <param name="iniPath">A path to an INI file to read from.</param>
    public void AppendValuesFromIniFile(string iniPath)
        => AppendValuesFromIniFile(new CCIniFile(iniPath));

    /// <summary>
    /// Reads <see cref="Values"/> from an INI file, overriding possibly existing ones.
    /// </summary>
    /// <param name="ini">An INI file to read from.</param>
    public void AppendValuesFromIniFile(IniFile ini)
    {
        IniSection valuesSection = ini.GetSection(nameof(Values));
        foreach (var (key, value) in valuesSection.Keys)
            Values[key] = value.FromIniString();
    }

    /// <param name="localeCode">The locale code to look up the language name for.</param>
    /// <returns>The language name for the given locale code.</returns>
    public static string GetLanguageName(string localeCode)
    {
        string result = null;

        string iniPath = SafePath.CombineFilePath(
            ClientConfiguration.Instance.TranslationsFolderPath, localeCode, ClientConfiguration.Instance.TranslationIniName);

        if (SafePath.GetFile(iniPath).Exists)
        {
            // This parses only the metadata section content so that we don't parse
            // the bazillion of localized values just to read the translation name.
            // The only issue is that inheritance would break.
            // FIXME AllowNewSections is ignored with inheritance
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
            result = new CultureInfo(localeCode).NativeName;

        if (string.IsNullOrWhiteSpace(result))
            result = localeCode;

        return result;
    }

    /// <summary>
    /// Lists valid available translations from the <see cref="TranslationsFolderPath"/> along with their UI names.
    /// A localization is valid if it has a corresponding <see cref="TranslationIniName"/> file in the <see cref="TranslationsFolderPath"/>.
    /// </summary>
    /// <returns>Locale code -> display name pairs.</returns>
    public static Dictionary<string, string> GetTranslations()
    {
        var translations = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            // Add default localization so that we always have it in the list even if the localization does not exist
            [ProgramConstants.HARDCODED_LOCALE_CODE] = GetLanguageName(ProgramConstants.HARDCODED_LOCALE_CODE)
        };

        if (!Directory.Exists(ClientConfiguration.Instance.TranslationsFolderPath))
            return translations;

        foreach (var localizationFolder in Directory.GetDirectories(ClientConfiguration.Instance.TranslationsFolderPath))
        {
            string localizationCode = Path.GetFileName(localizationFolder);
            translations[localizationCode] = GetLanguageName(localizationCode);
        }

        return translations;
    }

    /// <summary>
    /// Checks the current UI culture and finds the closest match from supported translations.
    /// </summary>
    /// <returns>Available translation locale code.</returns>
    public static string GetDefaultTranslationLocaleCode()
    {
        // we don't need names here pretty much
        Dictionary<string, string> translations = GetTranslations();

        for (var culture = InitialUICulture;
            culture != CultureInfo.InvariantCulture;
            culture = culture.Parent)
        {
            string translation = culture.Name;

            // the keys in 'translations' are case-insensitive
            if (translations.ContainsKey(translation))
                return translation;
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

        if (_culture is not null)
            general.AddKey(nameof(Culture), _culture.Name);

        general.AddKey(nameof(Author), Author);

        general.AddKey(nameof(MapEncoding), EncodingExt.EncodingWithAutoToString(MapEncoding));

        ini.AddSection(nameof(Values));
        IniSection translation = ini.GetSection(nameof(Values));

        foreach (var (key, value) in Values.OrderBy(kvp => kvp.Key))
        {
            bool valueMissing = MissingKeys.Contains(key);
            if (!saveOnlyMissingValues || valueMissing)
            {
                translation.AddKey(valueMissing
                        ? MISSING_KEY_PREFIX + key
                        : key,
                    value.ToIniString());
            }
        }

        return ini;
    }

    private bool HandleMissing(string key, string defaultValue)
    {
        if (MissingKeys.Add(key))
        {
            Values[key] = defaultValue;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Looks up the translated value that corresponds to the given key.
    /// </summary>
    /// <param name="key">The translation key (identifier).</param>
    /// <param name="defaultValue">The value to fall back to in case there's no translated value.</param>
    /// <param name="notify">Whether to add this key and value to the list of missing key-values.</param>
    /// <returns>The translated value or a default value.</returns>
    public string LookUp(string key, string defaultValue, bool notify = true)
    {
        if (Values.ContainsKey(key))
            return Values[key];

        if (notify)
            _ = HandleMissing(key, defaultValue);

        return defaultValue;
    }

    /// <summary>
    /// Looks up the translated value that corresponds to the given key with a fallback to the value of global key.
    /// </summary>
    /// <param name="key">The translation key (identifier).</param>
    /// <param name="fallbackKey">The fallback translation key (identifier).</param>
    /// <param name="defaultValue">The value to fall back to in case there's no translated value.</param>
    /// <param name="notify">Whether to add this key and value to the list of missing key-values. Doesn't include the fallback key.</param>
    /// <returns>The translated value or a default value.</returns>
    public string LookUp(string key, string fallbackKey, string defaultValue, bool notify = true)
    {
        string result;
        if (Values.ContainsKey(key))
        {
            result = Values[key];
        }
        else if (key != fallbackKey && Values.ContainsKey(fallbackKey))
        {
            result = Values[fallbackKey];
        }
        else
        {
            result = defaultValue;

            if (notify)
                _ = HandleMissing(key, defaultValue);
        }

        return result;
    }
}
