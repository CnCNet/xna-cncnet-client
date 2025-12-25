#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

using ClientCore;
using ClientCore.Extensions;

using Microsoft.Win32;

using Rampastring.Tools;

namespace DTAClient.Domain;

[SupportedOSPlatform("windows")]
public static class DirectDrawCompatibilityFixer
{
    private static readonly IReadOnlyList<string> OSCompatibilityValues = [
        "WIN8RTM", "WIN7RTM", "VISTASP2", "VISTASP1", "VISTARTM", "WINXPSP3", "WINXPSP2", "WIN98", "WIN95"
    ];

    private static readonly IReadOnlyList<string> StaticExecutablesToCheck = [
        "CnCNetYRLauncher.exe",
        "gamemd.exe",
        "gamemd-spawn.exe"
    ];

    private static IEnumerable<string> GetExecutablesToCheck()
    {
        // clientdx.exe, clientogl.exe, or clientxna.exe
        string currentExeName = SafePath.GetFile(ProgramConstants.StartupExecutable).Name;

        // static list plus the current executable
        return StaticExecutablesToCheck.Append(currentExeName);
    }

    public static void Examine(out bool requireFix, out bool requireAdmin)
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

    public static void Fix()
    {
        void FixValue(object? regValue, out bool success, out string newRegValue)
        {
            if (regValue is string regValueString)
            {
                newRegValue = string.Join(" ",
                    regValueString
                        .SplitWithCleanup(new [] {' '})
                        .Where(v => !OSCompatibilityValues.Contains(v)));
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

                    FixValue(value, out bool success, out string newValue);

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
}
