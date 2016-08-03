using Rampastring.Tools;

namespace ClientCore.Settings
{
    public class IntSetting : INISetting<int>
    {
        public IntSetting(IniFile iniFile, string iniSection, string iniKey, int defaultValue)
            : base(iniFile, iniSection, iniKey, defaultValue)
        {
        }

        public static implicit operator int(IntSetting intSetting)
        {
            return intSetting.Get();
        }

        protected override int Get()
        {
            return IniFile.GetIntValue(IniSection, IniKey, DefaultValue);
        }

        protected override void Set(int value)
        {
            IniFile.SetIntValue(IniSection, IniKey, value);
        }

        public override string ToString()
        {
            return Get().ToString();
        }
    }
}
