#nullable enable
using System;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Generic
{
    public static class URLHandler
    {
        /// <summary>
        /// Checks whether a URL is safe before opening it, prompting a warning as an XNAMessageBox otherwise.
        /// </summary>
        public static void OpenLink(WindowManager wm, string url)
        {
            // Determine if the links is trusted
            bool isTrusted = false;
            try
            {
                string domain = new Uri(url).Host;
                var trustedDomains = ClientConfiguration.Instance.TrustedDomains.Concat(ClientConfiguration.Instance.AlwaysTrustedDomains);
                isTrusted = trustedDomains.Contains(domain, StringComparer.InvariantCultureIgnoreCase)
                    || trustedDomains.Any(trustedDomain => domain.EndsWith("." + trustedDomain, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception ex)
            {
                isTrusted = false;
                Logger.Log($"Error in parsing the URL \"{url}\": {ex.ToString()}");
            }

            if (isTrusted)
            {
                ProcessLauncher.StartShellProcess(url);
                return;
            }

            // Show the warning if the links is not trusted
            var msgBox = new XNAMessageBox(wm,
                "Open Link Confirmation".L10N("Client:Main:OpenLinkConfirmationTitle"),
                """
                    You're about to open a link shared in chat.

                    Please note that this link hasn't been verified,
                    and CnCNet is not responsible for its content.

                    Would you like to open the following link in your browser?
                    """.L10N("Client:Main:OpenLinkConfirmationText")
                + Environment.NewLine + Environment.NewLine + url,
                XNAMessageBoxButtons.YesNo);
            msgBox.YesClickedAction = (msgBox) => ProcessLauncher.StartShellProcess(url);
            msgBox.Show();
        }
    }
}
