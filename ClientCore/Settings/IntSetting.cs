using Rampastring.Tools;

namespace ClientCore.Settings
{
    public class IntSetting : INISetting<int>
    {
        public IntSetting(IniFile iniFile, string iniSection, string iniKey, int defaultValue)
            : base(iniFile, iniSection, iniKey, defaultValue)
        {
        }

        protected override int Get()
        {
            return IniFile.GetIntValue(IniSection, IniKey, DefaultValue);
        }

        protected override void Set(int value)
        {
            IniFile.SetIntValue(IniSection, IniKey, value);
        }

        public override void Write()
        {
            IniFile.SetIntValue(IniSection, IniKey, Get());
        }

        public override string ToString()
        {
            return Get().ToString();
        }
    }
}
