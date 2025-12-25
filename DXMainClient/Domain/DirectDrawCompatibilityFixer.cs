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
    private static IReadOnlyList<string> OSCompatibilityValues = [
        "WIN8RTM", "WIN7RTM", "VISTASP2", "VISTASP1", "VISTARTM", "WINXPSP3", "WINXPSP2", "WIN98", "WIN95"
    ];
    
    public static void Examine(out bool requireFix, out bool requireAdmin)
    {
        using RegistryKey hkcuKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers");
        using RegistryKey hklmKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers");
            
        string gameExeFullPath = SafePath.CombineFilePath(ProgramConstants.GamePath,
            ClientConfiguration.Instance.GetGameExecutableName());
            
        object hkcuValue = hkcuKey?.GetValue(gameExeFullPath);
        object hklmValue = hklmKey?.GetValue(gameExeFullPath);

        bool IsFixRequired(object regValue)
            => regValue is string regValueString 
               && regValueString.Split([' ']).Intersect(OSCompatibilityValues).Any();

        bool hkcuRequireFix = IsFixRequired(hkcuValue);
        bool hklmRequireFix = IsFixRequired(hklmValue);

        requireFix = hkcuRequireFix || hklmRequireFix;
        requireAdmin = hklmRequireFix;
    }

    public static void Fix()
    {
        using RegistryKey hkcuKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", writable: true);
        using RegistryKey hklmKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", writable: true);
            
        string gameExeFullPath = SafePath.CombineFilePath(ProgramConstants.GamePath,
            ClientConfiguration.Instance.GetGameExecutableName());
            
        object hkcuValue = hkcuKey?.GetValue(gameExeFullPath);
        object hklmValue = hklmKey?.GetValue(gameExeFullPath);

        void FixValue(object regValue, out bool success, out string newRegValue)
        {
            if (regValue is string regValueString)
            {
                newRegValue = string.Join(" ", 
                    regValueString
                        .SplitWithCleanup(new char[] {' '})
                        .Where(v => !OSCompatibilityValues.Contains(v)));
                success = true;
            }
            else
            {
                success = false;
                newRegValue = null;
            }
        }

        FixValue(hkcuValue, out bool hkcuFixSuccess, out string newHKCUValue);
        FixValue(hklmValue, out bool hklmFixSuccess, out string newHKLMValue);

        if (hkcuFixSuccess)
        {
            if (string.IsNullOrEmpty(newHKCUValue))
                hkcuKey.DeleteValue(gameExeFullPath, false);
            else
                hkcuKey.SetValue(gameExeFullPath, newHKCUValue, RegistryValueKind.String);
        }

        if (hklmFixSuccess)
        {
            if (string.IsNullOrEmpty(newHKLMValue))
                hklmKey.DeleteValue(gameExeFullPath, false);
            else
                hklmKey.SetValue(gameExeFullPath, newHKLMValue, RegistryValueKind.String);
        }
    }
}