#nullable enable
using System;
using System.Collections.Generic;
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

    private static IEnumerable<string> GetExecutablesToCheck()
    {
        string[] configExecutables = ClientConfiguration.Instance.GetCompatibilityCheckExecutables();

        // clientdx.exe, clientogl.exe, or clientxna.exe
        string currentExeName = SafePath.GetFile(ProgramConstants.StartupExecutable).Name;

        // config list plus the current executable
        return configExecutables.Append(currentExeName);
    }

    private static void Examine(out bool requireFix, out bool requireAdmin)
    {
        using RegistryKey? hkcuKey = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers");
        using RegistryKey? hklmKey = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers");

        static bool IsFixRequired(object? regValue)
            => regValue is string regValueString
               && regValueString.Split([' ']).Intersect(OSCompatibilityValues).Any();

        bool anyHkcuRequireFix = false;
        bool anyHklmRequireFix = false;

        foreach (string executableName in GetExecutablesToCheck())
        {
            string exeFullPath = SafePath.CombineFilePath(ProgramConstants.GamePath, executableName);

            object? hkcuValue = hkcuKey?.GetValue(exeFullPath);
            object? hklmValue = hklmKey?.GetValue(exeFullPath);

            if (IsFixRequired(hkcuValue))
                anyHkcuRequireFix = true;

            if (IsFixRequired(hklmValue))
                anyHklmRequireFix = true;
        }

        requireFix = anyHkcuRequireFix || anyHklmRequireFix;
        requireAdmin = anyHklmRequireFix;
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

                foreach (string executableName in GetExecutablesToCheck())
                {
                    string exeFullPath = SafePath.CombineFilePath(ProgramConstants.GamePath, executableName);
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

        FixRegistryKey(Registry.CurrentUser, subKeyPath);

        FixRegistryKey(Registry.LocalMachine, subKeyPath);
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
            Examine(out bool requireFix, out bool requireAdmin);

            if (!requireFix)
                return;

            Logger.Log("DirectDraw compatibility issue detected.");

            string message = "Problematic Windows compatibility mode settings have been detected that may interfere with the game.\n\n" +
                            "Would you like to remove these compatibility settings now?";

            if (requireAdmin)
            {
                message += "\n\nNote: Administrator privileges are required to remove compatibility settings.";
            }

            var messageBox = XNAMessageBox.ShowYesNoDialog(windowManager,
                "Compatibility Settings Detected",
                message);

            messageBox.YesClickedAction = _ =>
            {
                if (requireAdmin && !AdminRestarter.IsRunningAsAdministrator())
                {
                    Logger.Log("Administrator privileges required. Prompting to restart with elevated privileges.");

                    var adminMessageBox = XNAMessageBox.ShowYesNoDialog(windowManager,
                        "Administrator Required",
                        "Administrator privileges are required to fix compatibility settings.\n\n" +
                        "Would you like to restart the application as administrator?");

                    adminMessageBox.YesClickedAction = _ =>
                    {
                        if (AdminRestarter.RestartAsAdmin())
                            Environment.Exit(0);
                    };

                    adminMessageBox.NoClickedAction = _ =>
                    {
                        Logger.Log("User declined to restart with admin privileges.");
                    };
                }
                else
                {
                    Logger.Log("Attempting to fix DirectDraw compatibility settings.");
                    Fix();
                    Logger.Log("DirectDraw compatibility settings fixed successfully.");

                    XNAMessageBox.Show(windowManager,
                        "Fix Applied",
                        "Compatibility settings have been removed successfully.");
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
