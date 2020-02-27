using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain
{
    /// <summary>
    /// A util on setting Windows Firewall exception to avoid the anoying firewall pop-up at the first time.
    /// Only for Windows Vista or later.
    /// </summary>
    public static class WindowsFirewallSettings
    {

        /// <summary>
        /// Add Windows Firewall exception for the specified program.
        /// Administrator privilege is required.
        /// </summary>
        /// <param name="programPath">The path of the program</param>
        public static void AddItem(string programPath)
        {
            // As what Microsoft said, one should use `netsh.exe` to set up the Windows firewall,
            // and editing the corresponding registry is not recommended.
            // See: https://social.technet.microsoft.com/Forums/windows/en-US/7df33d04-8b35-4527-8579-b68feed60a75/adding-windows-firewall-setting-to-the-registry-why-does-it-not-appear

            // try to remove existing item
            RemoveItem(programPath);

            var programPathHash32 = GetShortHash(programPath);
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "netsh.exe",
                    Arguments = "advfirewall firewall add rule name=\"DTA-Client-Exception-" + programPathHash32 + "\" dir=in action=allow program=\"" + programPath + "\"",
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// Detect whether Windows Firewall exception for the specified program exists added by AddItem() method.
        /// Administrator privilege is NOT required.
        /// </summary>
        /// <param name="programPath">The path of the program</param>
        public static bool ItemExists(string programPath)
        {
            var programPathHash32 = GetShortHash(programPath);

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "netsh.exe",
                    Arguments = "advfirewall firewall show rule name=\"DTA-Client-Exception-" + programPathHash32 + "\" dir=in",
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            process.Start();
            process.WaitForExit(500);

            // Exit Code 0 means at least one item exists.
            return process.ExitCode == 0;
        }

        /// <summary> 
        /// Remove Windows Firewall exception for the specified program added by AddItem() method.
        /// Administrator privilege is required.
        /// </summary>
        /// <param name="programPath">The path of the program</param>
        public static void RemoveItem(string programPath)
        {
            var programPathHash32 = GetShortHash(programPath);

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "netsh.exe",
                    Arguments = "advfirewall firewall delete rule name=\"DTA-Client-Exception-" + programPathHash32 + "\" dir=in",
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            process.Start();
            process.WaitForExit(500);
        }

        private static string GetShortHash(string programPath)
        {
            StringBuilder programPathHash32 = new StringBuilder();
            {
                byte[] bytes = Encoding.UTF8.GetBytes(programPath);
                byte[] hash = System.Security.Cryptography.SHA256.Create().ComputeHash(bytes);

                for (int i = 0; i < Math.Min(hash.Length, 8); i++)
                {
                    programPathHash32.Append(hash[i].ToString("X2"));
                }
            }
            return programPathHash32.ToString();
        }


    }
}
