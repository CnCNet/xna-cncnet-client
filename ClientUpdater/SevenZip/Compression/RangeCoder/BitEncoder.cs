using SevenZip.Compression.RangeCoder;

namespace SevenZip.Compression.RangeCoder
{
    internal struct BitEncoder
    {
        public const int kNumBitModelTotalBits = 11;

        public const uint kBitModelTotal = 2048u;

        private const int kNumMoveBits = 5;

        private const int kNumMoveReducingBits = 2;

        public const int kNumBitPriceShiftBits = 6;

        private uint Prob;

        private static uint[] ProbPrices;

        public void Init()
        {
            Prob = 1024u;
        }

        public void UpdateModel(uint symbol)
        {
            if (symbol == 0)
            {
                Prob += 2048 - Prob >> 5;
            }
            else
            {
                Prob -= Prob >> 5;
            }
        }

        public void Encode(Encoder encoder, uint symbol)
        {
            uint num = (encoder.Range >> 11) * Prob;
            if (symbol == 0)
            {
                encoder.Range = num;
                Prob += 2048 - Prob >> 5;
            }
            else
            {
                encoder.Low += num;
                encoder.Range -= num;
                Prob -= Prob >> 5;
            }
            if (encoder.Range < 16777216)
            {
                encoder.Range <<= 8;
                encoder.ShiftLow();
            }
        }

        static BitEncoder()
        {
            ProbPrices = new uint[512];
            for (int num = 8; num >= 0; num--)
            {
                uint num2 = (uint)(1 << 9 - num - 1);
                uint num3 = (uint)(1 << 9 - num);
                for (uint num4 = num2; num4 < num3; num4++)
                {
                    ProbPrices[num4] = (uint)(num << 6) + (num3 - num4 << 6 >> 9 - num - 1);
                }
            }
        }

        public uint GetPrice(uint symbol)
        {
            return ProbPrices[(((Prob - symbol) ^ (int)(0 - symbol)) & 0x7FF) >> 2];
        }

        public uint GetPrice0()
        {
            return ProbPrices[Prob >> 2];
        }

        public uint GetPrice1()
        {
            return ProbPrices[2048 - Prob >> 2];
        }
    }
}