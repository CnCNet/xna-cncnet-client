using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using lzo.net;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// Decodes IsoMapPack5 tile data from a TS/RA2 map file.
    /// Extracts the per-cell height (level) values needed for accurate map preview overlay placement.
    /// </summary>
    internal static class IsoMapPackDecoder
    {
        // 11 bytes per tile: x(int16) y(int16) tileIndex(int32) subTile(byte) level(byte) iceGrowth(byte)
        private const int TileRecordSize = 11;

        /// <summary>
        /// Packs isometric tile (x, y) coordinates into a single long key for fast dictionary lookup.
        /// Both values must fit in a ushort (0–65535), which holds for all practical TS/RA2 map sizes.
        /// </summary>
        public static long TileKey(int x, int y) => ((long)(ushort)x << 16) | (ushort)y;

        /// <summary>
        /// Decodes the concatenated base64 IsoMapPack5 string and returns a dictionary
        /// from packed tile key (see <see cref="TileKey"/>) to height level.
        /// Returns an empty dictionary if the input is empty or malformed.
        /// </summary>
        public static Dictionary<long, byte> Decode(string base64)
        {
            var empty = new Dictionary<long, byte>(0);
            if (string.IsNullOrEmpty(base64))
                return empty;

            byte[] compressed;
            try { compressed = Convert.FromBase64String(base64); }
            catch { return empty; }

            // First pass: sum all decompressed block sizes so we can allocate one output buffer.
            // Block header: [inputSize:uint16][outputSize:uint16] — same format as PreviewPack.
            int totalOutputSize = 0;
            for (int p = 0; p + 4 <= compressed.Length;)
            {
                int inSz  = BitConverter.ToUInt16(compressed, p);
                int outSz = BitConverter.ToUInt16(compressed, p + 2);
                if (inSz == 0 || outSz == 0) break;
                totalOutputSize += outSz;
                p += 4 + inSz;
            }

            if (totalOutputSize == 0)
                return empty;

            // Single allocation for all decompressed data.
            byte[] data = new byte[totalOutputSize];
            int writePos = 0;

            // Second pass: decompress each LZO1X block directly into the output buffer.
            for (int p = 0; p + 4 <= compressed.Length;)
            {
                int inSz  = BitConverter.ToUInt16(compressed, p);
                int outSz = BitConverter.ToUInt16(compressed, p + 2);
                if (inSz == 0 || outSz == 0) break;

                using var lzoStream = new LzoStream(
                    new MemoryStream(compressed, p + 4, inSz),
                    CompressionMode.Decompress);

                int read = 0;
                while (read < outSz)
                {
                    int n = lzoStream.Read(data, writePos + read, outSz - read);
                    if (n == 0) break;
                    read += n;
                }
                writePos += outSz;
                p += 4 + inSz;
            }

            // Parse 11-byte tile records. Level byte is at offset 9 within each record.
            int tileCount = totalOutputSize / TileRecordSize;
            var tileLevels = new Dictionary<long, byte>(tileCount);
            for (int p = 0; p + TileRecordSize <= writePos; p += TileRecordSize)
            {
                short x = BitConverter.ToInt16(data, p);
                short y = BitConverter.ToInt16(data, p + 2);
                if (x > 0 && y > 0)
                    tileLevels[TileKey(x, y)] = data[p + 9];
            }

            return tileLevels;
        }
    }
}
