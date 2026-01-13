#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using Microsoft.Win32;

using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.Domain;

/// <summary>
/// Handles checking and fixing DirectDraw compatibility issues with user interaction.
/// </summary>
[SupportedOSPlatform("windows")]
public static class DirectDrawCompatibilityChecker
{
    private static readonly IReadOnlyList<string> OSCompatibilityValues = [
        "WIN8RTM", "WIN7RTM", "VISTASP2", "VISTASP1", "VISTARTM", "WINXPSP3", "WINXPSP2", "WIN98", "WIN95"
    ];

    private static IEnumerable<string> GetExecutableFilePathsToCheck()
    {
        List<string> executablePaths = ClientConfiguration.Instance.GetCompatibilityCheckExecutables()
            .Select(executableName => SafePath.CombineFilePath(ProgramConstants.GamePath, executableName))
            .ToList();

        // clientdx.exe, clientogl.exe, or clientxna.exe
        string currentExePath = SafePath.GetFile(ProgramConstants.StartupExecutable).FullName;

        executablePaths.Add(currentExePath);

        Logger.Log("Checking compatibility settings for executables: " +
                   string.Join(", ", executablePaths));

        return executablePaths;
    }

    private static void Examine(out bool requireFix, out bool requireAdmin, out IEnumerable<string> problematicExeNames)
    {
        RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

        using RegistryKey? hkcuKey = hkcu.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers");
        using RegistryKey? hklmKey = hklm.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers");

        static bool IsFixRequired(object? regValue)
            => regValue is string regValueString
               && regValueString.Split([' ']).Intersect(OSCompatibilityValues).Any();

        bool anyHkcuRequireFix = false;
        bool anyHklmRequireFix = false;

        var problematicExeNameHashSet = new HashSet<string>();
        foreach (string exeFullPath in GetExecutableFilePathsToCheck())
        {
            object? hkcuValue = hkcuKey?.GetValue(exeFullPath);
            object? hklmValue = hklmKey?.GetValue(exeFullPath);

            if (IsFixRequired(hkcuValue))
            {
                Logger.Log($"Executable '{exeFullPath}' has problematic compatibility settings in HKCU. Value: {hkcuValue}");
                anyHkcuRequireFix = true;
                problematicExeNameHashSet.Add(Path.GetFileName(exeFullPath));
            }

            if (IsFixRequired(hklmValue))
            {
                Logger.Log($"Executable '{exeFullPath}' has problematic compatibility settings in HKLM. Value: {hklmValue}");
                anyHklmRequireFix = true;
                problematicExeNameHashSet.Add(Path.GetFileName(exeFullPath));
            }
        }

        requireFix = anyHkcuRequireFix || anyHklmRequireFix;
        requireAdmin = anyHklmRequireFix;
        problematicExeNames = problematicExeNameHashSet;
    }

    private static string FixCompatLayerString(string value) => string.Join(" ",
            value
                .SplitWithCleanup(new[] { ' ' })
                .Where(v => !OSCompatibilityValues.Contains(v, StringComparer.InvariantCultureIgnoreCase)));

    private static void Fix()
    {
        void FixRegValue(object? regValue, out bool success, out string newRegValue)
        {
            if (regValue is string regValueString)
            {
                newRegValue = FixCompatLayerString(regValueString);
                success = true;
            }
            else
            {
                success = false;
                newRegValue = string.Empty;
            }
        }

        void FixRegistryKey(RegistryKey rootKey, string subKeyPath)
        {
            try
            {
                using RegistryKey? key = rootKey.OpenSubKey(subKeyPath, writable: true);
                if (key == null)
                    return;

                foreach (string exeFullPath in GetExecutableFilePathsToCheck())
                {
                    object? value = key.GetValue(exeFullPath);

                    FixRegValue(value, out bool success, out string newValue);

                    if (success)
                    {
                        if (string.IsNullOrEmpty(newValue))
                            key.DeleteValue(exeFullPath, false);
                        else
                            key.SetValue(exeFullPath, newValue, RegistryValueKind.String);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to fix registry key {rootKey.Name}\\{subKeyPath}: {ex.Message}");
            }
        }

        string subKeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";

        RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

        FixRegistryKey(hkcu, subKeyPath);

        FixRegistryKey(hklm, subKeyPath);
    }

    /// <summary>
    /// Checks for DirectDraw compatibility issues and prompts the user to fix them.
    /// </summary>
    /// <param name="windowManager">The WindowManager for displaying message boxes.</param>
    public static void CheckAndPromptFix(WindowManager windowManager)
    {
        // Fix environment variable __COMPAT_LAYER first, for the client itself.
        string compatLayerEnv = Environment.GetEnvironmentVariable("__COMPAT_LAYER") ?? string.Empty;
        string fixedCompatLayerEnv = FixCompatLayerString(compatLayerEnv);
        if (compatLayerEnv != fixedCompatLayerEnv)
        {
            Logger.Log("Fixing __COMPAT_LAYER environment variable. Previous value: " +
                       $"'{compatLayerEnv}', new value: '{fixedCompatLayerEnv}'");
            Environment.SetEnvironmentVariable("__COMPAT_LAYER", fixedCompatLayerEnv);
        }

        // Now check registry compatibility settings for all relevant executables.
        try
        {
            Examine(out bool requireFix, out bool requireAdmin, out var problematicExeNames);

            if (!requireFix)
                return;

            Logger.Log("DirectDraw compatibility issue detected.");

            string localizedMessage = "Problematic Windows compatibility mode settings have been detected that may interfere with the game."
                .L10N("Client:Main:ProblematicCompatibilityText1") + "\n\n"
                + "Affected executables:".L10N("Client:Main:ProblematicCompatibilityText2")
                + "\n- " + string.Join("\n- ", problematicExeNames) + "\n\n" +
                "Would you like to remove these compatibility settings now?".L10N("Client:Main:ProblematicCompatibilityText3");

            if (requireAdmin && !AdminRestarter.IsRunningAsAdministrator())
            {
                localizedMessage += "\n\n" + ("Note: Administrator privileges are required to remove compatibility settings." + " " +
                    "Clicking Yes will relaunch the client with administrator permissions.").L10N("Client:Main:ProblematicCompatibilityText4");
            }

            var messageBox = XNAMessageBox.ShowYesNoDialog(windowManager,
                "Problematic Compatibility Settings Detected".L10N("Client:Main:ProblematicCompatibilityTitle"),
                localizedMessage);

            messageBox.YesClickedAction = _ =>
            {
                if (requireAdmin && !AdminRestarter.IsRunningAsAdministrator())
                {
                    Logger.Log("Administrator privileges required. Restart with elevated privileges.");

                    if (AdminRestarter.RestartAsAdmin())
                        Environment.Exit(0);
                }
                else
                {
                    Logger.Log("Attempting to fix DirectDraw compatibility settings.");
                    Fix();
                    Logger.Log("DirectDraw compatibility settings fixed successfully.");

                    XNAMessageBox.Show(windowManager,
                        "Fix Applied".L10N("Client:Main:CompatibilityFixAppliedTitle"),
                        "Compatibility settings have been removed successfully.".L10N("Client:Main:CompatibilityFixAppliedText"));
                }
            };

            messageBox.NoClickedAction = _ =>
            {
                Logger.Log("User declined to fix DirectDraw compatibility settings.");
            };
        }
        catch (Exception ex)
        {
            Logger.Log("Error checking DirectDraw compatibility: " + ex.ToString());
        }
    }
}
