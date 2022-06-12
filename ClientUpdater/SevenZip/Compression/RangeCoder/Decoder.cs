using System.IO;

namespace SevenZip.Compression.RangeCoder
{
    internal class Decoder
    {
        public const uint kTopValue = 16777216u;

        public uint Range;

        public uint Code;

        public Stream Stream;

        public void Init(Stream stream)
        {
            Stream = stream;
            Code = 0u;
            Range = uint.MaxValue;
            for (int i = 0; i < 5; i++)
            {
                Code = (Code << 8) | (byte)Stream.ReadByte();
            }
        }

        public void ReleaseStream()
        {
            Stream = null;
        }

        public void CloseStream()
        {
            Stream.Close();
        }

        public void Normalize()
        {
            while (Range < 16777216)
            {
                Code = (Code << 8) | (byte)Stream.ReadByte();
                Range <<= 8;
            }
        }

        public void Normalize2()
        {
            if (Range < 16777216)
            {
                Code = (Code << 8) | (byte)Stream.ReadByte();
                Range <<= 8;
            }
        }

        public uint GetThreshold(uint total)
        {
            return Code / (Range /= total);
        }

        public void Decode(uint start, uint size, uint total)
        {
            Code -= start * Range;
            Range *= size;
            Normalize();
        }

        public uint DecodeDirectBits(int numTotalBits)
        {
            uint num = Range;
            uint num2 = Code;
            uint num3 = 0u;
            for (int num4 = numTotalBits; num4 > 0; num4--)
            {
                num >>= 1;
                uint num5 = num2 - num >> 31;
                num2 -= num & (num5 - 1);
                num3 = (num3 << 1) | (1 - num5);
                if (num < 16777216)
                {
                    num2 = (num2 << 8) | (byte)Stream.ReadByte();
                    num <<= 8;
                }
            }
            Range = num;
            Code = num2;
            return num3;
        }

        public uint DecodeBit(uint size0, int numTotalBits)
        {
            uint num = (Range >> numTotalBits) * size0;
            uint result;
            if (Code < num)
            {
                result = 0u;
                Range = num;
            }
            else
            {
                result = 1u;
                Code -= num;
                Range -= num;
            }
            Normalize();
            return result;
        }
    }
}