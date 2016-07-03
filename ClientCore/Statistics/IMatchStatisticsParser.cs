using System;

namespace ClientCore.Statistics
{
    internal interface IMatchStatisticsParser
    {
        void ParseStatistics(String gamepath);
    }
}
