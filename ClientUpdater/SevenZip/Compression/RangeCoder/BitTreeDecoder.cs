using SevenZip.Compression.RangeCoder;

namespace SevenZip.Compression.RangeCoder
{
    internal struct BitTreeDecoder
    {
        private BitDecoder[] Models;

        private int NumBitLevels;

        public BitTreeDecoder(int numBitLevels)
        {
            NumBitLevels = numBitLevels;
            Models = new BitDecoder[1 << numBitLevels];
        }

        public void Init()
        {
            for (uint num = 1u; num < 1 << NumBitLevels; num++)
            {
                Models[num].Init();
            }
        }

        public uint Decode(Decoder rangeDecoder)
        {
            uint num = 1u;
            for (int num2 = NumBitLevels; num2 > 0; num2--)
            {
                num = (num << 1) + Models[num].Decode(rangeDecoder);
            }
            return num - (uint)(1 << NumBitLevels);
        }

        public uint ReverseDecode(Decoder rangeDecoder)
        {
            uint num = 1u;
            uint num2 = 0u;
            for (int i = 0; i < NumBitLevels; i++)
            {
                uint num3 = Models[num].Decode(rangeDecoder);
                num <<= 1;
                num += num3;
                num2 |= num3 << i;
            }
            return num2;
        }

        public static uint ReverseDecode(BitDecoder[] Models, uint startIndex, Decoder rangeDecoder, int NumBitLevels)
        {
            uint num = 1u;
            uint num2 = 0u;
            for (int i = 0; i < NumBitLevels; i++)
            {
                uint num3 = Models[startIndex + num].Decode(rangeDecoder);
                num <<= 1;
                num += num3;
                num2 |= num3 << i;
            }
            return num2;
        }
    }
}