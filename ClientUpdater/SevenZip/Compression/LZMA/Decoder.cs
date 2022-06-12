using System;
using System.IO;
using System.Threading;
using SevenZip;
using SevenZip.Compression.LZ;
using SevenZip.Compression.LZMA;
using SevenZip.Compression.RangeCoder;

namespace SevenZip.Compression.LZMA
{
    public class Decoder : ICoder, ISetDecoderProperties
    {
        private class LenDecoder
        {
            private BitDecoder m_Choice;

            private BitDecoder m_Choice2;

            private BitTreeDecoder[] m_LowCoder = new BitTreeDecoder[16];

            private BitTreeDecoder[] m_MidCoder = new BitTreeDecoder[16];

            private BitTreeDecoder m_HighCoder = new BitTreeDecoder(8);

            private uint m_NumPosStates;

            public void Create(uint numPosStates)
            {
                for (uint num = m_NumPosStates; num < numPosStates; num++)
                {
                    m_LowCoder[num] = new BitTreeDecoder(3);
                    m_MidCoder[num] = new BitTreeDecoder(3);
                }
                m_NumPosStates = numPosStates;
            }

            public void Init()
            {
                m_Choice.Init();
                for (uint num = 0u; num < m_NumPosStates; num++)
                {
                    m_LowCoder[num].Init();
                    m_MidCoder[num].Init();
                }
                m_Choice2.Init();
                m_HighCoder.Init();
            }

            public uint Decode(SevenZip.Compression.RangeCoder.Decoder rangeDecoder, uint posState)
            {
                if (m_Choice.Decode(rangeDecoder) == 0)
                {
                    return m_LowCoder[posState].Decode(rangeDecoder);
                }
                uint num = 8u;
                if (m_Choice2.Decode(rangeDecoder) == 0)
                {
                    return num + m_MidCoder[posState].Decode(rangeDecoder);
                }
                num += 8;
                return num + m_HighCoder.Decode(rangeDecoder);
            }
        }

        private class LiteralDecoder
        {
            private struct Decoder2
            {
                private BitDecoder[] m_Decoders;

                public void Create()
                {
                    m_Decoders = new BitDecoder[768];
                }

                public void Init()
                {
                    for (int i = 0; i < 768; i++)
                    {
                        m_Decoders[i].Init();
                    }
                }

                public byte DecodeNormal(SevenZip.Compression.RangeCoder.Decoder rangeDecoder)
                {
                    uint num = 1u;
                    do
                    {
                        num = (num << 1) | m_Decoders[num].Decode(rangeDecoder);
                    }
                    while (num < 256);
                    return (byte)num;
                }

                public byte DecodeWithMatchByte(SevenZip.Compression.RangeCoder.Decoder rangeDecoder, byte matchByte)
                {
                    uint num = 1u;
                    do
                    {
                        uint num2 = (uint)(matchByte >> 7) & 1u;
                        matchByte = (byte)(matchByte << 1);
                        uint num3 = m_Decoders[(1 + num2 << 8) + num].Decode(rangeDecoder);
                        num = (num << 1) | num3;
                        if (num2 != num3)
                        {
                            while (num < 256)
                            {
                                num = (num << 1) | m_Decoders[num].Decode(rangeDecoder);
                            }
                            break;
                        }
                    }
                    while (num < 256);
                    return (byte)num;
                }
            }

            private Decoder2[] m_Coders;

            private int m_NumPrevBits;

            private int m_NumPosBits;

            private uint m_PosMask;

            public void Create(int numPosBits, int numPrevBits)
            {
                if (m_Coders == null || m_NumPrevBits != numPrevBits || m_NumPosBits != numPosBits)
                {
                    m_NumPosBits = numPosBits;
                    m_PosMask = (uint)((1 << numPosBits) - 1);
                    m_NumPrevBits = numPrevBits;
                    uint num = (uint)(1 << m_NumPrevBits + m_NumPosBits);
                    m_Coders = new Decoder2[num];
                    for (uint num2 = 0u; num2 < num; num2++)
                    {
                        m_Coders[num2].Create();
                    }
                }
            }

            public void Init()
            {
                uint num = (uint)(1 << m_NumPrevBits + m_NumPosBits);
                for (uint num2 = 0u; num2 < num; num2++)
                {
                    m_Coders[num2].Init();
                }
            }

            private uint GetState(uint pos, byte prevByte)
            {
                return ((pos & m_PosMask) << m_NumPrevBits) + (uint)(prevByte >> 8 - m_NumPrevBits);
            }

