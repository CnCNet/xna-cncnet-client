using System;

using ClientCore.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

/// <summary>
/// A generic message box with OK or Yes/No or OK/Cancel buttons.
/// </summary>
public class XNAMessageBox : XNAWindow
{
    /// <summary>
    /// Creates a new message box.
    /// </summary>
    /// <param name="windowManager">The window manager.</param>
    /// <param name="caption">The caption of the message box.</param>
    /// <param name="description">The actual message of the message box.</param>
    /// <param name="messageBoxButtons">Defines which buttons are available in the dialog.</param>
    public XNAMessageBox(WindowManager windowManager,
        string caption, string description, XNAMessageBoxButtons messageBoxButtons)
        : base(windowManager)
    {
        this.caption = caption;
        this.description = description;
        this.messageBoxButtons = messageBoxButtons;
    }

    /// <summary>
    /// The method that is called when the user clicks OK on the message box.
    /// </summary>
    public Action<XNAMessageBox> OKClickedAction { get; set; }

    /// <summary>
    /// The method that is called when the user clicks Yes on the message box.
    /// </summary>
    public Action<XNAMessageBox> YesClickedAction { get; set; }

    /// <summary>
    /// The method that is called when the user clicks No on the message box.
    /// </summary>
    public Action<XNAMessageBox> NoClickedAction { get; set; }

    /// <summary>
    /// The method that is called when the user clicks Cancel on the message box.
    /// </summary>
    public Action<XNAMessageBox> CancelClickedAction { get; set; }

    private readonly string caption;
    private readonly string description;
    private readonly XNAMessageBoxButtons messageBoxButtons;

    public override void Initialize()
    {
        Name = "MessageBox";
        BackgroundTexture = AssetLoader.LoadTexture("msgboxform.png");

        XNALabel lblCaption = new(WindowManager)
        {
            Text = caption,
            ClientRectangle = new Rectangle(12, 9, 0, 0),
            FontIndex = 1
        };

        XNAPanel line = new(WindowManager)
        {
            ClientRectangle = new Rectangle(6, 29, 0, 1)
        };

        XNALabel lblDescription = new(WindowManager)
        {
            Text = description,
            ClientRectangle = new Rectangle(12, 39, 0, 0)
        };

        AddChild(lblCaption);
        AddChild(line);
        AddChild(lblDescription);

        Vector2 textDimensions = Renderer.GetTextDimensions(lblDescription.Text, lblDescription.FontIndex);
        ClientRectangle = new Rectangle(0, 0, (int)textDimensions.X + 24, (int)textDimensions.Y + 81);
        line.ClientRectangle = new Rectangle(6, 29, Width - 12, 1);

        if (messageBoxButtons == XNAMessageBoxButtons.OK)
        {
            AddOKButton();
        }
        else if (messageBoxButtons == XNAMessageBoxButtons.YesNo)
        {
            AddYesNoButtons();
        }
        else // messageBoxButtons == DXMessageBoxButtons.OKCancel
        {
            AddOKCancelButtons();
        }

        base.Initialize();

        WindowManager.CenterControlOnScreen(this);
    }

    private void AddOKButton()
    {
        XNAButton btnOK = new(WindowManager)
        {
            FontIndex = 1,
            ClientRectangle = new Rectangle(0, 0, 75, 23),
            IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
            HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png"),
            HoverSoundEffect = new EnhancedSoundEffect("button.wav"),
            Name = "btnOK",
            Text = "OK".L10N("Client:ClientGUI:ButtonOK")
        };
        btnOK.LeftClick += BtnOK_LeftClick;
        btnOK.HotKey = Keys.Enter;

        AddChild(btnOK);

        btnOK.CenterOnParent();
        btnOK.ClientRectangle = new Rectangle(btnOK.X,
            Height - 28, btnOK.Width, btnOK.Height);
    }

    private void AddYesNoButtons()
    {
        XNAButton btnYes = new(WindowManager)
        {
            FontIndex = 1,
            ClientRectangle = new Rectangle(0, 0, 75, 23),
            IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
            HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png"),
            HoverSoundEffect = new EnhancedSoundEffect("button.wav"),
            Name = "btnYes",
            Text = "Yes".L10N("Client:ClientGUI:ButtonYes")
        };
        btnYes.LeftClick += BtnYes_LeftClick;
        btnYes.HotKey = Keys.Y;

        AddChild(btnYes);

        btnYes.ClientRectangle = new Rectangle((Width - ((btnYes.Width + 5) * 2)) / 2,
            Height - 28, btnYes.Width, btnYes.Height);

        XNAButton btnNo = new(WindowManager)
        {
            FontIndex = 1,
            ClientRectangle = new Rectangle(0, 0, 75, 23),
            IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
            HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png"),
            HoverSoundEffect = new EnhancedSoundEffect("button.wav"),
            Name = "btnNo",
            Text = "No".L10N("Client:ClientGUI:ButtonNo")
        };
        btnNo.LeftClick += BtnNo_LeftClick;
        btnNo.HotKey = Keys.N;

        AddChild(btnNo);

        btnNo.ClientRectangle = new Rectangle(btnYes.X + btnYes.Width + 10,
            Height - 28, btnNo.Width, btnNo.Height);
    }

