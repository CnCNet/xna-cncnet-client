namespace DTAClient.DXGUI
{
    /// <summary>
    /// An interface for all switchable windows.
    /// </summary>
    public interface ISwitchable
    {
        void SwitchOn();

        void SwitchOff();

        string GetSwitchName();
    }
}
