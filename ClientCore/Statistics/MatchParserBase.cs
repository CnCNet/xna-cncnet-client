#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using Rampastring.Tools;

namespace ClientCore.Statistics
{
    public abstract class MatchParserBase
    {
        protected const int MaxPlayerCount = 8;

        private const string StatisticsFileName = "stats.dmp";
        private const int PacketHeaderLength = 4;
        private const int FieldHeaderLength = 8;

        private readonly Dictionary<string, List<KeyValuePair<string, string>>> typeRegistries =
            new Dictionary<string, List<KeyValuePair<string, string>>>(StringComparer.OrdinalIgnoreCase);

        protected bool isLoadedGame;

        public MatchStatistics Statistics { get; set; }

        public MatchParserBase(MatchStatistics ms, bool isLoadedGame)
        {
            Statistics = ms;
            this.isLoadedGame = isLoadedGame;
        }

        protected abstract void ParseStatistics(string gamepath);

        public virtual void ParseStats(string gamepath, string fileName) => ParseStatistics(gamepath);

        /// <summary>
        /// Reads the Westwood packet format shared by games that write stats.dmp.
        /// Game-specific fields are applied only after the complete packet has been read.
        /// </summary>
        protected void ParseStatisticsDump(string gamePath, Action resetResults, Action<string, byte[]> readRecord, Action applyResults)
        {
            FileInfo statisticsFile = SafePath.GetFile(gamePath, StatisticsFileName);
            if (!statisticsFile.Exists)
            {
                Logger.Log($"{GetType().Name}: Failed to read statistics: {StatisticsFileName} does not exist.");

                return;
            }

            try
            {
                List<(string Id, byte[] Data)> records = ReadRecords(statisticsFile.FullName);
                LoadTypeRegistries(gamePath);
                resetResults();

                foreach (var record in records)
                    readRecord(record.Id, record.Data);

                applyResults();
            }
            catch (Exception ex)
            {
                Logger.Log($"{GetType().Name}: Error parsing {StatisticsFileName}: " + ex);
            }
        }

        private void LoadTypeRegistries(string gamePath)
        {
            typeRegistries.Clear();

            try
            {
                string? rulesPath = FindFile(SafePath.CombineFilePath(gamePath, "rules.ini"));
                rulesPath ??= FindFile(SafePath.CombineFilePath(gamePath, "INI", "rules.ini"));

                if (rulesPath == null)
                    Logger.Log($"{GetType().Name}: No rules.ini found; using indexed statistics counters.");
                else
                    MergeTypeRegistries(new IniFile(rulesPath));
            }
            catch (Exception ex)
            {
                Logger.Log($"{GetType().Name}: Failed to read rules type lists; using indexed statistics counters: " + ex);
            }

            string scenario = "spawnmap.ini";
            try
            {
                string? spawnPath = FindFile(SafePath.CombineFilePath(gamePath, "spawn.ini"));
                if (spawnPath != null)
                {
                    string configuredScenario = new IniFile(spawnPath).GetStringValue("Settings", "Scenario", scenario);
                    if (!string.IsNullOrWhiteSpace(configuredScenario))
                        scenario = configuredScenario;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"{GetType().Name}: Failed to read the statistics scenario path; trying spawnmap.ini: " + ex);
            }

            try
            {
                string? scenarioPath = FindFile(SafePath.CombineFilePath(gamePath, scenario));
                if (scenarioPath != null)
                    MergeTypeRegistries(new IniFile(scenarioPath));
            }
            catch (Exception ex)
            {
                Logger.Log($"{GetType().Name}: Failed to read scenario type lists; retaining rules statistics counters: " + ex);
            }
        }

        private void MergeTypeRegistries(IniFile rules)
        {
            foreach (string sectionName in rules.GetSections())
            {
                if (!sectionName.EndsWith("Types", StringComparison.OrdinalIgnoreCase))
                    continue;

                IniSection section = rules.GetSection(sectionName);
                if (!typeRegistries.TryGetValue(sectionName, out List<KeyValuePair<string, string>>? entries))
                {
                    entries = new List<KeyValuePair<string, string>>();
                    typeRegistries.Add(sectionName, entries);
                }

                foreach (KeyValuePair<string, string> entry in section.Keys)
                {
                    int existingIndex = entries.FindIndex(candidate =>
                        string.Equals(candidate.Key, entry.Key, StringComparison.OrdinalIgnoreCase));
                    if (existingIndex < 0)
                        entries.Add(entry);
                    else
                        entries[existingIndex] = entry;
                }
            }
        }

