using Rampastring.Tools;

namespace ClientCore.Settings
{
    public class BoolSetting : INISetting<bool>
    {
        public BoolSetting(IniFile iniFile, string iniSection, string iniKey, bool defaultValue)
            : base(iniFile, iniSection, iniKey, defaultValue)
        {
        }

        protected override bool Get()
        {
            return IniFile.GetBooleanValue(IniSection, IniKey, DefaultValue);
        }

        protected override void Set(bool value)
        {
            IniFile.SetBooleanValue(IniSection, IniKey, value);
        }

        public override void Write()
        {
            IniFile.SetBooleanValue(IniSection, IniKey, Get());
        }

        public override string ToString()
        {
            return Get().ToString();
        }
    }
}
