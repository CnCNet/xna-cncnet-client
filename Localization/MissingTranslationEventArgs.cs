using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Localization
{
    public class MissingTranslationEventArgs : EventArgs
    {
        public string LanguageTag { get; set; }
        public string Label { get; set; }
        public string DefaultValue { get; set; }

        public MissingTranslationEventArgs(string languageTag, string label, string defaultValue)
        {
            LanguageTag = languageTag;
            Label = label;
            DefaultValue = defaultValue;
        }
    }
}
