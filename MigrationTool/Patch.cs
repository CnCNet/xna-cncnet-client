using System.IO;
using System.Linq;
using System.Collections.Generic;

using Rampastring.Tools;
using ClientCore.Enums;
namespace MigrationTool;

internal abstract class Patch
{
    public Version ClientVersion { get; protected set; }
    public ClientType Game { get; protected set; }
    public DirectoryInfo ClientDir { get; protected set; }
    public DirectoryInfo ResouresDir { get; protected set; }

    public Patch(string clientPath)
    {
        ClientDir = SafePath.GetDirectory(clientPath);
        ResouresDir = SafePath.GetDirectory(SafePath.CombineFilePath(clientPath, "Resources"));

        // Predict client type by guessing game engine files
        Game = ClientType.TS;

        if (SafePath.GetFile(SafePath.CombineFilePath(ClientDir.FullName, "Ares.dll")).Exists)
        {
            Game = ClientType.Ares;
        }
        else if (SafePath.GetFile(SafePath.CombineFilePath(ClientDir.FullName, "gamemd-spawn.dll")).Exists)
        {
            Game = ClientType.YR;
        }
    }

    public virtual void Apply()
    {
        Logger.Log($"Applying patch for client version {ClientVersion.ToString().Replace('_', '.')}...");
    }

    public Patch AddKeyWithLog(IniFile src, string section, string key, string value)
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

        return this;
    }

    public Patch RemoveKeyWithLog(IniFile src, string section, string key)
    {
        if (!src.KeyExists(section, key))
        {
            Logger.Log($"Update {src.FileName}: Skip removing [{section}]->{key}, reason: doesn't exist");
        }
        else
        {
            Logger.Log($"Update {src.FileName}: Remove [{section}]->{key}={src.GetSection(section).Keys.First(kvp => kvp.Key == key).Value}");
            src.GetSection(section).RemoveKey(key);
        }

        return this;
    }

    public void CalculatePositions(IniFile ini, string parent, string child)
    {
        int parentX, parentY, childX, childY;
        parentX = parentY = childX = childY = 0;

        var parentKeys = ini.GetSectionKeys(parent);
        var childKeys = ini.GetSectionKeys(child);

        var positionKeys = new List<string>() { "$X", "$Y", "X", "Y", "Location" };

        Logger.Log($"Update {ini.FileName}: Fix position for {child} control in {parent}");

        foreach (var control in new List<List<string>>() { parentKeys, childKeys })
        {
            int tmpX, tmpY;
            tmpX = tmpY = 0;

            foreach (var key in control.Where(key => positionKeys.Contains(key)))
            {
                switch (key)
                {
                    case ("$X"):
                    case ("X"):
                        tmpX = ini.GetIntValue(control == parentKeys ? parent : child, key, tmpX);
                        continue;
                    case ("$Y"):
                    case ("Y"):
                        tmpY = ini.GetIntValue(control == parentKeys ? parent : child, key, tmpY);
                        continue;
                    case ("Location"):
                        var value = ini.GetStringValue(control == parentKeys ? parent : child, key, string.Empty).Split(',');
                        tmpX = Conversions.IntFromString(value[0], tmpX);
                        tmpY = Conversions.IntFromString(value[1], tmpY);
                        break;
                    default:
                        break;
                }
            }

            if (control == parentKeys)
            {
                parentX = tmpX;
                parentY = tmpY;
            }
            else
            {
                childX = tmpX;
                childY = tmpY;
            }
        }

        positionKeys.ForEach(key => ini.RemoveKey(child, key));

        childX = childX - parentX;
        childY = childY - parentY;

        ini.GetSection(child).AddKey("$X", $"{childX}");
        ini.GetSection(child).AddKey("$Y", $"{childY}");
    }

    public Patch TransferKeys(IniFile srcIni, string srcSection, IniFile desIni, string? desSection = null)
    {
        desSection ??= srcSection;

        srcIni.GetSectionKeys(srcSection)
            .ForEach(key => AddKeyWithLog(desIni, desSection, key, srcIni.GetStringValue(srcSection, key, string.Empty)));

        return this;
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

