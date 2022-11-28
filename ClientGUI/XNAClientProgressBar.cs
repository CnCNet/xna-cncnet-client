using System;
using ClientCore.Enums;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

public class XNAClientProgressBar : XNAProgressBar
{
    public int Speed { get; set; } = 4;

    public double WidthRatio { get; set; } = 0.25;

    public ProgressBarModeEnum ProgressBarMode { get; set; }

    private int _left { get; set; }

    public XNAClientProgressBar(WindowManager windowManager) : base(windowManager)
    {
    }

    public override void Update(GameTime gameTime)
    {
        _left = (_left + Speed) % Width;

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        switch (ProgressBarMode)
        {
            case ProgressBarModeEnum.Indeterminate:
                DrawIndeterminateMode(gameTime);
                return;
            case ProgressBarModeEnum.Determinate:
            default:
                base.Draw(gameTime);
                return;
        }
    }

    public void DrawIndeterminateMode(GameTime gameTime)
    {
        Rectangle wrect = RenderRectangle();
        int filledWidth = (int)(wrect.Width * WidthRatio);

        for (int i = 0; i < BorderWidth; i++)
        {
            var rect = new Rectangle(wrect.X + i, wrect.Y + i, wrect.Width - i, wrect.Height - i);

            Renderer.DrawRectangle(rect, BorderColor);
        }

        Renderer.FillRectangle(new Rectangle(wrect.X + BorderWidth, wrect.Y + BorderWidth, wrect.Width - BorderWidth * 2, wrect.Height - BorderWidth * 2), UnfilledColor);

        if (_left + filledWidth > wrect.Width - BorderWidth * 2)
        {
            Renderer.FillRectangle(new Rectangle(wrect.X + BorderWidth, wrect.Y + BorderWidth, (_left + filledWidth) - (wrect.Width - (BorderWidth * 2)), wrect.Height - BorderWidth * 2), FilledColor);
        }

        Renderer.FillRectangle(new Rectangle(wrect.X + BorderWidth + _left, wrect.Y + BorderWidth, Math.Min(filledWidth, wrect.Width - (BorderWidth * 2) - _left), wrect.Height - BorderWidth * 2), FilledColor);
    }

    public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "WidthRatio":
                WidthRatio = double.Parse(value);
                return;
            case "ProgressBarMode":
                ProgressBarMode = (ProgressBarModeEnum)Enum.Parse(typeof(ProgressBarModeEnum), value);
                return;
            case "Speed":
                Speed = int.Parse(value);
                return;
            default:
                base.ParseAttributeFromINI(iniFile, key, value);
                return;
        }
    }
}