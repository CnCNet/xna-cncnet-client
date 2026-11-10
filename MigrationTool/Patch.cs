using System.IO;
using System.Linq;
using System.Collections.Generic;

using Rampastring.Tools;
using ClientCore;
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

    public Patch AddKeyWithLog(IniFile src, string section, string key, string value, bool recursive = true)
    {
        if (src.KeyExists(section, key))
        {
            Logger.Log($"Update {src.FilePath}: Skip adding [{section}]->{key}, reason: already exists");
            return this;
        }

        if (recursive)
        {
            HashSet<string> visitedSections = [section];
            string baseSectionName = src.GetStringValue(section, "$BaseSection", string.Empty).Trim();

            while (!string.IsNullOrEmpty(baseSectionName) && visitedSections.Add(baseSectionName))
            {
                if (src.KeyExists(baseSectionName, key))
                {
                    string baseValue = src.GetStringValue(baseSectionName, key, string.Empty);
                    Logger.Log($"Update {src.FilePath}: Skip adding [{section}]->{key}, reason: already exists in base section [{baseSectionName}]->{key}={baseValue}");
                    return this;
                }

                baseSectionName = src.GetStringValue(baseSectionName, "$BaseSection", string.Empty).Trim();
            }
        }

        Logger.Log($"Update {src.FilePath}: Add [{section}]->{key}={value}");

        if (!src.SectionExists(section))
            src.AddSection(section);

        src.GetSection(section).AddKey(key, value);

        return this;
    }

    public Patch RemoveKeyWithLog(IniFile src, string section, string key)
    {
        if (!src.KeyExists(section, key))
        {
            Logger.Log($"Update {src.FilePath}: Skip removing [{section}]->{key}, reason: doesn't exist");
        }
        else
        {
            Logger.Log($"Update {src.FilePath}: Remove [{section}]->{key}={src.GetSection(section).Keys.First(kvp => kvp.Key == key).Value}");
            src.GetSection(section).RemoveKey(key);
        }

        return this;
    }

    /// <summary>
    /// Replaces a key while keeping the operation local to the file.
    /// Unlike <see cref="AddKeyWithLog"/>, an inherited target key must not suppress
    /// the value of a locally overridden source key. The target key is therefore
    /// always written locally, and the source key is removed only from the local file.
    /// </summary>
    /// <returns><see langword="true"/> if the file was changed.</returns>
    public bool ReplaceKeyWithLog(IniFile src, string sourceSection, string sourceKey,
        string targetSection, string targetKey)
    {
        if (sourceSection == targetSection && sourceKey == targetKey)
            return false;

        bool sourceIsLocal = src.KeyExists(sourceSection, sourceKey);

        if (src.KeyExists(targetSection, targetKey))
        {
            if (sourceIsLocal)
            {
                Logger.Log($"Update {src.FilePath}: Skip replacing [{sourceSection}]->{sourceKey} with [{targetSection}]->{targetKey}, reason: target already exists");
                RemoveKeyWithLog(src, sourceSection, sourceKey);
                return true;
            }

            return false;
        }

        string? replacementValue = null;

        if (sourceIsLocal)
        {
            replacementValue = src.GetStringValue(sourceSection, sourceKey, string.Empty);
        }
        else if (TryGetInheritedKeyValue(src, targetSection, targetKey, out _))
        {
            Logger.Log($"Update {src.FilePath}: Skip replacing [{sourceSection}]->{sourceKey} with [{targetSection}]->{targetKey}, reason: target already exists in a base section");
            return false;
        }
        else if (TryGetInheritedKeyValue(src, sourceSection, sourceKey, out string inheritedValue))
        {
            replacementValue = inheritedValue;
        }

        if (replacementValue == null)
            return false;

        AddKeyWithLog(src, targetSection, targetKey, replacementValue, recursive: false);

        if (sourceIsLocal)
            RemoveKeyWithLog(src, sourceSection, sourceKey);
        else
            Logger.Log($"Update {src.FilePath}: Added [{targetSection}]->{targetKey} from inherited [{sourceSection}]->{sourceKey}; the inherited source key cannot be removed");

        return true;
    }

    private static bool TryGetInheritedKeyValue(IniFile src, string section, string key, out string value)
    {
        HashSet<string> visitedSections = [section];
        string baseSectionName = src.GetStringValue(section, "$BaseSection", string.Empty).Trim();

        while (!string.IsNullOrEmpty(baseSectionName) && visitedSections.Add(baseSectionName))
        {
            if (src.KeyExists(baseSectionName, key))
            {
                value = src.GetStringValue(baseSectionName, key, string.Empty);
                return true;
            }

            baseSectionName = src.GetStringValue(baseSectionName, "$BaseSection", string.Empty).Trim();
        }

        value = string.Empty;
        return false;
    }

    public void CalculatePositions(IniFile ini, string parent, string child)
    {
        int parentX, parentY, childX, childY;
        parentX = parentY = childX = childY = 0;

        var parentKeys = ini.GetSectionKeys(parent);
        var childKeys = ini.GetSectionKeys(child);

        var positionKeys = new List<string>() { "$X", "$Y", "X", "Y", "Location" };

        Logger.Log($"Update {ini.FilePath}: Fix position for {child} control in {parent}");

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

