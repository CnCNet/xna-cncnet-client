using System;

namespace ClientCore
{
    public class SideCheckboxPrerequisite
    {
        bool valid = false;

        public bool Valid { get { return valid; } }
        public int CheckBoxIndex = -1;
        public bool RequiredValue = false;

        public void SetData(int checkBoxIndex, bool requiredValue)
        {
            if (checkBoxIndex == -1)
                throw new ArgumentException("CheckBoxIndex cannot be below zero!");

            CheckBoxIndex = checkBoxIndex;
            RequiredValue = requiredValue;
            valid = true;
        }
    }
}
