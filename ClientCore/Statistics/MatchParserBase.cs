namespace ClientCore.Statistics;

public abstract class MatchParserBase
{
    protected bool isLoadedGame;

    public MatchStatistics Statistics {get; set;}

    public MatchParserBase(MatchStatistics ms, bool isLoadedGame)
    {
        Statistics = ms;
        this.isLoadedGame = isLoadedGame;
    }

    protected abstract void ParseStatistics(string gamepath);
    public abstract void ParseStats(string gamepath, string fileName);
}
