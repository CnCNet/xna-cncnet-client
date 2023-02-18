using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ClientCore.Statistics
{
    public abstract class GenericStatisticsManager
    {
        protected List<MatchStatistics> Statistics = new List<MatchStatistics>();

        protected static async ValueTask<string> GetStatDatabaseVersionAsync(string scorePath)
        {
            if (!File.Exists(scorePath))
            {
                return null;
            }

            using var reader = new StreamReader(scorePath);
            char[] versionBuffer = new char[4];
            await reader.ReadAsync(versionBuffer, 0, versionBuffer.Length).ConfigureAwait(false);

            return new string(versionBuffer);
        }

        public int GetMatchCount() { return Statistics.Count; }

        public MatchStatistics GetMatchByIndex(int index)
        {
            return Statistics[index];
        }
    }
}