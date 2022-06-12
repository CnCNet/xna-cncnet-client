using SevenZip;

namespace SevenZip
{
    internal class CRC
    {
        public static readonly uint[] Table;

        private uint _value = uint.MaxValue;

        static CRC()
        {
            Table = new uint[256];
            for (uint num = 0u; num < 256; num++)
            {
                uint num2 = num;
                for (int i = 0; i < 8; i++)
                {
                    num2 = (((num2 & 1) == 0) ? (num2 >> 1) : ((num2 >> 1) ^ 0xEDB88320u));
                }
                Table[num] = num2;
            }
        }

        public void Init()
        {
            _value = uint.MaxValue;
        }

        public void UpdateByte(byte b)
        {
            _value = Table[(byte)_value ^ b] ^ (_value >> 8);
        }

        public void Update(byte[] data, uint offset, uint size)
        {
            for (uint num = 0u; num < size; num++)
            {
                _value = Table[(byte)_value ^ data[offset + num]] ^ (_value >> 8);
            }
        }

        public uint GetDigest()
        {
            return _value ^ 0xFFFFFFFFu;
        }

        private static uint CalculateDigest(byte[] data, uint offset, uint size)
        {
            CRC cRC = new CRC();
            cRC.Update(data, offset, size);
            return cRC.GetDigest();
        }

        private static bool VerifyDigest(uint digest, byte[] data, uint offset, uint size)
        {
            return CalculateDigest(data, offset, size) == digest;
        }
    }
}