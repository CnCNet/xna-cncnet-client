using Rampastring.Tools;

namespace ClientCore.Settings
{
    public class StringSetting : INISetting<string>
    {
        public StringSetting(IniFile iniFile, string iniSection, string iniKey, string defaultValue)
            : base(iniFile, iniSection, iniKey, defaultValue)
        {
        }

        protected override string Get()
        {
            return IniFile.GetStringValue(IniSection, IniKey, DefaultValue);
        }

        protected override void Set(string value)
        {
            IniFile.SetStringValue(IniSection, IniKey, value);
        }

        public override void Write()
        {
            IniFile.SetStringValue(IniSection, IniKey, Get());
        }

        public override string ToString()
        {
            return Get();
        }
    }
}
