using Rampastring.Tools;

namespace ClientCore.Settings
{
    public class DoubleSetting : INISetting<double>
    {
        public DoubleSetting(IniFile iniFile, string iniSection, string iniKey, double defaultValue)
            : base(iniFile, iniSection, iniKey, defaultValue)
        {
        }

        protected override double Get()
        {
            return IniFile.GetDoubleValue(IniSection, IniKey, DefaultValue);
        }

        protected override void Set(double value)
        {
            IniFile.SetDoubleValue(IniSection, IniKey, value);
        }

        public override void Write()
        {
            IniFile.SetDoubleValue(IniSection, IniKey, Get());
        }

        public override string ToString()
        {
            return Get().ToString();
        }
    }
}
