/*********************************************************************
* Dawn of the Tiberium Age MonoGame/XNA CnCNet Client
* Expression Parser
* Copyright (C) Rampastring 2022
* 
* The CnCNet Client is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
* 
* The CnCNet Client is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with this program.If not, see<https://www.gnu.org/licenses/>.
* 
*********************************************************************/

using ClientCore;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClientGUI
{
    /// <summary>
    /// Parses arithmetic expressions.
    /// </summary>
    class Parser
    {
        private const int CHAR_VALUE_ZERO = 48;

        public Parser(WindowManager windowManager)
        {
            if (_instance != null)
                throw new InvalidOperationException("Only one instance of Parser can exist at a time.");

            globalConstants = new Dictionary<string, int>();
            globalConstants.Add("RESOLUTION_WIDTH", windowManager.RenderResolutionX);
            globalConstants.Add("RESOLUTION_HEIGHT", windowManager.RenderResolutionY);

            IniSection parserConstantsSection = ClientConfiguration.Instance.GetParserConstants();
            if (parserConstantsSection != null)
            {
                foreach (var kvp in parserConstantsSection.Keys)
                    globalConstants.Add(kvp.Key, Conversions.IntFromString(kvp.Value, 0));
            }

            _instance = this;
        }

        private static Parser _instance;
        public static Parser Instance => _instance;

        private static Dictionary<string, int> globalConstants;

        public string Input { get; private set; }

        private int tokenPlace;
        private XNAControl primaryControl;
        private XNAControl parsingControl;

        private XNAControl GetControl(string controlName)
        {
            if (controlName == primaryControl.Name)
                return primaryControl;

            if (controlName == primaryControl.Parent.Name)
                return primaryControl.Parent;

            var control = Find(primaryControl.Children, controlName);
            if (control == null)
                throw new KeyNotFoundException($"Control '{controlName}' not found while parsing input '{Input}'");

            return control;
        }

        private XNAControl Find(IEnumerable<XNAControl> list, string controlName)
        {
            foreach (XNAControl child in list)
            {
                if (child.Name == controlName)
                    return child;

                XNAControl childOfChild = Find(child.Children, controlName);
                if (childOfChild != null)
                    return childOfChild;
            }

            return null;
        }

        private int GetConstant(string constantName)
        {
            return globalConstants[constantName];
        }

        public void SetPrimaryControl(XNAControl primaryControl)
        {
            this.primaryControl = primaryControl;
        }

        public int GetExprValue(string input, XNAControl parsingControl)
        {
            this.parsingControl = parsingControl;
            Input = Regex.Replace(input, @"\s", "");
            tokenPlace = 0;
            return GetExprValue();
        }

        private int GetExprValue()
        {
            int value = 0;

            while (true)
            {
                if (IsEndOfInput())
                    return value;

                char c = Input[tokenPlace];

                if (char.IsDigit(c))
                {
                    value = GetInt();
                }
                else if (c == '+')
                {
                    tokenPlace++;
                    value += GetNumericalValue();
                }
                else if (c == '-')
                {
                    tokenPlace++;
                    value -= GetNumericalValue();
                }
                else if (c == '/')
                {
                    tokenPlace++;
                    value /= GetExprValue();
                }
                else if (c == '*')
                {
                    tokenPlace++;
                    value *= GetExprValue();
                }
                else if (c == '(')
                {
                    tokenPlace++;
                    value = GetExprValue();
                }
                else if (c == ')')
                {
                    tokenPlace++;
                    return value;
                }
                else if (char.IsUpper(c))
                {
                    value = GetConstantValue();
                }
                else if (char.IsLower(c))
                {
                    value = GetFunctionValue();
                }
            }
        }

        private int GetNumericalValue()
        {
            SkipWhitespace();

            if (IsEndOfInput())
                return 0;

            char c = Input[tokenPlace];

            if (char.IsDigit(c))
            {
                return GetInt();
            }
            else if (char.IsUpper(c))
            {
                return GetConstantValue();
            }
            else if (char.IsLower(c))
            {
                return GetFunctionValue();
            }
            else if (c == '(')
            {
                tokenPlace++;
                return GetExprValue();
            }
            else
            {
                throw new INIConfigException("Unexpected character " + c + " when parsing input: " + Input);
            }
        }

        private void SkipWhitespace()
        {
            while (true)
            {
                if (IsEndOfInput())
                    return;

                char c = Input[tokenPlace];
                if (c == ' ' || c == '\r' || c == '\n')
                    tokenPlace++;
                else
                    break;
            }
        }

        private string GetIdentifier()
        {
            string identifierName = "";

            while (true)
            {
                if (IsEndOfInput())
                    break;

                char c = Input[tokenPlace];
                if (char.IsWhiteSpace(c))
                    break;

                if (!char.IsLetterOrDigit(c) && c != '_' && c != '$')
                    break;

                identifierName += c.ToString();
                tokenPlace++;
            }

            return identifierName;
        }

        private int GetConstantValue()
        {
            string constantName = GetIdentifier();
            return GetConstant(constantName);
        }

        private int GetFunctionValue()
        {
            string functionName = GetIdentifier();
            SkipWhitespace();
            ConsumeChar('(');
            string paramName = GetIdentifier();
            SkipWhitespace();
            ConsumeChar(')');

            if (paramName == "$ParentControl")
            {
                if (parsingControl.Parent == null)
                    throw new INIConfigException("$ParentControl used for control that has no parent: " + parsingControl.Name);

                paramName = parsingControl.Parent.Name;
            }
            else if (paramName == "$Self")
            {
                paramName = parsingControl.Name;
            }

            switch (functionName)
            {
                case "getX":
                    return GetControl(paramName).X;
                case "getY":
                    return GetControl(paramName).Y;
                case "getWidth":
                    return GetControl(paramName).Width;
                case "getHeight":
                    return GetControl(paramName).Height;
                case "getBottom":
                    return GetControl(paramName).Bottom;
                case "getRight":
                    return GetControl(paramName).Right;
                case "horizontalCenterOnParent":
                    parsingControl.CenterOnParentHorizontally();
                    return parsingControl.X;
                default:
                    throw new INIConfigException("Unknown function " + functionName + " in expression " + Input);
            }
        }

        private void ConsumeChar(char token)
        {
            if (Input[tokenPlace] != token)
                throw new INIConfigException($"Parse error: expected '{token}' in expression {Input}. Instead encountered '{Input[tokenPlace]}'.");

            tokenPlace++;
        }

        private int GetInt()
        {
            int value = 0;
            while (true)
            {
                if (IsEndOfInput())
                    return value;

                char c = Input[tokenPlace];
                if (!char.IsDigit(c))
                    return value;

                value = (value * 10) + Input[tokenPlace] - CHAR_VALUE_ZERO;
                tokenPlace++;
            }
        }

        private bool IsEndOfInput() => tokenPlace >= Input.Length;
    }
}
