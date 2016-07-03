using System;
using System.Collections.Generic;
using System.IO;

namespace ClientCore.Statistics
{
    public abstract class GenericStatisticsManager
    {
        protected List<MatchStatistics> Statistics = new List<MatchStatistics>();

        protected static string GetStatDatabaseVersion(string scorePath)
        {
            if (!File.Exists(scorePath))
            {
                return null;
            }

            using (StreamReader reader = new StreamReader(scorePath))
            {
                char[] versionBuffer = new char[4];
                reader.Read(versionBuffer, 0, versionBuffer.Length);

                String s = new String(versionBuffer);
                return s;
            }
        }

        public abstract void ReadStatistics(string gamePath);

        public int GetMatchCount() { return Statistics.Count; }

        public MatchStatistics GetMatchByIndex(int index)
        {
            return Statistics[index];
        }
    }
}
