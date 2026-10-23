#nullable enable
using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.Versioning;

namespace DTAClient.Domain.Telemetry;

/// <summary>
/// Processor, graphics card and memory information of the local machine.
/// </summary>
internal sealed class SystemSpecifications
{
    private SystemSpecifications()
    {
    }

    /// <summary>
    /// The processor names, each followed by its core count, or an error text.
    /// </summary>
    public string CpuDescription { get; private set; } = string.Empty;

    /// <summary>
    /// The name of the first processor, or null if it couldn't be read.
    /// </summary>
    public string? CpuName { get; private set; }

    public List<string> VideoControllers { get; } = new();

    /// <summary>
    /// The total installed physical memory in bytes, or 0 if it couldn't be read.
    /// </summary>
    public ulong TotalMemoryBytes { get; private set; }

    /// <summary>
    /// Queries the specifications through WMI. This takes lots of time.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static SystemSpecifications Collect()
    {
        var specifications = new SystemSpecifications();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");

            foreach (var proc in searcher.Get())
            {
                string name = proc["Name"]?.ToString()?.Trim() ?? string.Empty;
                specifications.CpuName ??= name;
                specifications.CpuDescription += name + " (" + proc["NumberOfCores"] + " cores) ";
            }
        }
        catch
        {
            specifications.CpuDescription = "CPU info not found";
        }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            foreach (ManagementObject mo in searcher.Get())
            {
                var currentBitsPerPixel = mo.Properties["CurrentBitsPerPixel"];
                var description = mo.Properties["Description"];
                string? descriptionText = description?.Value?.ToString()?.Trim();
                if (currentBitsPerPixel?.Value != null && !string.IsNullOrEmpty(descriptionText))
                    specifications.VideoControllers.Add(descriptionText!);
            }
        }
        catch
        {
        }

        try
        {
            using var searcher = new ManagementObjectSearcher("Select * From Win32_PhysicalMemory");

            foreach (ManagementObject ram in searcher.Get())
                specifications.TotalMemoryBytes += Convert.ToUInt64(ram.GetPropertyValue("Capacity"));
        }
        catch
        {
            specifications.TotalMemoryBytes = 0;
        }

        return specifications;
    }

    public override string ToString()
    {
        string videoControllers = VideoControllers.Count > 0
            ? "Video controller: " + string.Join(" Video controller: ", VideoControllers)
            : "Video controller info not found";

        string memory = TotalMemoryBytes == 0
            ? "Memory info not found"
            : "Total physical memory: " + (TotalMemoryBytes >= 1073741824 ? TotalMemoryBytes / 1073741824 + "GB" : TotalMemoryBytes / 1048576 + "MB");

        return string.Format("{0} | {1} | {2}", CpuDescription.Trim(), videoControllers, memory);
    }
}
