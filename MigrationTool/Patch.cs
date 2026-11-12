using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Rampastring.Tools;
using ClientCore;
namespace MigrationTool;

/// <summary>
/// Base class for a client migration patch.
/// A patch must only implement changes introduced in the patch's client version.
/// A patch must not rely on later-version patches or implement later-version behavior.
/// Patches are applied in version order for a full migration, but each patch must
/// remain valid when applied alone.
/// </summary>
internal abstract class Patch
{
    // TODO: The key and section helpers only resolve $BaseSection inheritance. INISystem/BasedOn file inheritance is not supported.

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
        CopyVersionPicturesWithLog();
    }

    /// <summary>
    /// Adds a key to a section.
    /// </summary>
    /// <param name="src">The INI file to modify.</param>
    /// <param name="section">The target section.</param>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to assign to the key.</param>
    /// <param name="skipIfInherited">
    /// Whether a key inherited through the <c>$BaseSection</c> chain counts as existing.
    /// </param>
    /// <returns><see langword="true"/> if the file was changed.</returns>
    public static bool AddKeyWithLog(IniFile src, string section, string key, string value, bool skipIfInherited = true)
    {
        if (src.KeyExists(section, key))
        {
            Logger.Log($"Update {src.FilePath}: Skip adding [{section}]->{key}, reason: already exists");
            return false;
        }

        if (skipIfInherited && TryGetKeyFromBaseSection(src, section, key, out string baseSectionName, out string baseValue))
        {
            Logger.Log($"Update {src.FilePath}: Skip adding [{section}]->{key}, reason: already exists in base section [{baseSectionName}]->{key}={baseValue}");
            return false;
        }

        Logger.Log($"Update {src.FilePath}: Add [{section}]->{key}={value}");

        if (!src.SectionExists(section))
            src.AddSection(section);

        src.GetSection(section).AddKey(key, value);

        return true;
    }

    /// <summary>
    /// Removes a key from a section. By default only local keys are removed.
    /// </summary>
    /// <param name="src">The INI file to modify.</param>
    /// <param name="section">The section that contains the key.</param>
    /// <param name="key">The key to remove.</param>
    /// <param name="removeInherited">
    /// Whether to also remove the key from every base section in the <c>$BaseSection</c>
    /// chain. Removing the key from a base section also affects other sections that
    /// inherit from the base section.
    /// </param>
    /// <returns><see langword="true"/> if the file was changed.</returns>
    public static bool RemoveKeyWithLog(IniFile src, string section, string key, bool removeInherited = false)
    {
        bool removed = false;

        if (src.KeyExists(section, key))
        {
            Logger.Log($"Update {src.FilePath}: Remove [{section}]->{key}={src.GetSection(section).Keys.First(kvp => kvp.Key == key).Value}");
            src.GetSection(section).RemoveKey(key);
            removed = true;
        }

        if (removeInherited)
        {
            while (TryGetKeyFromBaseSection(src, section, key, out string baseSectionName, out string baseValue))
            {
                Logger.Log($"Update {src.FilePath}: Remove [{baseSectionName}]->{key}={baseValue}");
                src.GetSection(baseSectionName).RemoveKey(key);
                removed = true;
            }
        }

        if (!removed)
            Logger.Log($"Update {src.FilePath}: Skip removing [{section}]->{key}, reason: doesn't exist");

        return removed;
    }

    private void CopyVersionPicturesWithLog()
    {
        string resourcePrefix = $"{nameof(MigrationTool)}.Pictures.{ClientVersion}.";
        Assembly assembly = Assembly.GetExecutingAssembly();

        foreach (string resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(resourcePrefix, StringComparison.Ordinal))
                continue;

            string fileName = resourceName.Substring(resourcePrefix.Length);
            CopyEmbeddedResourceWithLog(assembly, resourceName, fileName, ResouresDir);
        }
    }

    private static void CopyEmbeddedResourceWithLog(Assembly assembly, string resourceName, string fileName, DirectoryInfo destinationDir)
    {
        using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);

        if (resourceStream == null)
        {
            Logger.Log($"Unable to find embedded resource {resourceName}, skipping copying {fileName}.");
            return;
        }

        string destinationPath = SafePath.CombineFilePath(destinationDir.FullName, fileName);

        if (File.Exists(destinationPath))
        {
            Logger.Log($"Skip copying {fileName} to {destinationDir.FullName}, reason: already exists");
            return;
        }

        using FileStream fileStream = new FileStream(destinationPath, FileMode.CreateNew);
        resourceStream.CopyTo(fileStream);
        Logger.Log($"Copy {fileName} to {destinationDir.FullName}");
    }

    /// <summary>
    /// Replaces a legacy key with the current section and key name.
    /// </summary>
    /// <param name="src">The INI file to modify.</param>
    /// <param name="legacySection">The section that contains the legacy key.</param>
    /// <param name="legacyKey">The legacy key to replace.</param>
    /// <param name="currentSection">The section that contains the current key.</param>
    /// <param name="currentKey">The current key to write.</param>
    /// <param name="useInheritedLegacyValue">
    /// Whether an inherited legacy value may be used when the legacy key is not defined
    /// locally. When false, only local keys are considered.
    /// </param>
    /// <returns><see langword="true"/> if the file was changed.</returns>
    /// <remarks>
    /// The current key wins when the current key exists locally. A local legacy override
    /// is copied to the current key even when the current key is inherited. Inherited
    /// legacy keys are copied to a local current key and are not removed from the base
    /// section.
    /// </remarks>
    public static bool ReplaceKeyWithLog(IniFile src, string legacySection, string legacyKey, string currentSection,
        string currentKey, bool useInheritedLegacyValue = true)
    {
        if (legacySection == currentSection && legacyKey == currentKey)
            return false;

        if (src.KeyExists(currentSection, currentKey))
        {
            if (src.KeyExists(legacySection, legacyKey))
            {
                Logger.Log($"Update {src.FilePath}: Skip replacing [{legacySection}]->{legacyKey} with [{currentSection}]->{currentKey}, reason: current key already exists");
                RemoveKeyWithLog(src, legacySection, legacyKey);
                return true;
            }

            return false;
        }

        string? legacyValue = null;
        bool legacyIsFromBaseSection = false;

        if (src.KeyExists(legacySection, legacyKey))
        {
            legacyValue = src.GetStringValue(legacySection, legacyKey, string.Empty);
        }
        else if (useInheritedLegacyValue && TryGetKeyFromBaseSection(src, currentSection, currentKey, out string baseSectionName, out _))
        {
            Logger.Log($"Update {src.FilePath}: Skip replacing [{legacySection}]->{legacyKey} with [{currentSection}]->{currentKey}, reason: current key already exists in base section [{baseSectionName}]");
            return false;
        }
        else if (useInheritedLegacyValue && TryGetKeyFromBaseSection(src, legacySection, legacyKey, out _, out string inheritedValue))
        {
            legacyValue = inheritedValue;
            legacyIsFromBaseSection = true;
        }

        if (legacyValue == null)
            return false;

        // Write the current key locally without checking base sections. A base-section
        // lookup would incorrectly skip the write when the current key is inherited.
        AddKeyWithLog(src, currentSection, currentKey, legacyValue, skipIfInherited: false);

        if (legacyIsFromBaseSection)
            Logger.Log($"Update {src.FilePath}: Old key [{legacySection}]->{legacyKey} comes from a base section. The old key value was copied to new local key [{currentSection}]->{currentKey}. The base section was left untouched.");
        else
            RemoveKeyWithLog(src, legacySection, legacyKey);

        return true;
    }

    /// <summary>
    /// Renames a section.
    /// </summary>
    /// <param name="src">The INI file to modify.</param>
    /// <param name="legacySection">The section to rename.</param>
    /// <param name="currentSection">The new section name.</param>
    /// <returns><see langword="true"/> if the file was changed.</returns>
    /// <remarks>
    /// The section is renamed in place, preserving all keys including
    /// <c>$BaseSection</c>. If the target section already exists, the rename is skipped.
    /// </remarks>
    public static bool RenameSectionWithLog(IniFile src, string legacySection, string currentSection)
    {
        if (legacySection == currentSection || !src.SectionExists(legacySection))
            return false;

        if (src.SectionExists(currentSection))
        {
            Logger.Log($"Update {src.FilePath}: Skip renaming [{legacySection}] to [{currentSection}], reason: target section already exists");
            return false;
        }

        src.GetSection(legacySection).SectionName = currentSection;
        Logger.Log($"Update {src.FilePath}: Rename [{legacySection}] to [{currentSection}]");
        return true;
    }

    /// <summary>
    /// Copies the local keys of a section into another section.
    /// </summary>
    /// <param name="sourceIni">The INI file that contains the source section.</param>
    /// <param name="sourceSection">The section whose keys are copied.</param>
    /// <param name="targetIni">The INI file that contains the target section.</param>
    /// <param name="targetSection">The section that receives the keys.</param>
    /// <param name="overwriteExistingKeys">
    /// Whether source local keys and <c>$BaseSection</c> overwrite existing target local
    /// values. When false, target local values win.
    /// </param>
    /// <returns><see langword="true"/> if the target file was changed.</returns>
    /// <remarks>
    /// A missing target <c>$BaseSection</c> is created from the source. If the target
    /// does not contain the referenced base section, a warning is logged and the
    /// reference is copied anyway. Inherited target keys do not block the copy. Source
    /// inherited keys are not expanded.
    /// </remarks>
    public static bool CopySectionWithLog(IniFile sourceIni, string sourceSection, IniFile targetIni,
        string targetSection, bool overwriteExistingKeys = false)
    {
        if (!sourceIni.SectionExists(sourceSection))
            return false;

        bool changed = false;

        foreach (string key in sourceIni.GetSectionKeys(sourceSection))
        {
            if (key == "$BaseSection")
                continue;

            string value = sourceIni.GetStringValue(sourceSection, key, string.Empty);

            if (targetIni.KeyExists(targetSection, key))
            {
                if (!overwriteExistingKeys)
                    continue;

                targetIni.SetStringValue(targetSection, key, value);
                Logger.Log($"Update {targetIni.FilePath}: Set [{targetSection}]->{key}={value}");
                changed = true;
                continue;
            }

            changed |= AddKeyWithLog(targetIni, targetSection, key, value, skipIfInherited: false);
        }

        if (sourceIni.KeyExists(sourceSection, "$BaseSection"))
        {
            string sourceBaseSection = sourceIni.GetStringValue(sourceSection, "$BaseSection", string.Empty);

            if (!targetIni.KeyExists(targetSection, "$BaseSection") || overwriteExistingKeys)
            {
                if (!targetIni.SectionExists(sourceBaseSection))
                    Logger.Log($"Update {targetIni.FilePath}: Base section [{sourceBaseSection}] does not exist in target, copying $BaseSection to [{targetSection}] anyway");

                targetIni.SetStringValue(targetSection, "$BaseSection", sourceBaseSection);
                Logger.Log($"Update {targetIni.FilePath}: Set [{targetSection}]->$BaseSection={sourceBaseSection}");
                changed = true;
            }
            else
            {
                Logger.Log($"Update {targetIni.FilePath}: Skip copying $BaseSection to [{targetSection}], reason: target already defines $BaseSection");
            }
        }

        return changed;
    }

    private static bool TryGetKeyFromBaseSection(IniFile src, string section, string key,
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

