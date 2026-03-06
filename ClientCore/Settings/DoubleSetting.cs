using Rampastring.Tools;
using System;

namespace ClientCore.Settings
{
    public class DoubleSetting : INISetting<double>
    {
        private double _cachedValue;
        public event EventHandler ValueChanged;

        public DoubleSetting(IniFile iniFile, string iniSection, string iniKey, double defaultValue)
            : base(iniFile, iniSection, iniKey, defaultValue)
        {
            _cachedValue = Get();
        }

        protected override double Get()
        {
            return IniFile.GetDoubleValue(IniSection, IniKey, DefaultValue);
        }

        protected override void Set(double value)
        {
            if (_cachedValue != value)
            {
                _cachedValue = value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
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

        public double Value
        {
            get => Get();
            set => Set(value);
        }
    }
}
