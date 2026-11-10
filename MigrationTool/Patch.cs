using System.IO;
using System.Linq;
using System.Collections.Generic;

using Rampastring.Tools;
using ClientCore;
namespace MigrationTool;

/// <summary>
/// Base class for a client migration patch.
/// A patch must only implement changes introduced in its own client version.
/// It must not rely on patches for later versions or implement their behavior:
/// patches are applied in version order for a full migration, but each patch
/// must remain valid when applied on its own.
/// </summary>
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

        if (recursive && TryGetBaseSectionKey(src, section, key, out string baseSectionName, out string baseValue))
        {
            Logger.Log($"Update {src.FilePath}: Skip adding [{section}]->{key}, reason: already exists in base section [{baseSectionName}]->{key}={baseValue}");
            return this;
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
    /// Replaces a legacy key with its current section and name.
    /// The current key is authoritative: if it already exists locally, it wins and
    /// only the local legacy key is removed. If the legacy key is locally overridden
    /// while the current key is only inherited, the legacy value is written locally
    /// so the override is not lost. Inherited legacy keys cannot be removed; their
    /// value is copied to a local current key instead and the legacy key is then
    /// ignored by the migrated client.
    /// </summary>
    /// <returns><see langword="true"/> if the file was changed.</returns>
    public bool ReplaceKeyWithLog(IniFile src, string legacySection, string legacyKey,
        string currentSection, string currentKey)
    {
        if (legacySection == currentSection && legacyKey == currentKey)
            return false;

        bool legacyIsLocal = src.KeyExists(legacySection, legacyKey);

        if (src.KeyExists(currentSection, currentKey))
        {
            if (legacyIsLocal)
            {
                Logger.Log($"Update {src.FilePath}: Skip replacing [{legacySection}]->{legacyKey} with [{currentSection}]->{currentKey}, reason: current key already exists");
                RemoveKeyWithLog(src, legacySection, legacyKey);
                return true;
            }

            return false;
        }

        string? legacyValue = null;

        if (legacyIsLocal)
        {
            legacyValue = src.GetStringValue(legacySection, legacyKey, string.Empty);
        }
        else if (TryGetBaseSectionKey(src, currentSection, currentKey, out string currentBaseSectionName, out _))
        {
            Logger.Log($"Update {src.FilePath}: Skip replacing [{legacySection}]->{legacyKey} with [{currentSection}]->{currentKey}, reason: current key already exists in base section [{currentBaseSectionName}]");
            return false;
        }
        else if (TryGetBaseSectionKey(src, legacySection, legacyKey, out _, out string inheritedValue))
        {
            legacyValue = inheritedValue;
        }

        if (legacyValue == null)
            return false;

        // The current key is absent locally, so this only writes the replacement
        // locally; recursive lookup would incorrectly skip it when the current key
        // is inherited from a base section.
        AddKeyWithLog(src, currentSection, currentKey, legacyValue, recursive: false);

        if (legacyIsLocal)
            RemoveKeyWithLog(src, legacySection, legacyKey);
        else
            Logger.Log($"Update {src.FilePath}: Added [{currentSection}]->{currentKey} from inherited [{legacySection}]->{legacyKey}; the inherited legacy key cannot be removed");

        return true;
    }

    /// <summary>
    /// Renames a section, or merges its keys into the current section when both exist.
    /// The current section wins for keys it already defines. Structural inheritance keys
    /// are not moved between sections.
    /// </summary>
    /// <returns><see langword="true"/> if the file was changed.</returns>
    public bool MoveSectionWithLog(IniFile src, string legacySection, string currentSection)
    {
        if (legacySection == currentSection || !src.SectionExists(legacySection))
            return false;

        if (!src.SectionExists(currentSection))
        {
            src.GetSection(legacySection).SectionName = currentSection;
            Logger.Log($"Update {src.FilePath}: Rename [{legacySection}] to [{currentSection}]");
            return true;
        }

        foreach (string key in src.GetSectionKeys(legacySection))
        {
            if (key == "$BaseSection")
                continue;

            ReplaceKeyWithLog(src, legacySection, key, currentSection, key);
        }

        src.RemoveSection(legacySection);
        Logger.Log($"Update {src.FilePath}: Merge [{legacySection}] into [{currentSection}]");
        return true;
    }

    /// <summary>
    /// Copies the keys of a section into another section in the target INI file.
    /// Existing local target keys win; structural inheritance keys are not copied.
    /// </summary>
    public void CopySectionWithLog(IniFile sourceIni, string sourceSection,
        IniFile targetIni, string targetSection)
    {
        if (!sourceIni.SectionExists(sourceSection))
            return;

        foreach (string key in sourceIni.GetSectionKeys(sourceSection))
        {
            if (key == "$BaseSection")
                continue;

            AddKeyWithLog(targetIni, targetSection, key,
                sourceIni.GetStringValue(sourceSection, key, string.Empty), recursive: false);
        }
    }

    private static bool TryGetBaseSectionKey(IniFile src, string section, string key,
        out string baseSectionName, out string value)
    {
        HashSet<string> visitedSections = [section];
        string currentSectionName = src.GetStringValue(section, "$BaseSection", string.Empty).Trim();

        while (!string.IsNullOrEmpty(currentSectionName) && visitedSections.Add(currentSectionName))
        {
            if (src.KeyExists(currentSectionName, key))
            {
                baseSectionName = currentSectionName;
                value = src.GetStringValue(currentSectionName, key, string.Empty);
                return true;
            }

            currentSectionName = src.GetStringValue(currentSectionName, "$BaseSection", string.Empty).Trim();
        }

        baseSectionName = string.Empty;
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

