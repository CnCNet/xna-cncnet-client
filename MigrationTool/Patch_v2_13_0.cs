#nullable enable
using System.IO;

using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_v2_13_0 : Patch
{
    private const string LEGACY_TEAMS_SECTION = "chkBoxForceRandomTeams";
    private const string TEAMS_SECTION = "chkBoxForceNoTeams";

    public Patch_v2_13_0(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.v2_13_0;
    }

    public override void Apply()
    {
        base.Apply();

        MigratePlayerExtraOptionsPanel();
    }

    private void MigratePlayerExtraOptionsPanel()
    {
        string gameLobbyBasePath = SafePath.CombineFilePath(ResouresDir.FullName, "GameLobbyBase.ini");
        string playerExtraOptionsPanelPath = SafePath.CombineFilePath(ResouresDir.FullName, "PlayerExtraOptionsPanel.ini");

        if (!File.Exists(gameLobbyBasePath))
        {
            Logger.Log("No GameLobbyBase.ini found, skipping the PlayerExtraOptionsPanel migration.");
            return;
        }

        IniFile gameLobbyBaseIni = new IniFile(gameLobbyBasePath, applyBaseIni: false);
        bool changed = RenameLegacyTeamsSection(gameLobbyBaseIni);

        if (!File.Exists(playerExtraOptionsPanelPath))
        {
            if (changed)
                gameLobbyBaseIni.WriteIniFile();

            Logger.Log("No PlayerExtraOptionsPanel.ini found, nothing to append to GameLobbyBase.ini.");
            return;
        }

        IniFile playerExtraOptionsPanelIni = new IniFile(playerExtraOptionsPanelPath, applyBaseIni: false);

        foreach (string section in playerExtraOptionsPanelIni.GetSections())
        {
            if (section == "INISystem")
                continue;

            string targetSection = section == LEGACY_TEAMS_SECTION ? TEAMS_SECTION : section;

            CopySectionNonRecursiveWithLog(playerExtraOptionsPanelIni, section, gameLobbyBaseIni, targetSection);
        }

        gameLobbyBaseIni.WriteIniFile();
        SafePath.DeleteFileIfExists(playerExtraOptionsPanelPath);
        Logger.Log($"Update {gameLobbyBasePath}: Appended PlayerExtraOptionsPanel.ini and removed the obsolete file.");
    }

    private bool RenameLegacyTeamsSection(IniFile gameLobbyBaseIni)
        => MoveSectionRecursiveWithLog(gameLobbyBaseIni, LEGACY_TEAMS_SECTION, TEAMS_SECTION);
}
