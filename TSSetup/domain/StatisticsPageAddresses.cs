using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using ClientCore;

namespace dtasetup.domain
{
    class StatisticsPageAddresses
    {
        /// <summary>
        /// Visits the CnCNet statistics page of the current game.
        /// </summary>
        public static string GetCnCNetStatsPageAddress()
        {
            if (MCDomainController.Instance().GetShortGameName() == "DTA")
            {
                return "http://dta.ppmsite.com/ga-dta-cncnet.htm";
            }
            else if (MCDomainController.Instance().GetShortGameName() == "TI")
            {
                return "http://dta.ppmsite.com/ga-ti-cncnet.htm";
            }
            else if (MCDomainController.Instance().GetShortGameName() == "TS")
            {
                return "http://dta.ppmsite.com/ga-ts-cncnet.htm";
            }
            else if (MCDomainController.Instance().GetShortGameName() == "YR")
            {
                return "http://dta.ppmsite.com/ga-yr-cncnet.htm";
            }

            return String.Empty;
        }

        /// <summary>
        /// Visits the update statistics page of the current game.
        /// </summary>
        public static string GetUpdateStatsPageAddress()
        {
            if (MCDomainController.Instance().GetShortGameName() == "DTA")
            {
                return "http://dta.ppmsite.com/ga-dta-update.htm";
            }
            else if (MCDomainController.Instance().GetShortGameName() == "TI")
            {
                return "http://dta.ppmsite.com/ga-ti-update.htm";
            }
            else if (MCDomainController.Instance().GetShortGameName() == "TS")
            {
                return "http://dta.ppmsite.com/ga-ts-update.htm";
            }
            else if (MCDomainController.Instance().GetShortGameName() == "YR")
            {
                return "http://dta.ppmsite.com/ga-yr-update.htm";
            }

            return String.Empty;
        }
    }
}