            public byte DecodeNormal(SevenZip.Compression.RangeCoder.Decoder rangeDecoder, uint pos, byte prevByte)
            {
                return m_Coders[GetState(pos, prevByte)].DecodeNormal(rangeDecoder);
            }

            public byte DecodeWithMatchByte(SevenZip.Compression.RangeCoder.Decoder rangeDecoder, uint pos, byte prevByte, byte matchByte)
            {
                return m_Coders[GetState(pos, prevByte)].DecodeWithMatchByte(rangeDecoder, matchByte);
            }
        }

        private OutWindow m_OutWindow = new OutWindow();

        private SevenZip.Compression.RangeCoder.Decoder m_RangeDecoder = new SevenZip.Compression.RangeCoder.Decoder();

        private BitDecoder[] m_IsMatchDecoders = new BitDecoder[192];

        private BitDecoder[] m_IsRepDecoders = new BitDecoder[12];

        private BitDecoder[] m_IsRepG0Decoders = new BitDecoder[12];

        private BitDecoder[] m_IsRepG1Decoders = new BitDecoder[12];

        private BitDecoder[] m_IsRepG2Decoders = new BitDecoder[12];

        private BitDecoder[] m_IsRep0LongDecoders = new BitDecoder[192];

        private BitTreeDecoder[] m_PosSlotDecoder = new BitTreeDecoder[4];

        private BitDecoder[] m_PosDecoders = new BitDecoder[114];

        private BitTreeDecoder m_PosAlignDecoder = new BitTreeDecoder(4);

        private LenDecoder m_LenDecoder = new LenDecoder();

        private LenDecoder m_RepLenDecoder = new LenDecoder();

        private LiteralDecoder m_LiteralDecoder = new LiteralDecoder();

        private uint m_DictionarySize;

        private uint m_DictionarySizeCheck;

        private uint m_PosStateMask;

        private CancellationToken cancellationToken;

        private bool _solid;

        public Decoder()
        {
            m_DictionarySize = uint.MaxValue;
            for (int i = 0; (long)i < 4L; i++)
            {
                m_PosSlotDecoder[i] = new BitTreeDecoder(6);
            }
        }

        public Decoder(CancellationToken cancellationToken)
            : this()
        {
            this.cancellationToken = cancellationToken;
        }

        private void SetDictionarySize(uint dictionarySize)
        {
            if (m_DictionarySize != dictionarySize)
            {
                m_DictionarySize = dictionarySize;
                m_DictionarySizeCheck = Math.Max(m_DictionarySize, 1u);
                uint windowSize = Math.Max(m_DictionarySizeCheck, 4096u);
                m_OutWindow.Create(windowSize);
            }
        }

        private void SetLiteralProperties(int lp, int lc)
        {
            if (lp > 8)
            {
                throw new InvalidParamException();
            }
            if (lc > 8)
            {
                throw new InvalidParamException();
            }
            m_LiteralDecoder.Create(lp, lc);
        }

        private void SetPosBitsProperties(int pb)
        {
            if (pb > 4)
            {
                throw new InvalidParamException();
            }
            uint num = (uint)(1 << pb);
            m_LenDecoder.Create(num);
            m_RepLenDecoder.Create(num);
            m_PosStateMask = num - 1;
        }

        private void Init(Stream inStream, Stream outStream)
        {
            m_RangeDecoder.Init(inStream);
            m_OutWindow.Init(outStream, _solid);
            for (uint num = 0u; num < 12; num++)
            {
                for (uint num2 = 0u; num2 <= m_PosStateMask; num2++)
                {
                    uint num3 = (num << 4) + num2;
                    m_IsMatchDecoders[num3].Init();
                    m_IsRep0LongDecoders[num3].Init();
                }
                m_IsRepDecoders[num].Init();
                m_IsRepG0Decoders[num].Init();
                m_IsRepG1Decoders[num].Init();
                m_IsRepG2Decoders[num].Init();
            }
            m_LiteralDecoder.Init();
            for (uint num = 0u; num < 4; num++)
            {
                m_PosSlotDecoder[num].Init();
            }
            for (uint num = 0u; num < 114; num++)
            {
                m_PosDecoders[num].Init();
            }
            m_LenDecoder.Init();
            m_RepLenDecoder.Init();
            m_PosAlignDecoder.Init();
        }

