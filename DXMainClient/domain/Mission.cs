namespace DTAClient.Domain
{
    /// <summary>
    /// A Tiberian Sun mission listed in Battle(E).ini.
    /// </summary>
    public class Mission
    {
        public Mission(int cd, int side, string scenario, string guiName,
            string guiDescription, string finalMovie, bool requiredAddon)
        {
            CD = cd;
            Side = side;
            Scenario = scenario;
            GUIName = guiName;
            GUIDescription = guiDescription;
            FinalMovie = finalMovie;
            RequiredAddon = requiredAddon;
        }

        public int CD { get; private set; }
        public int Side { get; private set; }
        public string Scenario { get; private set; }
        public string GUIName { get; private set; }
        public string GUIDescription { get; private set; }
        public string FinalMovie { get; private set; }
        public bool RequiredAddon { get; private set; }
    }
}
