namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmRequestResponseSpawnSettings
{
    public string UIMapName { get; set; }

    public string MapHash { get; set; }

    public int Seed { get; set; }

    public int GameID { get; set; }

    public int WOLGameID { get; set; }

    public string Host { get; set; }

    public string IsSpectator { get; set; }

    public string Name { get; set; }

    public int Port { get; set; }

    public int Side { get; set; }

    public int Color { get; set; }

    public string GameSpeed { get; set; }

    public string Credits { get; set; }

    public string UnitCount { get; set; }

    public string SuperWeapons { get; set; }

    public string Tournament { get; set; }

    public string ShortGame { get; set; }

    public string Bases { get; set; }

    public string MCVRedeploy { get; set; }

    public string MultipleFactory { get; set; }

    public string Crates { get; set; }

    public string GameMode { get; set; }

    public string FrameSendRate { get; set; }

    public string DisableSWvsYuri { get; set; }
}