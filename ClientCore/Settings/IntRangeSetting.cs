using System;
using Rampastring.Tools;

namespace ClientCore.Settings
{
    /// <summary>
    /// Similar to IntSetting, this setting forces a min and max value upon getting and setting.
    /// </summary>
    public class IntRangeSetting : IntSetting
    {
        private readonly int MinValue;
        private readonly int MaxValue;

        public IntRangeSetting(IniFile iniFile, string iniSection, string iniKey, int defaultValue, int minValue, int maxValue) : base(iniFile, iniSection, iniKey, defaultValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        /// <summary>
        /// Checks the validity of the value. If the value is invalid, return the default value of this setting.
        /// Otherwise, return the set value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private int NormalizeValue(int value)
        {
            return InvalidValue(value) ? DefaultValue : value;
        }

        private bool InvalidValue(int value)
        {
            return value < MinValue || value > MaxValue;
        }

        protected override int Get()
        {
            return NormalizeValue(IniFile.GetIntValue(IniSection, IniKey, DefaultValue));
        }

        protected override void Set(int value)
        {
            IniFile.SetIntValue(IniSection, IniKey, NormalizeValue(value));
        }
    }
}
