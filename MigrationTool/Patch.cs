using System;
using System.IO;
using System.Text.RegularExpressions;

using Rampastring.Tools;
namespace MigrationTool;

internal abstract class Patch
{
    public Version ClientVersion { get; protected set; }
    public ClientGameType Game { get; protected set; }
    public DirectoryInfo ClientDir { get; protected set; }
    public DirectoryInfo ResouresDir { get; protected set; }

    public Patch(string clientPath)
    {
        ClientDir = SafePath.GetDirectory(clientPath);
        ResouresDir = SafePath.GetDirectory(SafePath.CombineFilePath(clientPath, "Resources"));

        // Predict client type by guessing game engine files
        var Game = ClientGameType.TS;
        if (!SafePath.GetFile(SafePath.CombineFilePath(ClientDir.FullName, "Ares.dll")).Exists
            && SafePath.GetFile(SafePath.CombineFilePath(ClientDir.FullName, "gamemd-spawn.exe")).Exists)
        {
            Game = ClientGameType.YR;
        }
        else if (SafePath.GetFile(SafePath.CombineFilePath(ClientDir.FullName, "Ares.dll")).Exists)
        {
            Game = ClientGameType.Ares;
        }
    }

    public virtual Patch Apply()
    {
        Logger.Log($"Applying patch for client version {ClientVersion.ToString().Replace('_', '.')}...");
        return this;
    }

    public void AddKeyWithLog(IniFile src, string section, string key, string value)
    {
        if (src.KeyExists(section, key))
        {
            Logger.Log($"Update {src.FileName}: Skip adding [{section}]->{key}, reason: already exist");
        }
        else
        {
            Logger.Log($"Update {src.FileName}: Add [{section}]->{key}={value}");
            if (!src.SectionExists(section)) src.AddSection(section);
            src.GetSection(section).AddKey(key, value);
        }
    }

    public bool TryApply()
    {
        try
        {
            Apply();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

