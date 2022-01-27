using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Localization
{
    public static class LanguageInstance
    {
        /// <summary>
        /// This field controls the runtime translation file that is used by any UI outputs.
        /// </summary>
        public static TranslationTable TranslationTable { get; set; } = new TranslationTable(); // load an empty table by default; can be re-assigned
    }
}
