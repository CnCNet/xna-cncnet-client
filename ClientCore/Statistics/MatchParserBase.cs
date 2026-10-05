namespace ClientCore.Statistics;

public abstract class MatchParserBase
{
    public MatchStatistics Statistics {get; set;}

    public MatchParserBase(MatchStatistics ms)
    {
        Statistics = ms;
    }

    protected abstract void ParseStatistics(string gamepath);
}
