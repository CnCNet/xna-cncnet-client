using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;
using Rampastring.Tools;

namespace ClientCore
{
    /// <summary>
    /// Utility for restarting the client with administrator privileges.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class AdminRestarter
    {
        /// <summary>
        /// Checks if the application is running with administrator privileges.
        /// </summary>
        /// <returns>True if running as administrator, false otherwise.</returns>
        public static bool IsRunningAsAdministrator()
        {
            try
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Restarts the current application with administrator privileges.
        /// </summary>
        /// <returns>True if the restart was initiated successfully, false otherwise.</returns>
        public static bool RestartAsAdmin()
        {
            try
            {
                using var _ = Process.Start(new ProcessStartInfo
                {
                    FileName = SafePath.CombineFilePath(ProgramConstants.StartupExecutable),
                    Verb = "runas",
                    UseShellExecute = true,
                });

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to restart with admin privileges: " + ex.ToString());
                return false;
            }
        }
    }
}
