using ClientCore;
using ClientCore.Extensions;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

/// <summary>
/// A window that allows the host to modify game lobby settings.
/// </summary>
public class GameLobbySettingsWindow(WindowManager windowManager) : XNAWindow(windowManager)
{
    public event EventHandler<GameLobbySettingsEventArgs> SettingsChanged;

    private XNATextBox tbGameName;
    private XNATextBox tbPassword;
    private XNAClientDropDown ddMaxPlayers;
    private XNAClientDropDown ddSkillLevel;

    private XNALabel lblRoomName;
    private XNALabel lblPassword;
    private XNALabel lblMaxPlayers;
    private XNALabel lblSkillLevel;

    private XNAClientButton btnSave;
    private XNAClientButton btnCancel;

    private string[] SkillLevelOptions;

    public override void Initialize()
    {
        SkillLevelOptions = ClientConfiguration.Instance.SkillLevelOptions.Split(',');

        Name = "GameLobbySettingsWindow";
        ClientRectangle = new Rectangle(0, 0, 400, 240);
        BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");

        lblRoomName = new XNALabel(WindowManager);
        lblRoomName.Name = nameof(lblRoomName);
        lblRoomName.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
            UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, UIDesignConstants.EMPTY_SPACE_TOP +
            UIDesignConstants.CONTROL_VERTICAL_MARGIN, 0, 0);
        lblRoomName.Text = "Game room name:".L10N("Client:Main:GameRoomName");

        tbGameName = new XNATextBox(WindowManager);
        tbGameName.Name = nameof(tbGameName);
        tbGameName.MaximumTextLength = 23;
        tbGameName.ClientRectangle = new Rectangle(Width - 200 - UIDesignConstants.EMPTY_SPACE_SIDES -
            UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, lblRoomName.Y - 2, 200, 21);

        int nextY = tbGameName.Bottom + 15;

        lblPassword = new XNALabel(WindowManager);
        lblPassword.Name = nameof(lblPassword);
        lblPassword.ClientRectangle = new Rectangle(lblRoomName.X, nextY, 0, 0);
        lblPassword.Text = "Password:".L10N("Client:Main:LobbyPassword");

        tbPassword = new XNATextBox(WindowManager);
        tbPassword.Name = nameof(tbPassword);
        tbPassword.MaximumTextLength = 20;
        tbPassword.ClientRectangle = new Rectangle(tbGameName.X, lblPassword.Y - 2, 200, 21);

        nextY = tbPassword.Bottom + 15;

        lblMaxPlayers = new XNALabel(WindowManager);
        lblMaxPlayers.Name = nameof(lblMaxPlayers);
        lblMaxPlayers.ClientRectangle = new Rectangle(lblRoomName.X, nextY, 0, 0);
        lblMaxPlayers.Text = "Max players:".L10N("Client:Main:GameMaxPlayers");

        ddMaxPlayers = new XNAClientDropDown(WindowManager);
        ddMaxPlayers.Name = nameof(ddMaxPlayers);
        ddMaxPlayers.ClientRectangle = new Rectangle(tbGameName.X, lblMaxPlayers.Y - 2,
            tbGameName.Width, 21);
        for (int i = 8; i > 1; i--)
            ddMaxPlayers.AddItem(i.ToString());
        ddMaxPlayers.SelectedIndex = 0;

        nextY = ddMaxPlayers.Bottom + 15;

        lblSkillLevel = new XNALabel(WindowManager);
        lblSkillLevel.Name = nameof(lblSkillLevel);
        lblSkillLevel.ClientRectangle = new Rectangle(lblRoomName.X, nextY, 0, 0);
        lblSkillLevel.Text = "Preferred skill level:".L10N("Client:Main:PreferredSkillLevel");

        ddSkillLevel = new XNAClientDropDown(WindowManager);
        ddSkillLevel.Name = nameof(ddSkillLevel);
        ddSkillLevel.ClientRectangle = new Rectangle(tbGameName.X, lblSkillLevel.Y - 2,
            tbGameName.Width, 21);

        for (int i = 0; i < SkillLevelOptions.Length; i++)
        {
            string skillLevel = SkillLevelOptions[i];
            string localizedSkillLevel = skillLevel.L10N($"INI:ClientDefinitions:SkillLevel:{i}");
            ddSkillLevel.AddItem(localizedSkillLevel);
        }

        ddSkillLevel.SelectedIndex = ClientConfiguration.Instance.DefaultSkillLevelIndex;

        nextY = ddSkillLevel.Bottom + 20;

        btnSave = new XNAClientButton(WindowManager);
        btnSave.Name = nameof(btnSave);
        btnSave.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
            UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, nextY, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
        btnSave.Text = "Save".L10N("Client:Main:ButtonSave");
        btnSave.LeftClick += BtnSave_LeftClick;

        btnCancel = new XNAClientButton(WindowManager);
        btnCancel.Name = nameof(btnCancel);
        btnCancel.ClientRectangle = new Rectangle(Width - UIDesignConstants.BUTTON_WIDTH_133 - UIDesignConstants.EMPTY_SPACE_SIDES -
            UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, btnSave.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
        btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
        btnCancel.LeftClick += BtnCancel_LeftClick;

        AddChild(lblRoomName);
        AddChild(tbGameName);
        AddChild(lblPassword);
        AddChild(tbPassword);
        AddChild(lblMaxPlayers);
        AddChild(ddMaxPlayers);
        AddChild(lblSkillLevel);
        AddChild(ddSkillLevel);
        AddChild(btnSave);
        AddChild(btnCancel);

        Height = btnSave.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN + UIDesignConstants.EMPTY_SPACE_BOTTOM;

        base.Initialize();

        CenterOnParent();
    }

    public void Open(string currentGameName, int currentMaxPlayers, int currentSkillLevel, string currentPassword)
    {
        tbGameName.Text = currentGameName;
        tbPassword.Text = currentPassword ?? string.Empty;
        ddMaxPlayers.SelectedIndex = 8 - currentMaxPlayers;
        ddSkillLevel.SelectedIndex = currentSkillLevel;

        Enable();
    }

    private void BtnSave_LeftClick(object sender, EventArgs e)
    {
        string gameName = NameValidator.GetSanitizedGameName(tbGameName.Text);

        NameValidationError validationError = NameValidator.IsGameNameValid(gameName, out string errorMessage);
        if (validationError != NameValidationError.None)
        {
            XNAMessageBox.Show(WindowManager, "Invalid game name".L10N("Client:Main:InvalidGameName"),
                errorMessage);
            return;
        }

        int maxPlayers = int.Parse(ddMaxPlayers.SelectedItem.Text);
        int skillLevel = ddSkillLevel.SelectedIndex;
        string password = tbPassword.Text;

        SettingsChanged?.Invoke(this, new GameLobbySettingsEventArgs(
            gameName, maxPlayers, skillLevel, password));

        Disable();
    }

    private void BtnCancel_LeftClick(object sender, EventArgs e)
    {
        Disable();
    }
}
