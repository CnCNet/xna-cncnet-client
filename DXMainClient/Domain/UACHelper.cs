using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DTAClient.Domain
{

    public static class UACHelper
    {
        /// <summary>
        /// Detect whether the current process runs as Administrator.
        /// </summary>
        /// <returns>Whether the current process runs as Administrator.</returns>
        public static bool IsElevated()
        {
            //https://stackoverflow.com/questions/1220213/detect-if-running-as-administrator-with-or-without-elevated-privileges
            return WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
        } 

        public static void RequireElevated(string reasonText, bool canSkip = true)
        {
            if (!IsElevated())
            {
                if (MessageBox.Show(
                    reasonText + Environment.NewLine + Environment.NewLine +
                     "Would you like to restart the client with elevated privileges?" + Environment.NewLine + Environment.NewLine +
                     (canSkip ? "Click \"No\" to skip this step." : "Click \"No\" to exit the client.")
                , "Elevated privileges required", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                {
                    if (!canSkip)
                    {
                        Environment.Exit(0);
                    }
                    return;
                }
                RestartAsElevated();
            }
        }

        /// <summary>
        /// Restart the client with elevated privileges.
        /// </summary>
        public static void RestartAsElevated()
        {

            ProcessStartInfo psInfo = new ProcessStartInfo();
            psInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
            psInfo.Verb = "runas";
            Process.Start(psInfo);
            Environment.Exit(0);
        }
    }
}
