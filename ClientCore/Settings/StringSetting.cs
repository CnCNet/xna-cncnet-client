using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.Tools;

namespace ClientCore.Settings
{
    public class StringSetting : INISetting<string>
    {
        public StringSetting(IniFile iniFile, string iniSection, string iniKey, string defaultValue)
            : base(iniFile, iniSection, iniKey, defaultValue)
        {
        }

        public static implicit operator string(StringSetting ss)
        {
            return ss.Get();
        }

        protected override string Get()
        {
            return IniFile.GetStringValue(IniSection, IniKey, DefaultValue);
        }

        protected override void Set(string value)
        {
            IniFile.SetStringValue(IniSection, IniKey, value);
        }
    }
}
