namespace ClientCore
{
    public class SideComboboxPrerequisite
    {
        public bool Valid = false;
        public int ComboBoxId = -1;
        public int RequiredIndexId = -1;

        public void SetData(int comboBoxId, int requiredIndexId)
        {
            ComboBoxId = comboBoxId;
            RequiredIndexId = requiredIndexId;
            Valid = true;
        }
    }
}
