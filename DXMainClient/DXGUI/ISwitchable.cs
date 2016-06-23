using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
