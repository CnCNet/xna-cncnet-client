using System;
using System.Collections.Generic;
using System.IO;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.Settings;

using ClientGUI;

using ClientUpdater;

using DTAClient.Domain;

using Microsoft.Xna.Framework;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic;

public class CampaignSelector : XNAWindow
{
    private const int DEFAULT_WIDTH = 650;
    private const int DEFAULT_HEIGHT = 600;

    private static readonly string[] DifficultyNames = new string[] { "Easy", "Medium", "Hard" };

    private static readonly string[] DifficultyIniPaths = new string[]
    {
        "INI/Map Code/Difficulty Easy.ini",
        "INI/Map Code/Difficulty Medium.ini",
        "INI/Map Code/Difficulty Hard.ini"
    };

    public CampaignSelector(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager)
    {
        this.discordHandler = discordHandler;
    }

    private readonly DiscordHandler discordHandler;

    private readonly List<Mission> Missions = [];
    private XNAListBox lbCampaignList;
    private XNAClientButton btnLaunch;
    private XNATextBlock tbMissionDescription;
    private XNATrackbar trbDifficultySelector;

    private CheaterWindow cheaterWindow;

    private readonly string[] filesToCheck = new string[]
    {
        "INI/AI.ini",
        "INI/AIE.ini",
        "INI/Art.ini",
        "INI/ArtE.ini",
        "INI/Enhance.ini",
        "INI/Rules.ini",
        "INI/Map Code/Difficulty Hard.ini",
        "INI/Map Code/Difficulty Medium.ini",
        "INI/Map Code/Difficulty Easy.ini"
    };

    private Mission missionToLaunch;

    public override void Initialize()
    {
        BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
        ClientRectangle = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);
        BorderColor = UISettings.ActiveSettings.PanelBorderColor;

        Name = "CampaignSelector";

        XNALabel lblSelectCampaign = new(WindowManager)
        {
            Name = "lblSelectCampaign",
            FontIndex = 1,
            ClientRectangle = new Rectangle(12, 12, 0, 0),
            Text = "MISSIONS:".L10N("Client:Main:Missions")
        };

