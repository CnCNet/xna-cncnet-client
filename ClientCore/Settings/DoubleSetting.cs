using Rampastring.Tools;

namespace ClientCore.Settings
{
    public class DoubleSetting : INISetting<double>
    {
        private readonly double scale;

        public DoubleSetting(IniFile iniFile, string iniSection, string iniKey, double defaultValue, double storageScale = 1.0)
            : base(iniFile, iniSection, iniKey, defaultValue)
        {
            this.scale = storageScale;
        }

        protected override double Get()
        {
            return IniFile.GetDoubleValue(IniSection, IniKey, DefaultValue * scale) / scale;
        }

        protected override void Set(double value)
        {
            IniFile.SetDoubleValue(IniSection, IniKey, value * scale);
        }

        public override void Write()
        {
            Set(Get());
        }

        public override string ToString()
        {
            return Get().ToString();
        }
    }
}
