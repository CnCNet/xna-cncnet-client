using System;
using System.IO;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using Microsoft.Xna.Framework;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer;

/// <summary>
/// A window that makes it possible for a LAN player who's hosting a game
/// to pick between hosting a new game and hosting a loaded game.
/// </summary>
internal class LANGameCreationWindow : XNAWindow
{
    public LANGameCreationWindow(WindowManager windowManager) : base(windowManager)
    {
    }

    public event EventHandler NewGame;
    public event EventHandler<GameLoadEventArgs> LoadGame;

    private XNALabel lblDescription;

    private XNAButton btnNewGame;
    private XNAButton btnLoadGame;
    private XNAButton btnCancel;

    public override void Initialize()
    {
        Name = "LANGameCreationWindow";
        BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");
        ClientRectangle = new Rectangle(0, 0, 447, 77);

        lblDescription = new XNALabel(WindowManager)
        {
            Name = "lblDescription",
            FontIndex = 1,
            Text = "SELECT SESSION TYPE".L10N("Client:Main:SelectMissionType")
        };

        AddChild(lblDescription);

        lblDescription.CenterOnParent();
        lblDescription.ClientRectangle = new Rectangle(
            lblDescription.X,
            12,
            lblDescription.Width,
            lblDescription.Height);

        btnNewGame = new XNAButton(WindowManager)
        {
            Name = "btnNewGame",
            ClientRectangle = new Rectangle(12, 42, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
            IdleTexture = AssetLoader.LoadTexture("133pxbtn.png"),
            HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png"),
            FontIndex = 1,
            Text = "New Game".L10N("Client:Main:NewGame"),
            HoverSoundEffect = new EnhancedSoundEffect("button.wav")
        };
        btnNewGame.LeftClick += BtnNewGame_LeftClick;

        btnLoadGame = new XNAButton(WindowManager)
        {
            Name = "btnLoadGame",
            ClientRectangle = new Rectangle(btnNewGame.Right + 12,
            btnNewGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
            IdleTexture = btnNewGame.IdleTexture,
            HoverTexture = btnNewGame.HoverTexture,
            FontIndex = 1,
            Text = "Load Game".L10N("Client:Main:LoadGame"),
            HoverSoundEffect = btnNewGame.HoverSoundEffect
        };
        btnLoadGame.LeftClick += BtnLoadGame_LeftClick;

        btnCancel = new XNAButton(WindowManager)
        {
            Name = "btnCancel",
            ClientRectangle = new Rectangle(btnLoadGame.Right + 12,
            btnNewGame.Y, 133, 23),
            IdleTexture = btnNewGame.IdleTexture,
            HoverTexture = btnNewGame.HoverTexture,
            FontIndex = 1,
            Text = "Cancel".L10N("Client:Main:ButtonCancel"),
            HoverSoundEffect = btnNewGame.HoverSoundEffect
        };
        btnCancel.LeftClick += BtnCancel_LeftClick;

        AddChild(btnNewGame);
        AddChild(btnLoadGame);
        AddChild(btnCancel);

        base.Initialize();

        CenterOnParent();
    }

    private void BtnNewGame_LeftClick(object sender, EventArgs e)
    {
        Disable();
        NewGame?.Invoke(this, EventArgs.Empty);
    }

    private void BtnLoadGame_LeftClick(object sender, EventArgs e)
    {
        Disable();

        IniFile iniFile = new(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI));

        LoadGame?.Invoke(this, new GameLoadEventArgs(iniFile.GetIntValue("Settings", "GameID", -1)));
    }

    private void BtnCancel_LeftClick(object sender, EventArgs e)
    {
        Disable();
    }

    public void Open()
    {
        btnLoadGame.AllowClick = AllowLoadingGame();
        Enable();
    }

    private bool AllowLoadingGame()
    {
        FileInfo savedGameSpawnIniFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI);

        if (!savedGameSpawnIniFile.Exists)
        {
            return false;
        }

        IniFile iniFile = new(savedGameSpawnIniFile.FullName);
        if (iniFile.GetStringValue("Settings", "Name", string.Empty) != ProgramConstants.PLAYERNAME)
        {
            return false;
        }

        if (!iniFile.GetBooleanValue("Settings", "Host", false))
        {
            return false;
        }

        // Don't allow loading CnCNet games in LAN mode
        return !iniFile.SectionExists("Tunnel");
    }
}

public class GameLoadEventArgs : EventArgs
{
    public GameLoadEventArgs(int loadedGameId)
    {
        LoadedGameID = loadedGameId;
    }

    public int LoadedGameID { get; private set; }
}