        public void Code(Stream inStream, Stream outStream, long inSize, long outSize, ICodeProgress progress)
        {
            Init(inStream, outStream);
            Base.State state = default(Base.State);
            state.Init();
            uint num = 0u;
            uint num2 = 0u;
            uint num3 = 0u;
            uint num4 = 0u;
            ulong num5 = 0uL;
            if (num5 < (ulong)outSize)
            {
                if (m_IsMatchDecoders[state.Index << 4].Decode(m_RangeDecoder) != 0)
                {
                    throw new DataErrorException();
                }
                state.UpdateChar();
                byte b = m_LiteralDecoder.DecodeNormal(m_RangeDecoder, 0u, 0);
                m_OutWindow.PutByte(b);
                num5++;
            }
            while (num5 < (ulong)outSize)
            {
                _ = cancellationToken;
                cancellationToken.ThrowIfCancellationRequested();
                uint num6 = (uint)(int)num5 & m_PosStateMask;
                if (m_IsMatchDecoders[(state.Index << 4) + num6].Decode(m_RangeDecoder) == 0)
                {
                    byte @byte = m_OutWindow.GetByte(0u);
                    byte b2 = (state.IsCharState() ? m_LiteralDecoder.DecodeNormal(m_RangeDecoder, (uint)num5, @byte) : m_LiteralDecoder.DecodeWithMatchByte(m_RangeDecoder, (uint)num5, @byte, m_OutWindow.GetByte(num)));
                    m_OutWindow.PutByte(b2);
                    state.UpdateChar();
                    num5++;
                    continue;
                }
                uint num8;
                if (m_IsRepDecoders[state.Index].Decode(m_RangeDecoder) == 1)
                {
                    if (m_IsRepG0Decoders[state.Index].Decode(m_RangeDecoder) == 0)
                    {
                        if (m_IsRep0LongDecoders[(state.Index << 4) + num6].Decode(m_RangeDecoder) == 0)
                        {
                            state.UpdateShortRep();
                            m_OutWindow.PutByte(m_OutWindow.GetByte(num));
                            num5++;
                            continue;
                        }
                    }
                    else
                    {
                        uint num7;
                        if (m_IsRepG1Decoders[state.Index].Decode(m_RangeDecoder) == 0)
                        {
                            num7 = num2;
                        }
                        else
                        {
                            if (m_IsRepG2Decoders[state.Index].Decode(m_RangeDecoder) == 0)
                            {
                                num7 = num3;
                            }
                            else
                            {
                                num7 = num4;
                                num4 = num3;
                            }
                            num3 = num2;
                        }
                        num2 = num;
                        num = num7;
                    }
                    num8 = m_RepLenDecoder.Decode(m_RangeDecoder, num6) + 2;
                    state.UpdateRep();
                }
                else
                {
                    num4 = num3;
                    num3 = num2;
                    num2 = num;
                    num8 = 2 + m_LenDecoder.Decode(m_RangeDecoder, num6);
                    state.UpdateMatch();
                    uint num9 = m_PosSlotDecoder[Base.GetLenToPosState(num8)].Decode(m_RangeDecoder);
                    if (num9 >= 4)
                    {
                        int num10 = (int)((num9 >> 1) - 1);
                        num = (2 | (num9 & 1)) << num10;
                        if (num9 < 14)
                        {
                            num += BitTreeDecoder.ReverseDecode(m_PosDecoders, num - num9 - 1, m_RangeDecoder, num10);
                        }
                        else
                        {
                            num += m_RangeDecoder.DecodeDirectBits(num10 - 4) << 4;
                            num += m_PosAlignDecoder.ReverseDecode(m_RangeDecoder);
                        }
                    }
                    else
                    {
                        num = num9;
                    }
                }
                if (num >= m_OutWindow.TrainSize + num5 || num >= m_DictionarySizeCheck)
                {
                    if (num == uint.MaxValue)
                    {
                        break;
                    }
                    throw new DataErrorException();
                }
                m_OutWindow.CopyBlock(num, num8);
                num5 += num8;
            }
            m_OutWindow.Flush();
            m_OutWindow.ReleaseStream();
            m_RangeDecoder.ReleaseStream();
        }

        public void SetDecoderProperties(byte[] properties)
        {
            if (properties.Length < 5)
            {
                throw new InvalidParamException();
            }
            int lc = (int)properties[0] % 9;
            int num = (int)properties[0] / 9;
            int lp = num % 5;
            int num2 = num / 5;
            if (num2 > 4)
            {
                throw new InvalidParamException();
            }
            uint num3 = 0u;
            for (int i = 0; i < 4; i++)
            {
                num3 += (uint)(properties[1 + i] << i * 8);
            }
            SetDictionarySize(num3);
            SetLiteralProperties(lp, lc);
            SetPosBitsProperties(num2);
        }

        public bool Train(Stream stream)
        {
            _solid = true;
            return m_OutWindow.Train(stream);
        }
    }
}