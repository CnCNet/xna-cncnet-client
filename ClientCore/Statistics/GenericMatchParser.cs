namespace ClientCore.Statistics
{
    public abstract class GenericMatchParser
    {
        protected MatchStatistics Statistics {get; set;}

        protected GenericMatchParser(MatchStatistics ms)
        {
            Statistics = ms;
        }
    }
}