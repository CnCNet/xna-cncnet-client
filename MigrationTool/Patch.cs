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

    protected static ConsoleColor defaultColor = Console.ForegroundColor;

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
        Log($"Applying patch for client version {ClientVersion.ToString().Replace('_', '.')}...", ConsoleColor.White);
        return this;
    }

    public void AddKeyWithLog(IniFile src, string section, string key, string value)
    {
        if (src.KeyExists(section, key))
        {
            Log($"Update {src.FileName}: Skip adding [{section}]->{key}, reason: already exist", ConsoleColor.Red);
        }
        else
        {
            Log($"Update {src.FileName}: Add [{section}]->{key}={value}", ConsoleColor.Green);
            if (!src.SectionExists(section)) src.AddSection(section);
            src.GetSection(section).AddKey(key, value);
        }
    }

    protected void Log(string text, ConsoleColor? color = null, bool echoToConsole = true)
    {
        Console.ForegroundColor = color ?? defaultColor;
        Logger.Log(text);

        if (echoToConsole)
            Console.WriteLine(text);
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

