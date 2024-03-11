using System;
using System.IO;
using System.Reflection;

using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

using SixLabors.ImageSharp;

using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

/// <summary>
/// A box that notifies users of new private messages,
/// top-right of the game window.
/// </summary>
public class PrivateMessageNotificationBox : XNAPanel
{
    private const double DOWN_TIME_WAIT_SECONDS = 4.0;
    private const double DOWN_MOVEMENT_RATE = 2.0;
    private const double UP_MOVEMENT_RATE = 2.0;

    public PrivateMessageNotificationBox(WindowManager windowManager) : base(windowManager)
    {
        downTimeWaitTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS);
    }

    private XNALabel lblSender;
    private XNAPanel gameIconPanel;
    private XNALabel lblMessage;
    private TimeSpan downTime = TimeSpan.Zero;
    private readonly TimeSpan downTimeWaitTime;
    private bool isDown = false;
    private double locationY = -100.0;

    public override void Initialize()
    {
        Name = "PrivateMessageNotificationBox";
        BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 1, 1);
        ClientRectangle = new Rectangle(WindowManager.RenderResolutionX - 300, -100, 300, 100);
        PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

        XNALabel lblHeader = new(WindowManager)
        {
            Name = "lblHeader",
            FontIndex = 1,
            Text = "PRIVATE MESSAGE".L10N("Client:Main:PMHeader")
        };
        AddChild(lblHeader);
        lblHeader.CenterOnParent();
        lblHeader.ClientRectangle = new Rectangle(lblHeader.X,
            6, lblHeader.Width, lblHeader.Height);

        XNAPanel linePanel = new(WindowManager)
        {
            Name = "linePanel",
            ClientRectangle = new Rectangle(0, Height - 20, Width, 1)
        };

        XNALabel lblHint = new(WindowManager)
        {
            Name = "lblHint",
            RemapColor = UISettings.ActiveSettings.SubtleTextColor,
            Text = "Press F4 to respond".L10N("Client:Main:F4ToRespond")
        };

        AddChild(lblHint);
        lblHint.CenterOnParent();
        lblHint.ClientRectangle = new Rectangle(lblHint.X,
            linePanel.Y + 3,
            lblHint.Width, lblHint.Height);

        gameIconPanel = new XNAPanel(WindowManager)
        {
            Name = "gameIconPanel",
            ClientRectangle = new Rectangle(12, 30, 16, 16),
            DrawBorders = false
        };

        Assembly assembly = Assembly.GetAssembly(typeof(GameCollection));
        using Stream dtaIconStream = assembly.GetManifestResourceStream("ClientCore.Resources.dtaicon.png");
        using Image dtaIcon = Image.Load(dtaIconStream);

        gameIconPanel.BackgroundTexture = AssetLoader.TextureFromImage(dtaIcon);

        lblSender = new XNALabel(WindowManager)
        {
            Name = "lblSender",
            FontIndex = 1,
            ClientRectangle = new Rectangle(gameIconPanel.Right + 3,
            gameIconPanel.Y, 0, 0),
            Text = "Rampastring:"
        };

        lblMessage = new XNALabel(WindowManager)
        {
            Name = "lblMessage",
            ClientRectangle = new Rectangle(12, lblSender.Bottom + 6, 0, 0),
            RemapColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.ReceivedPMColor),
            Text = "This is a test message."
        };

        AddChild(gameIconPanel);
        AddChild(linePanel);
        AddChild(lblSender);
        AddChild(lblMessage);

        base.Initialize();
    }

    public void Show(Texture2D gameIcon, string sender, string message)
    {
        Visible = true;
        Enabled = true;
        gameIconPanel.BackgroundTexture = gameIcon;
        lblSender.Text = sender + ":";
        lblMessage.Text = message;

        if (lblMessage.Right > Width)
        {
            while (lblMessage.Right > Width)
            {
                lblMessage.Text = lblMessage.Text.Remove(lblMessage.Text.Length - 1);
            }

            if (lblMessage.Text.Length > 3)
            {
                lblMessage.Text = lblMessage.Text.Remove(lblMessage.Text.Length - 3) + "...";
            }
        }

        downTime = TimeSpan.Zero;
        isDown = true;
    }

    public void Hide()
    {
        isDown = false;
        locationY = -Height;
        ClientRectangle = new Rectangle(X, (int)locationY,
            Width, Height);
        Visible = false;
        Enabled = false;
    }

    public override void Update(GameTime gameTime)
    {
        if (isDown)
        {
            if (locationY < 0)
            {
                locationY += DOWN_MOVEMENT_RATE;
                ClientRectangle = new Rectangle(X, (int)locationY,
                    Width, Height);
            }

            if (WindowManager.HasFocus)
            {
                downTime += gameTime.ElapsedGameTime;
                isDown = downTime < downTimeWaitTime;
            }
        }
        else
        {
            if (locationY > -Height)
            {
                locationY -= UP_MOVEMENT_RATE;
                ClientRectangle = new Rectangle(X, (int)locationY, Width, Height);
            }
            else
            {
                Visible = false;
                Enabled = false;
            }
        }

        base.Update(gameTime);
    }
}