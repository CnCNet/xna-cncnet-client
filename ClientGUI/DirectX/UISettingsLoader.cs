using ClientCore;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI.DirectX
{
    /// <summary>
    /// A static class used for loading UI theme settings.
    /// </summary>
    public static class UISettingsLoader
    {
        public static Color GetWindowBorderColor()
        {
            return AssetLoader.GetColorFromString(DomainController.Instance().GetWindowBorderColor());
        }

        public static Color GetUILabelColor()
        {
            return AssetLoader.GetColorFromString(DomainController.Instance().GetUILabelColor());
        }

        public static Color GetUIAltColor()
        {
            return AssetLoader.GetColorFromString(DomainController.Instance().GetUIAltColor());
        }

        public static Color GetUIBackColor()
        {
            return AssetLoader.GetColorFromString(DomainController.Instance().GetUIAltBackgroundColor());
        }

        public static Color GetListBoxFocusColor()
        {
            return AssetLoader.GetColorFromString(DomainController.Instance().GetListBoxFocusColor());
        }

        public static Color GetPanelBorderColor()
        {
            return AssetLoader.GetColorFromString(DomainController.Instance().GetPanelBorderColor());
        }

        public static Color GetDropDownBorderColor()
        {
            return AssetLoader.GetColorFromString(DomainController.Instance().GetDropDownBorderColor());
        }
    }
}
