namespace ClientCore.Statistics
{
    public abstract class GenericMatchParser
    {
        public MatchStatistics Statistics {get; set;}

        public GenericMatchParser(MatchStatistics ms)
        {
            Statistics = ms;
        }

        protected abstract void ParseStatistics(string gamepath);
    }
}
