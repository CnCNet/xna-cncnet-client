using SevenZip.Compression.RangeCoder;

namespace SevenZip.Compression.RangeCoder
{
    internal struct BitDecoder
    {
        public const int kNumBitModelTotalBits = 11;

        public const uint kBitModelTotal = 2048u;

        private const int kNumMoveBits = 5;

        private uint Prob;

        public void UpdateModel(int numMoveBits, uint symbol)
        {
            if (symbol == 0)
            {
                Prob += 2048 - Prob >> numMoveBits;
            }
            else
            {
                Prob -= Prob >> numMoveBits;
            }
        }

        public void Init()
        {
            Prob = 1024u;
        }

        public uint Decode(Decoder rangeDecoder)
        {
            uint num = (rangeDecoder.Range >> 11) * Prob;
            if (rangeDecoder.Code < num)
            {
                rangeDecoder.Range = num;
                Prob += 2048 - Prob >> 5;
                if (rangeDecoder.Range < 16777216)
                {
                    rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
                    rangeDecoder.Range <<= 8;
                }
                return 0u;
            }
            rangeDecoder.Range -= num;
            rangeDecoder.Code -= num;
            Prob -= Prob >> 5;
            if (rangeDecoder.Range < 16777216)
            {
                rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
                rangeDecoder.Range <<= 8;
            }
            return 1u;
        }
    }
}