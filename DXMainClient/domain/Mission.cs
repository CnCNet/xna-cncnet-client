using System;
using System.Collections.Generic;
using System.Text;

namespace dtasetup.domain
{
    /// <summary>
    /// A Tiberian Sun mission listed in Battle(E).ini.
    /// </summary>
    public class Mission
    {
        public Mission() { }

        public Mission(int cd, int side, string scenario, string guiName, string guiDescription, string finalMovie, bool requiredAddon)
        {
            CD = cd;
            Side = side;
            Scenario = scenario;
            GUIName = guiName;
            GUIDescription = guiDescription;
            FinalMovie = finalMovie;
            RequiredAddon = requiredAddon;
        }

        public int CD { get; set; }
        public int Side { get; set; }
        public string Scenario { get; set; }
        public string GUIName { get; set; }
        public string GUIDescription { get; set; }
        public string FinalMovie { get; set; }
        public bool RequiredAddon { get; set; }
    }
}
