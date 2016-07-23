using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.Tools;

namespace ClientCore.Settings
{
    public class DoubleSetting : INISetting<double>
    {
        public DoubleSetting(IniFile iniFile, string iniSection, string iniKey, double defaultValue)
            : base(iniFile, iniSection, iniKey, defaultValue)
        {
        }

        public static implicit operator double(DoubleSetting ds)
        {
            return ds.Get();
        }

        protected override double Get()
        {
            return IniFile.GetDoubleValue(IniSection, IniKey, DefaultValue);
        }

        protected override void Set(double value)
        {
            IniFile.SetDoubleValue(IniSection, IniKey, value);
        }
    }
}
