using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;
using ClientCore.I18N;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

public class TranslationINIParser : IControlINIAttributeParser
{
    private static TranslationINIParser _instance;
    public static TranslationINIParser Instance => _instance ??= new TranslationINIParser();

    // shorthand for localization function
    private string Localize(XNAControl control, string attributeName, string defaultValue, bool notify = true)
        => Translation.Instance.LookUp(control, attributeName, defaultValue, notify);

    public bool ParseINIAttribute(XNAControl control, IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "Text":
                control.Text = Localize(control, key, value.FromIniString());
                return true;
            case "Size":
                string[] size = Localize(control, key, value, notify: false).Split(',');
                control.ClientRectangle = new Rectangle(control.X, control.Y,
                    int.Parse(size[0], CultureInfo.InvariantCulture),
                    int.Parse(size[1], CultureInfo.InvariantCulture));
                return true;
            case "Width":
                control.Width = int.Parse(Localize(control, key, value, notify: false),
                    CultureInfo.InvariantCulture);
                return true;
            case "Height":
                control.Height = int.Parse(Localize(control, key, value, notify: false),
                    CultureInfo.InvariantCulture);
                return true;
            case "Location":
                string[] location = Localize(control, key, value, notify: false).Split(',');
                control.ClientRectangle = new Rectangle(
                    int.Parse(location[0], CultureInfo.InvariantCulture),
                    int.Parse(location[1], CultureInfo.InvariantCulture),
                    control.Width, control.Height);
                return true;
            case "X":
                control.X = int.Parse(Localize(control, key, value, notify: false),
                    CultureInfo.InvariantCulture);
                return true;
            case "Y":
                control.Y = int.Parse(Localize(control, key, value, notify: false),
                    CultureInfo.InvariantCulture);
                return true;
            case "DistanceFromRightBorder":
                if (control.Parent != null)
                {
                    control.ClientRectangle = new Rectangle(
                        control.Parent.Width
                            - control.Width
                            - Conversions.IntFromString(Localize(control, key, value, notify: false), 0),
                        control.Y,
                        control.Width, control.Height);
                }
                return true;
            case "DistanceFromBottomBorder":
                if (control.Parent != null)
                {
                    control.ClientRectangle = new Rectangle(
                        control.X,
                        control.Parent.Height
                            - control.Height
                            - Conversions.IntFromString(Localize(control, key, value, notify: false), 0),
                        control.Width, control.Height);
                }
                return true;
            case "ToolTip" when control is IToolTipContainer controlWithToolTip:
                controlWithToolTip.ToolTipText = Localize(control, key, value.FromIniString());
                return true;
            case "Suggestion" when control is XNASuggestionTextBox suggestionTextBox:
                suggestionTextBox.Suggestion = Localize(control, key, value.FromIniString());
                return true;
            case "URL" when control is XNALinkButton button:  // need to link localized docs
                button.URL = Localize(control, key, value.FromIniString(), notify: false);
                return true;
            case "UnixURL" when control is XNALinkButton button:
                button.UnixURL = Localize(control, key, value.FromIniString(), notify: false);
                return true;
        }

        return false;
    }
}