        private static string? FindFile(string filePath)
        {
            if (File.Exists(filePath))
                return filePath;

            string? directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory))
                return null;

            if (!Directory.Exists(directory))
            {
                string? parent = Path.GetDirectoryName(directory);
                if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
                    return null;

                string directoryName = Path.GetFileName(directory);
                foreach (string candidate in Directory.EnumerateDirectories(parent))
                {
                    if (string.Equals(Path.GetFileName(candidate), directoryName, StringComparison.OrdinalIgnoreCase))
                    {
                        directory = candidate;
                        break;
                    }
                }

                if (!Directory.Exists(directory))
                    return null;
            }

            string fileName = Path.GetFileName(filePath);
            foreach (string candidate in Directory.EnumerateFiles(directory))
            {
                if (string.Equals(Path.GetFileName(candidate), fileName, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Names counter slots from an ordered rules type list without losing slots
        /// that have no unique type identifier in the loaded rules.
        /// </summary>
        protected Dictionary<string, uint> ReadTypeCounters(byte[] data, params string[] typeSections)
        {
            if (data.Length % sizeof(uint) != 0)
                throw new InvalidDataException("Invalid statistics counter array length.");

            List<KeyValuePair<string, string>>? entries = null;
            foreach (string sectionName in typeSections)
            {
                if (typeRegistries.TryGetValue(sectionName, out entries))
                    break;
            }

            var typeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (entries != null)
            {
                foreach (KeyValuePair<string, string> entry in entries)
                {
                    string typeId = entry.Value;
                    if (string.IsNullOrWhiteSpace(typeId))
                        continue;

                    typeCounts.TryGetValue(typeId, out int count);
                    typeCounts[typeId] = count + 1;
                }
            }

            var counters = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < data.Length / sizeof(uint); i++)
            {
                string? typeId = entries != null && i < entries.Count ? entries[i].Value : null;
                string key = !string.IsNullOrWhiteSpace(typeId) && typeCounts[typeId!] == 1
                    ? "type:" + typeId
                    : "index:" + i.ToString(CultureInfo.InvariantCulture);
                counters.Add(key, BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(i * sizeof(uint), sizeof(uint))));
            }

            return counters;
        }

        private static List<(string Id, byte[] Data)> ReadRecords(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

            byte[] packetHeader = ReadBytes(reader, PacketHeaderLength);
            int declaredLength = BinaryPrimitives.ReadUInt16BigEndian(packetHeader.AsSpan(0, sizeof(ushort)));
            if (declaredLength < PacketHeaderLength || declaredLength > stream.Length)
                throw new InvalidDataException("stats.dmp declares an invalid packet length.");

            var records = new List<(string Id, byte[] Data)>();

            while (stream.Position < declaredLength)
            {
                if (declaredLength - stream.Position < FieldHeaderLength)
                    throw new InvalidDataException("stats.dmp ends inside a field header.");

                byte[] fieldHeader = ReadBytes(reader, FieldHeaderLength);
                string id = Encoding.ASCII.GetString(fieldHeader, 0, 4);
                int length = BinaryPrimitives.ReadUInt16BigEndian(fieldHeader.AsSpan(6, sizeof(ushort)));
                int padding = (4 - length % 4) % 4;
                if (length + padding > declaredLength - stream.Position)
                    throw new InvalidDataException($"stats.dmp field {id} exceeds the packet length.");

                records.Add((id, ReadBytes(reader, length)));
                ReadBytes(reader, padding);
            }

            return records;
        }

        private static byte[] ReadBytes(BinaryReader reader, int length)
        {
            byte[] data = reader.ReadBytes(length);
            if (data.Length != length)
                throw new EndOfStreamException("stats.dmp ended before the packet was fully read.");

            return data;
        }

        protected static bool TryGetHouseIndex(string id, string prefix, out int houseIndex, int firstHouseNumber = 0)
        {
            houseIndex = -1;
            if (id.Length != 4 || prefix.Length != 3
                || !id.StartsWith(prefix, StringComparison.Ordinal)
                || id[3] < '0' || id[3] > '9')
            {
                return false;
            }

            houseIndex = id[3] - '0' - firstHouseNumber;

            return houseIndex >= 0 && houseIndex < MaxPlayerCount;
        }

        protected static string ReadString(byte[] data)
        {
            int length = Array.IndexOf(data, (byte)0);

            return Encoding.ASCII.GetString(data, 0, length < 0 ? data.Length : length);
        }

        protected static bool TryReadUInt32(byte[] data, out uint value)
        {
            value = 0;
            if (data.Length < sizeof(uint))
                return false;

            value = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0, sizeof(uint)));

            return true;
        }
    }
}