    private void AddOKCancelButtons()
    {
        XNAButton btnOK = new(WindowManager)
        {
            FontIndex = 1,
            ClientRectangle = new Rectangle(0, 0, 75, 23),
            IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
            HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png"),
            HoverSoundEffect = new EnhancedSoundEffect("button.wav"),
            Name = "btnOK",
            Text = "OK".L10N("Client:ClientGUI:ButtonOK")
        };
        btnOK.LeftClick += BtnYes_LeftClick;
        btnOK.HotKey = Keys.Enter;

        AddChild(btnOK);

        btnOK.ClientRectangle = new Rectangle((Width - ((btnOK.Width + 5) * 2)) / 2,
            Height - 28, btnOK.Width, btnOK.Height);

        XNAButton btnCancel = new(WindowManager)
        {
            FontIndex = 1,
            ClientRectangle = new Rectangle(0, 0, 75, 23),
            IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
            HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png"),
            HoverSoundEffect = new EnhancedSoundEffect("button.wav"),
            Name = "btnCancel",
            Text = "Cancel".L10N("Client:ClientGUI:ButtonCancel")
        };
        btnCancel.LeftClick += BtnCancel_LeftClick;
        btnCancel.HotKey = Keys.C;

        AddChild(btnCancel);

        btnCancel.ClientRectangle = new Rectangle(btnOK.X + btnOK.Width + 10,
            Height - 28, btnCancel.Width, btnCancel.Height);
    }

    private void BtnOK_LeftClick(object sender, EventArgs e)
    {
        Hide();
        OKClickedAction?.Invoke(this);
    }

    private void BtnYes_LeftClick(object sender, EventArgs e)
    {
        Hide();
        YesClickedAction?.Invoke(this);
    }

    private void BtnNo_LeftClick(object sender, EventArgs e)
    {
        Hide();
        NoClickedAction?.Invoke(this);
    }

    private void BtnCancel_LeftClick(object sender, EventArgs e)
    {
        Hide();
        CancelClickedAction?.Invoke(this);
    }

    private void Hide()
    {
        if (Parent != null)
        {
            WindowManager.RemoveControl(Parent);
        }
        else
        {
            WindowManager.RemoveControl(this);
        }
    }

    public void Show()
    {
        DarkeningPanel.AddAndInitializeWithControl(WindowManager, this);
    }

    #region Static Show methods

    /// <summary>
    /// Creates and displays a new message box with the specified caption and description.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <param name="caption">The caption/header of the message box.</param>
    /// <param name="description">The description of the message box.</param>
    public static void Show(WindowManager windowManager, string caption, string description)
    {
        DarkeningPanel panel = new(windowManager)
        {
            Focused = true
        };
        windowManager.AddAndInitializeControl(panel);

        XNAMessageBox msgBox = new(windowManager,
            Renderer.GetSafeString(caption, 1),
            Renderer.GetSafeString(description, 0),
            XNAMessageBoxButtons.OK);

        panel.AddChild(msgBox);
        msgBox.OKClickedAction = MsgBox_OKClicked;
        windowManager.AddAndInitializeControl(msgBox);
        windowManager.SelectedControl = null;
    }

    private static void MsgBox_OKClicked(XNAMessageBox messageBox)
    {
        DarkeningPanel parent = (DarkeningPanel)messageBox.Parent;
        parent.Hide();
        parent.Hidden += Parent_Hidden;
    }

    /// <summary>
    /// Shows a message box with "Yes" and "No" being the user input options.
    /// </summary>
    /// <param name="windowManager">The WindowManager.</param>
    /// <param name="caption">The caption of the message box.</param>
    /// <param name="description">The description in the message box.</param>
    /// <returns>The XNAMessageBox instance that is created.</returns>
    public static XNAMessageBox ShowYesNoDialog(WindowManager windowManager, string caption, string description)
    {
        DarkeningPanel panel = new(windowManager);
        windowManager.AddAndInitializeControl(panel);

        XNAMessageBox msgBox = new(windowManager,
            Renderer.GetSafeString(caption, 1),
            Renderer.GetSafeString(description, 0),
            XNAMessageBoxButtons.YesNo);

        panel.AddChild(msgBox);
        msgBox.YesClickedAction = MsgBox_YesClicked;
        msgBox.NoClickedAction = MsgBox_NoClicked;

        return msgBox;
    }

    private static void MsgBox_NoClicked(XNAMessageBox messageBox)
    {
        DarkeningPanel parent = (DarkeningPanel)messageBox.Parent;
        parent.Hide();
        parent.Hidden += Parent_Hidden;
    }

    private static void MsgBox_YesClicked(XNAMessageBox messageBox)
    {
        DarkeningPanel parent = (DarkeningPanel)messageBox.Parent;
        parent.Hide();
        parent.Hidden += Parent_Hidden;
    }

    private static void Parent_Hidden(object sender, EventArgs e)
    {
        DarkeningPanel darkeningPanel = (DarkeningPanel)sender;

        darkeningPanel.WindowManager.RemoveControl(darkeningPanel);
        darkeningPanel.Hidden -= Parent_Hidden;
    }

    #endregion
}

public enum XNAMessageBoxButtons
{
    OK,
    YesNo,
    OKCancel
}