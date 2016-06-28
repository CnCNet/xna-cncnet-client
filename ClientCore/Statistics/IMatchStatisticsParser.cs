using System;
using System.Collections.Generic;
using System.Text;

namespace ClientCore.Statistics
{
    internal interface IMatchStatisticsParser
    {
        void ParseStatistics(String gamepath);
    }
}