        lbCampaignList = new XNAListBox(WindowManager)
        {
            Name = "lbCampaignList",
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2),
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED,
            ClientRectangle = new Rectangle(12,
            lblSelectCampaign.Bottom + 6, 300, 516)
        };
        lbCampaignList.SelectedIndexChanged += LbCampaignList_SelectedIndexChanged;

        XNALabel lblMissionDescriptionHeader = new(WindowManager)
        {
            Name = "lblMissionDescriptionHeader",
            FontIndex = 1,
            ClientRectangle = new Rectangle(
            lbCampaignList.Right + 12,
            lblSelectCampaign.Y, 0, 0),
            Text = "MISSION DESCRIPTION:".L10N("Client:Main:MissionDescription")
        };

        tbMissionDescription = new XNATextBlock(WindowManager)
        {
            Name = "tbMissionDescription",
            ClientRectangle = new Rectangle(
            lblMissionDescriptionHeader.X,
            lblMissionDescriptionHeader.Bottom + 6,
            Width - 24 - lbCampaignList.Right, 430),
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED,
            Alpha = 1.0f
        };

        tbMissionDescription.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor),
            tbMissionDescription.Width, tbMissionDescription.Height);

        XNALabel lblDifficultyLevel = new(WindowManager)
        {
            Name = "lblDifficultyLevel",
            Text = "DIFFICULTY LEVEL".L10N("Client:Main:DifficultyLevel"),
            FontIndex = 1
        };
        Vector2 textSize = Renderer.GetTextDimensions(lblDifficultyLevel.Text, lblDifficultyLevel.FontIndex);
        lblDifficultyLevel.ClientRectangle = new Rectangle(
            tbMissionDescription.X + ((tbMissionDescription.Width - (int)textSize.X) / 2),
            tbMissionDescription.Bottom + 12, (int)textSize.X, (int)textSize.Y);

        trbDifficultySelector = new XNATrackbar(WindowManager)
        {
            Name = "trbDifficultySelector",
            ClientRectangle = new Rectangle(
            tbMissionDescription.X, lblDifficultyLevel.Bottom + 6,
            tbMissionDescription.Width, 30),
            MinValue = 0,
            MaxValue = 2,
            BackgroundTexture = AssetLoader.CreateTexture(
            new Color(0, 0, 0, 128), 2, 2),
            ButtonTexture = AssetLoader.LoadTextureUncached(
            "trackbarButton_difficulty.png")
        };

        XNALabel lblEasy = new(WindowManager)
        {
            Name = "lblEasy",
            FontIndex = 1,
            Text = "EASY".L10N("Client:Main:DifficultyEasy"),
            ClientRectangle = new Rectangle(trbDifficultySelector.X,
            trbDifficultySelector.Bottom + 6, 1, 1)
        };

        XNALabel lblNormal = new(WindowManager)
        {
            Name = "lblNormal",
            FontIndex = 1,
            Text = "NORMAL".L10N("Client:Main:DifficultyNormal")
        };
        textSize = Renderer.GetTextDimensions(lblNormal.Text, lblNormal.FontIndex);
        lblNormal.ClientRectangle = new Rectangle(
            tbMissionDescription.X + ((tbMissionDescription.Width - (int)textSize.X) / 2),
            lblEasy.Y, (int)textSize.X, (int)textSize.Y);

        XNALabel lblHard = new(WindowManager)
        {
            Name = "lblHard",
            FontIndex = 1,
            Text = "HARD".L10N("Client:Main:DifficultyHard")
        };
        lblHard.ClientRectangle = new Rectangle(
            tbMissionDescription.Right - lblHard.Width,
            lblEasy.Y, 1, 1);

        btnLaunch = new XNAClientButton(WindowManager)
        {
            Name = "btnLaunch",
            ClientRectangle = new Rectangle(12, Height - 35, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Launch".L10N("Client:Main:ButtonLaunch"),
            AllowClick = false
        };
        btnLaunch.LeftClick += BtnLaunch_LeftClick;

        XNAClientButton btnCancel = new(WindowManager)
        {
            Name = "btnCancel",
            ClientRectangle = new Rectangle(Width - 145,
            btnLaunch.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Cancel".L10N("Client:Main:ButtonCancel")
        };
        btnCancel.LeftClick += BtnCancel_LeftClick;

        AddChild(lblSelectCampaign);
        AddChild(lblMissionDescriptionHeader);
        AddChild(lbCampaignList);
        AddChild(tbMissionDescription);
        AddChild(lblDifficultyLevel);
        AddChild(btnLaunch);
        AddChild(btnCancel);
        AddChild(trbDifficultySelector);
        AddChild(lblEasy);
        AddChild(lblNormal);
        AddChild(lblHard);

        // Set control attributes from INI file
        base.Initialize();

        // Center on screen
        CenterOnParent();

        trbDifficultySelector.Value = UserINISettings.Instance.Difficulty;

        ReadMissionList();

        cheaterWindow = new CheaterWindow(WindowManager);
        DarkeningPanel dp = new(WindowManager);
        dp.AddChild(cheaterWindow);
        AddChild(dp);
        dp.CenterOnParent();
        cheaterWindow.CenterOnParent();
        cheaterWindow.YesClicked += CheaterWindow_YesClicked;
        cheaterWindow.Disable();
    }

    private void LbCampaignList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lbCampaignList.SelectedIndex == -1)
        {
            tbMissionDescription.Text = string.Empty;
            btnLaunch.AllowClick = false;
            return;
        }

        Mission mission = Missions[lbCampaignList.SelectedIndex];

        if (string.IsNullOrEmpty(mission.Scenario))
        {
            tbMissionDescription.Text = string.Empty;
            btnLaunch.AllowClick = false;
            return;
        }

        tbMissionDescription.Text = mission.GUIDescription;

        if (!mission.Enabled)
        {
            btnLaunch.AllowClick = false;
            return;
        }

        btnLaunch.AllowClick = true;
    }

    private void BtnCancel_LeftClick(object sender, EventArgs e)
    {
        Enabled = false;
    }

    private void BtnLaunch_LeftClick(object sender, EventArgs e)
    {
        int selectedMissionId = lbCampaignList.SelectedIndex;

        Mission mission = Missions[selectedMissionId];

        if (!ClientConfiguration.Instance.ModMode &&
            (!Updater.IsFileNonexistantOrOriginal(mission.Scenario) || AreFilesModified()))
        {
            // Confront the user by showing the cheater screen
            missionToLaunch = mission;
            cheaterWindow.Enable();
            return;
        }

        LaunchMission(mission);
    }

    private bool AreFilesModified()
    {
        foreach (string filePath in filesToCheck)
        {
            if (!Updater.IsFileNonexistantOrOriginal(filePath))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Called when the user wants to proceed to the mission despite having
    /// being called a cheater.
    /// </summary>
    private void CheaterWindow_YesClicked(object sender, EventArgs e)
    {
        LaunchMission(missionToLaunch);
    }

    /// <summary>
    /// Starts a singleplayer mission.
    /// </summary>
    private void LaunchMission(Mission mission)
    {
        bool copyMapsToSpawnmapINI = ClientConfiguration.Instance.CopyMissionsToSpawnmapINI;

        Logger.Log("About to write spawn.ini.");
        using (StreamWriter spawnStreamWriter = new(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawn.ini")))
        {
            spawnStreamWriter.WriteLine("; Generated by DTA Client");
            spawnStreamWriter.WriteLine("[Settings]");
            if (copyMapsToSpawnmapINI)
            {
                spawnStreamWriter.WriteLine("Scenario=spawnmap.ini");
            }
            else
            {
                spawnStreamWriter.WriteLine("Scenario=" + mission.Scenario);
            }

            // No one wants to play missions on Fastest, so we'll change it to Faster
            if (UserINISettings.Instance.GameSpeed == 0)
            {
                UserINISettings.Instance.GameSpeed.Value = 1;
            }

            spawnStreamWriter.WriteLine("CampaignID=" + mission.Index);
            spawnStreamWriter.WriteLine("GameSpeed=" + UserINISettings.Instance.GameSpeed);
#if YR || ARES
            spawnStreamWriter.WriteLine("Ra2Mode=" + !mission.RequiredAddon);
#else
            spawnStreamWriter.WriteLine("Firestorm=" + mission.RequiredAddon);
#endif
            spawnStreamWriter.WriteLine("CustomLoadScreen=" + LoadingScreenController.GetLoadScreenName(mission.Side.ToString()));
            spawnStreamWriter.WriteLine("IsSinglePlayer=Yes");
            spawnStreamWriter.WriteLine("SidebarHack=" + ClientConfiguration.Instance.SidebarHack);
            spawnStreamWriter.WriteLine("Side=" + mission.Side);
            spawnStreamWriter.WriteLine("BuildOffAlly=" + mission.BuildOffAlly);

            UserINISettings.Instance.Difficulty.Value = trbDifficultySelector.Value;

            spawnStreamWriter.WriteLine("DifficultyModeHuman=" + (mission.PlayerAlwaysOnNormalDifficulty ? "1" : trbDifficultySelector.Value.ToString()));
            spawnStreamWriter.WriteLine("DifficultyModeComputer=" + GetComputerDifficulty());

            spawnStreamWriter.WriteLine();
            spawnStreamWriter.WriteLine();
            spawnStreamWriter.WriteLine();
        }

        IniFile difficultyIni = new(SafePath.CombineFilePath(ProgramConstants.GamePath, DifficultyIniPaths[trbDifficultySelector.Value]));
        string difficultyName = DifficultyNames[trbDifficultySelector.Value];

        if (copyMapsToSpawnmapINI)
        {
            IniFile mapIni = new(SafePath.CombineFilePath(ProgramConstants.GamePath, mission.Scenario));
            IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
            mapIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawnmap.ini"));
        }

        UserINISettings.Instance.Difficulty.Value = trbDifficultySelector.Value;
        UserINISettings.Instance.SaveSettings();

        ((MainMenuDarkeningPanel)Parent).Hide();

        discordHandler.UpdatePresence(mission.UntranslatedGUIName, difficultyName, mission.IconPath, true);
        GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

        GameProcessLogic.StartGameProcess(WindowManager);
    }

    private int GetComputerDifficulty()
    {
        return Math.Abs(trbDifficultySelector.Value - 2);
    }

    private void GameProcessExited_Callback()
    {
        WindowManager.AddCallback(new Action(GameProcessExited), null);
    }

    protected virtual void GameProcessExited()
    {
        GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;
        // Logger.Log("GameProcessExited: Updating Discord Presence.");
        discordHandler.UpdatePresence();
    }

    private void ReadMissionList()
    {
        _ = ParseBattleIni("INI/Battle.ini");

        if (Missions.Count == 0)
        {
            _ = ParseBattleIni("INI/" + ClientConfiguration.Instance.BattleFSFileName);
        }
    }

    /// <summary>
    /// Parses a Battle(E).ini file. Returns true if succesful (file found), otherwise false.
    /// </summary>
    /// <param name="path">The path of the file, relative to the game directory.</param>
    /// <returns>True if succesful, otherwise false.</returns>
    private bool ParseBattleIni(string path)
    {
        Logger.Log("Attempting to parse " + path + " to populate mission list.");

        FileInfo battleIniFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);
        if (!battleIniFileInfo.Exists)
        {
            Logger.Log("File " + path + " not found. Ignoring.");
            return false;
        }

        if (Missions.Count > 0)
        {
            throw new InvalidOperationException("Loading multiple Battle*.ini files is not supported anymore.");
        }

        IniFile battleIni = new(battleIniFileInfo.FullName);

        List<string> battleKeys = battleIni.GetSectionKeys("Battles");

        if (battleKeys == null)
        {
            return false; // File exists but [Battles] doesn't
        }

        for (int i = 0; i < battleKeys.Count; i++)
        {
            string battleEntry = battleKeys[i];
            string battleSection = battleIni.GetStringValue("Battles", battleEntry, "NOT FOUND");

            if (!battleIni.SectionExists(battleSection))
            {
                continue;
            }

            Mission mission = new(battleIni, battleSection, i);

            Missions.Add(mission);

            XNAListBoxItem item = new()
            {
                Text = mission.GUIName
            };
            if (!mission.Enabled)
            {
                item.TextColor = UISettings.ActiveSettings.DisabledItemColor;
            }
            else if (string.IsNullOrEmpty(mission.Scenario))
            {
                item.TextColor = AssetLoader.GetColorFromString(
                    ClientConfiguration.Instance.ListBoxHeaderColor);
                item.IsHeader = true;
                item.Selectable = false;
            }
            else
            {
                item.TextColor = lbCampaignList.DefaultItemColor;
            }

            if (!string.IsNullOrEmpty(mission.IconPath))
            {
                item.Texture = AssetLoader.LoadTexture(mission.IconPath + "icon.png");
            }

            lbCampaignList.AddItem(item);
        }

        Logger.Log("Finished parsing " + path + ".");
        return true;
    }

    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
    }
}