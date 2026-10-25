#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
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
