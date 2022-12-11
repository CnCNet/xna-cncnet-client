﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;
public class LocalizationParser : IControlINIAttributeParser
{
    private readonly TranslationTable _translationTable;

    public LocalizationParser(TranslationTable translationTable)
    {
        _translationTable = translationTable;
    }

    // shorthand for localization function
    private string Localize(XNAControl control, string attributeName, string defaultValue, bool notify = true)
        => _translationTable.LocalizeControlINIAttribute(control, attributeName, defaultValue, notify);

    public bool ParseINIAttribute(XNAControl control, IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "Text":
                control.Text = Localize(control, key, value).Replace("@", Environment.NewLine);
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
            case "ToolTip" when control is IHasToolTip controlWithToolTip:
                controlWithToolTip.ToolTipText = Localize(control, key, value).Replace("@", Environment.NewLine);
                return true;
        }

        return false;
    }
}
