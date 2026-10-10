#nullable enable

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace DTAClient.Domain
{
    internal static class OpenTSSavedGameReader
    {
        private const int HeaderSize = 32;
        private const int FieldHeaderSize = 8;
        private const uint MaxTableLength = 1024 * 1024;
        private const uint MaxFieldLength = 64 * 1024;
        private const uint MaxContentLength = 256 * 1024 * 1024;
        private static readonly UTF8Encoding DescriptionEncoding = new UTF8Encoding(false, true);

        internal static bool HasSignature(Stream file)
        {
            long position = file.Position;

            try
            {
                return file.ReadByte() == 'O' && file.ReadByte() == 'T'
                    && file.ReadByte() == 'S' && file.ReadByte() == 'V';
            }
            finally
            {
                file.Position = position;
            }
        }

        internal static SavedGameMetadata ReadInfo(Stream file)
        {
            byte[] header = new byte[HeaderSize];
            ReadBytes(file, header);

            if (header[0] != 'O' || header[1] != 'T' || header[2] != 'S' || header[3] != 'V')
                throw new InvalidDataException("Invalid OpenTS saved game signature.");

            ushort version = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(4, 2));
            ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(6, 2));

            if (version != 1 || (flags & ~1) != 0)
                throw new InvalidDataException("Unsupported OpenTS saved game format version or flags.");

            uint tableLength = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(8, 4));
            uint contentOffset = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(12, 4));
            uint storedLength = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(16, 4));
            uint contentLength = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(20, 4));
            uint headerChecksum = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(28, 4));

            if (tableLength > MaxTableLength || contentOffset != HeaderSize + tableLength ||
                storedLength > MaxContentLength || contentLength > MaxContentLength)
            {
                throw new InvalidDataException("Invalid OpenTS saved game header lengths.");
            }

            if (tableLength > file.Length - file.Position)
                throw new EndOfStreamException("Unexpected end of OpenTS saved game field table.");

            byte[] table = new byte[(int)tableLength];
            ReadBytes(file, table);

            // OpenTS checks the listing independently of the compressed game state.
            uint checksum = ComputeChecksum(header.AsSpan(0, HeaderSize - 4), 0);
            checksum = ComputeChecksum(table, checksum);

            if (checksum != headerChecksum)
                throw new InvalidDataException("Invalid OpenTS saved game metadata checksum.");

            string? description = null;
            int offset = 0;

            while (offset < table.Length)
            {
                if (table.Length - offset < FieldHeaderSize)
                    throw new InvalidDataException("Incomplete OpenTS saved game field header.");

                ushort id = BinaryPrimitives.ReadUInt16LittleEndian(table.AsSpan(offset, 2));
                ushort kind = BinaryPrimitives.ReadUInt16LittleEndian(table.AsSpan(offset + 2, 2));
                uint length = BinaryPrimitives.ReadUInt32LittleEndian(table.AsSpan(offset + 4, 4));
                offset += FieldHeaderSize;

                if (length > MaxFieldLength || length > table.Length - offset)
                    throw new InvalidDataException("Invalid OpenTS saved game field length.");

                if (id == 2 && kind == 1 && description == null)
                    description = DescriptionEncoding.GetString(table, offset, (int)length);

                offset += (int)length;
            }

            return new SavedGameMetadata(description ?? throw new InvalidDataException("OpenTS saved game description is missing."), 0);
        }

        private static void ReadBytes(Stream file, byte[] bytes)
        {
            int offset = 0;

            while (offset < bytes.Length)
            {
                int count = file.Read(bytes, offset, bytes.Length - offset);

                if (count == 0)
                    throw new EndOfStreamException("Unexpected end of OpenTS saved game metadata.");

                offset += count;
            }
        }

        private static uint ComputeChecksum(ReadOnlySpan<byte> bytes, uint seed)
        {
            uint checksum = seed ^ uint.MaxValue;

            foreach (byte value in bytes)
            {
                checksum ^= value;

                for (int bit = 0; bit < 8; bit++)
                    checksum = (checksum >> 1) ^ ((checksum & 1) == 0 ? 0 : 0xEDB88320u);
            }

            return checksum ^ uint.MaxValue;
        }
    }
}
