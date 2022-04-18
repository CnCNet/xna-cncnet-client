using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Localization
{
    public class TranslationTable : ICloneable
    {
        /// <summary>
        /// The internal ID for a language. Should be unique.
        /// It is recommended to use BCP-47 Language Tags.
        /// </summary>
        public string LanguageTag { get; set; } = "en-US";
        /// <summary>
        /// The user-friendly name for a language.
        /// </summary>
        public string LanguageName { get; set; } = "English (United States)";
        /// <summary>
        /// The translation table. The key stands for a label name, and the value stands for a string that is used in System.string.Format().
        /// It is advised that the label name is started with "UI:" prefix.
        /// The value can not contains IniNewLinePattern when loading or saving via ini format.
        /// </summary>
        public Dictionary<string, string> Table { get; } = new Dictionary<string, string>();
        public CultureInfo CultureInfo { get; set; } = new CultureInfo("en-US");
        /// <summary>
        /// This a string showing the information about the authors. The program will not depend on this string.
        /// </summary>
        public string Author { get; set; } = string.Empty;

        // public bool IsRightToLeft { get; set; } // TODO

        /// <summary>
        /// Get notified when a translation table does not contain a label that is needed.
        /// </summary>
        public event EventHandler<MissingTranslationEventArgs> MissingTranslationEvent;
        private readonly HashSet<string> NotifiedMissingLabelsSet = new HashSet<string>();

        // As the ini value can not contains NewLine character '\n', it will be replaced with '@@' pattern.
        public static readonly string IniNewLinePattern = "@@";

        public static TranslationTable Instance { get; set; } = new TranslationTable();

        /// <summary>
        /// Create an empty translation table.
        /// </summary>
        public TranslationTable() { }
        /// <summary>
        /// Load the translation table from an ini file.
        /// </summary>
        /// <param name="ini">An ini file to be read.</param>
        public TranslationTable(IniFile ini)
        {
            if (ini == null) throw new ArgumentNullException(nameof(ini));

            IniSection general = ini.GetSection("General");
            LanguageTag = general?.GetStringValue("LanguageTag", null);
            LanguageName = general?.GetStringValue("LanguageName", null);
            string CultureInfoName = general?.GetStringValue("CultureInfo", null);
            Author = general?.GetStringValue("Author", string.Empty);
            IniSection translation = ini.GetSection("Translation");

            if (general == null || translation == null || LanguageTag == null
                || LanguageName == null || CultureInfoName == null)
            {
                throw new Exception("Invalid translation table file.");
            }

            CultureInfo = new CultureInfo(CultureInfoName);

            foreach (var kv in translation.Keys)
            {
                string label = kv.Key;
                string value = kv.Value;

                value = UnescapeIniValue(value);
                Table.Add(label, value);
            }
        }

        public TranslationTable(TranslationTable table)
        {
            LanguageTag = table.LanguageTag;
            LanguageName = table.LanguageName;
            Author = table.Author;
            CultureInfo = table.CultureInfo;
            foreach (var kv in table.Table)
            {
                Table.Add(kv.Key, kv.Value);
            }
        }
        public TranslationTable Clone() => new TranslationTable(this);
        object ICloneable.Clone() => Clone();

        public static TranslationTable LoadFromIniFile(string iniPath)
        {
            using (var stream = File.Open(iniPath, FileMode.Open))
            {
                IniFile iniFile = new IniFile(stream);
                return new TranslationTable(iniFile);
            }
        }

        /// <summary>
        /// Dump the translation table to an ini file.
        /// </summary>
        /// <returns>An ini file that contains the translation table.</returns>
        public IniFile SaveIni()
        {
            IniFile ini = new IniFile();

            ini.AddSection("General");
            IniSection general = ini.GetSection("General");
            general.AddKey("LanguageTag", LanguageTag);
            general.AddKey("LanguageName", LanguageName);
            general.AddKey("CultureInfo", CultureInfo.Name);
            general.AddKey("Author", Author);

            ini.AddSection("Translation");
            IniSection translation = ini.GetSection("Translation");

            foreach (var kv in Table)
            {
                string label = kv.Key;
                string value = kv.Value;

                value = EscapeIniValue(value);

                translation.AddKey(label, value);
            }

            return ini;
        }
        private void OnMissingTranslationEvent(object sender, MissingTranslationEventArgs e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));

            if (NotifiedMissingLabelsSet.Contains(e.Label)) return;
            MissingTranslationEvent?.Invoke(this, e);
            _ = NotifiedMissingLabelsSet.Add(e.Label);
        }
        public string GetTableValue(string label, string defaultValue)
        {
            if (Table.ContainsKey(label))
            {
                return Table[label];
            }
            else
            {
                OnMissingTranslationEvent(this, new MissingTranslationEventArgs(LanguageTag, label, defaultValue));
                return defaultValue;
            }
        }

        public static string EscapeIniValue(string raw)
        {
            if (raw.Contains(IniNewLinePattern))
                throw new Exception($"Pattern {IniNewLinePattern} is forbidden as this pattern is used to represent the new line.");

            if (raw.Contains(";"))
                throw new Exception("The semi-colon(;) is forbidden as this pattern is used to represent a comment line.");

            string value = raw.Replace(Environment.NewLine, "\n");
            value = value.Replace("\n", IniNewLinePattern);
            return value;
        }

        public static string UnescapeIniValue(string escaped)
            => escaped.Replace(IniNewLinePattern, "\n");
    }
}
