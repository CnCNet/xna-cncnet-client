using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using ClientCore;

namespace DTAClient.domain
{
    public static class StatisticsPageAddresses
    {
        /// <summary>
        /// Visits the CnCNet statistics page of the current game.
        /// </summary>
        public static string GetCnCNetStatsPageAddress()
        {
            switch (MCDomainController.Instance.GetShortGameName())
            {
                case "DTA":
                    return "http://dta.ppmsite.com/ga-dta-cncnet.htm";
                case "TI":
                    return "http://dta.ppmsite.com/ga-ti-cncnet.htm";
                case "TS":
                    return "http://dta.ppmsite.com/ga-ts-cncnet.htm";
                case "YR":
                    return "http://dta.ppmsite.com/ga-yr-cncnet.htm";
                default:
                    return String.Empty;
            }
        }

        /// <summary>
        /// Visits the update statistics page of the current game.
        /// </summary>
        public static string GetUpdateStatsPageAddress()
        {
            switch (MCDomainController.Instance.GetShortGameName())
            {
                case "DTA":
                    return "http://dta.ppmsite.com/ga-dta-update.htm";
                case "TI":
                    return "http://dta.ppmsite.com/ga-ti-update.htm";
                case "TS":
                    return "http://dta.ppmsite.com/ga-ts-update.htm";
                case "YR":
                    return "http://dta.ppmsite.com/ga-yr-update.htm";
                default:
                    return String.Empty;
            }
        }
    }
}
