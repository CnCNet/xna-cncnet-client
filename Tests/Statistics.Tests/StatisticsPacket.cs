#nullable enable
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

using ClientCore.Statistics;

namespace Statistics.Tests;

internal sealed class StatisticsPacket : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "cncnet-stats-" + Guid.NewGuid().ToString("N"));

    public StatisticsPacket()
    {
        Directory.CreateDirectory(directory);
    }

    public string DirectoryPath => directory;

    public void Write(params (string Id, byte[] Data)[] fields) => WriteBytes(Create(fields));

    public void WriteBytes(byte[] data) => File.WriteAllBytes(Path.Combine(directory, "stats.dmp"), data);

    public static byte[] Create(params (string Id, byte[] Data)[] fields)
    {
        using var stream = new MemoryStream();
        stream.Write(new byte[4]);

        foreach ((string id, byte[] data) in fields)
        {
            stream.Write(Encoding.ASCII.GetBytes(id));
            byte[] header = new byte[4];
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0, 2), 20);
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(2, 2), checked((ushort)data.Length));
            stream.Write(header);
            stream.Write(data);

            while (stream.Position % 4 != 0)
                stream.WriteByte(0);
        }

        byte[] packet = stream.ToArray();
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(0, 2), checked((ushort)packet.Length));

        return packet;
    }

    public static byte[] Text(string value) => Encoding.ASCII.GetBytes(value + '\0');

    public static byte[] Number(uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);

        return bytes;
    }

    public static byte[] Counts(params uint[] values)
    {
        byte[] bytes = new byte[values.Length * 4];
        for (int i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(i * 4, 4), values[i]);

        return bytes;
    }

    public static MatchStatistics Match(params string[] names)
    {
        var statistics = new MatchStatistics();
        foreach (string name in names)
            statistics.AddPlayer(name, false, false, false, 0, 0, 0, 0);

        return statistics;
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);
}