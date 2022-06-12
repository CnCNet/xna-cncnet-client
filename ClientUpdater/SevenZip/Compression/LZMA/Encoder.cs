using System;
using System.IO;
using System.Threading;
using SevenZip;
using SevenZip.Compression.LZ;
using SevenZip.Compression.LZMA;
using SevenZip.Compression.RangeCoder;

namespace SevenZip.Compression.LZMA
{
    public class Encoder : ICoder, ISetCoderProperties, IWriteCoderProperties
    {
        private enum EMatchFinderType
        {
            BT2,
            BT4
        }

        private class LiteralEncoder
        {
            public struct Encoder2
            {
                private BitEncoder[] m_Encoders;

                public void Create()
                {
                    m_Encoders = new BitEncoder[768];
                }

                public void Init()
                {
                    for (int i = 0; i < 768; i++)
                    {
                        m_Encoders[i].Init();
                    }
                }

                public void Encode(SevenZip.Compression.RangeCoder.Encoder rangeEncoder, byte symbol)
                {
                    uint num = 1u;
                    for (int num2 = 7; num2 >= 0; num2--)
                    {
                        uint num3 = (uint)(symbol >> num2) & 1u;
                        m_Encoders[num].Encode(rangeEncoder, num3);
                        num = (num << 1) | num3;
                    }
                }

                public void EncodeMatched(SevenZip.Compression.RangeCoder.Encoder rangeEncoder, byte matchByte, byte symbol)
                {
                    uint num = 1u;
                    bool flag = true;
                    for (int num2 = 7; num2 >= 0; num2--)
                    {
                        uint num3 = (uint)(symbol >> num2) & 1u;
                        uint num4 = num;
                        if (flag)
                        {
                            uint num5 = (uint)(matchByte >> num2) & 1u;
                            num4 += 1 + num5 << 8;
                            flag = num5 == num3;
                        }
                        m_Encoders[num4].Encode(rangeEncoder, num3);
                        num = (num << 1) | num3;
                    }
                }

                public uint GetPrice(bool matchMode, byte matchByte, byte symbol)
                {
                    uint num = 0u;
                    uint num2 = 1u;
                    int num3 = 7;
                    if (matchMode)
                    {
                        while (num3 >= 0)
                        {
                            uint num4 = (uint)(matchByte >> num3) & 1u;
                            uint num5 = (uint)(symbol >> num3) & 1u;
                            num += m_Encoders[(1 + num4 << 8) + num2].GetPrice(num5);
                            num2 = (num2 << 1) | num5;
                            if (num4 != num5)
                            {
                                num3--;
                                break;
                            }
                            num3--;
                        }
                    }
                    while (num3 >= 0)
                    {
                        uint num6 = (uint)(symbol >> num3) & 1u;
                        num += m_Encoders[num2].GetPrice(num6);
                        num2 = (num2 << 1) | num6;
                        num3--;
                    }
                    return num;
                }
            }

            private Encoder2[] m_Coders;

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
                    m_Coders = new Encoder2[num];
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

            public Encoder2 GetSubCoder(uint pos, byte prevByte)
            {
                return m_Coders[(int)((pos & m_PosMask) << m_NumPrevBits) + (prevByte >> 8 - m_NumPrevBits)];
            }
        }

        private class LenEncoder
        {
            private BitEncoder _choice;

            private BitEncoder _choice2;

            private BitTreeEncoder[] _lowCoder = new BitTreeEncoder[16];

            private BitTreeEncoder[] _midCoder = new BitTreeEncoder[16];

            private BitTreeEncoder _highCoder = new BitTreeEncoder(8);

            public LenEncoder()
            {
                for (uint num = 0u; num < 16; num++)
                {
                    _lowCoder[num] = new BitTreeEncoder(3);
                    _midCoder[num] = new BitTreeEncoder(3);
                }
            }

            public void Init(uint numPosStates)
            {
                _choice.Init();
                _choice2.Init();
                for (uint num = 0u; num < numPosStates; num++)
                {
                    _lowCoder[num].Init();
                    _midCoder[num].Init();
                }
                _highCoder.Init();
            }

            public void Encode(SevenZip.Compression.RangeCoder.Encoder rangeEncoder, uint symbol, uint posState)
            {
                if (symbol < 8)
                {
                    _choice.Encode(rangeEncoder, 0u);
                    _lowCoder[posState].Encode(rangeEncoder, symbol);
                    return;
                }
                symbol -= 8;
                _choice.Encode(rangeEncoder, 1u);
                if (symbol < 8)
                {
                    _choice2.Encode(rangeEncoder, 0u);
                    _midCoder[posState].Encode(rangeEncoder, symbol);
                }
                else
                {
                    _choice2.Encode(rangeEncoder, 1u);
                    _highCoder.Encode(rangeEncoder, symbol - 8);
                }
            }

            public void SetPrices(uint posState, uint numSymbols, uint[] prices, uint st)
            {
                uint price = _choice.GetPrice0();
                uint price2 = _choice.GetPrice1();
                uint num = price2 + _choice2.GetPrice0();
                uint num2 = price2 + _choice2.GetPrice1();
                uint num3 = 0u;
                for (num3 = 0u; num3 < 8; num3++)
                {
                    if (num3 >= numSymbols)
                    {
                        return;
                    }
                    prices[st + num3] = price + _lowCoder[posState].GetPrice(num3);
                }
                for (; num3 < 16; num3++)
                {
                    if (num3 >= numSymbols)
                    {
                        return;
                    }
                    prices[st + num3] = num + _midCoder[posState].GetPrice(num3 - 8);
                }
                for (; num3 < numSymbols; num3++)
                {
                    prices[st + num3] = num2 + _highCoder.GetPrice(num3 - 8 - 8);
                }
            }
        }

        private class LenPriceTableEncoder : LenEncoder
        {
            private uint[] _prices = new uint[4352];

            private uint _tableSize;

            private uint[] _counters = new uint[16];

            public void SetTableSize(uint tableSize)
            {
                _tableSize = tableSize;
            }

            public uint GetPrice(uint symbol, uint posState)
            {
                return _prices[posState * 272 + symbol];
            }

            private void UpdateTable(uint posState)
            {
                SetPrices(posState, _tableSize, _prices, posState * 272);
                _counters[posState] = _tableSize;
            }

            public void UpdateTables(uint numPosStates)
            {
                for (uint num = 0u; num < numPosStates; num++)
                {
                    UpdateTable(num);
                }
            }

            public new void Encode(SevenZip.Compression.RangeCoder.Encoder rangeEncoder, uint symbol, uint posState)
            {
                base.Encode(rangeEncoder, symbol, posState);
                if (--_counters[posState] == 0)
                {
                    UpdateTable(posState);
                }
            }
        }

        private class Optimal
        {
            public Base.State State;

            public bool Prev1IsChar;

            public bool Prev2;

            public uint PosPrev2;

            public uint BackPrev2;

            public uint Price;

            public uint PosPrev;

            public uint BackPrev;

            public uint Backs0;

            public uint Backs1;

            public uint Backs2;

            public uint Backs3;

            public void MakeAsChar()
            {
                BackPrev = uint.MaxValue;
                Prev1IsChar = false;
            }

            public void MakeAsShortRep()
            {
                BackPrev = 0u;
                Prev1IsChar = false;
            }

            public bool IsShortRep()
            {
                return BackPrev == 0;
            }
        }

        private const uint kIfinityPrice = 268435455u;

        private static byte[] g_FastPos;

        private Base.State _state;

        private byte _previousByte;

        private uint[] _repDistances = new uint[4];

        private const int kDefaultDictionaryLogSize = 22;

        private const uint kNumFastBytesDefault = 32u;

        private const uint kNumLenSpecSymbols = 16u;

        private const uint kNumOpts = 4096u;

        private Optimal[] _optimum = new Optimal[4096];

        private IMatchFinder _matchFinder;

        private SevenZip.Compression.RangeCoder.Encoder _rangeEncoder = new SevenZip.Compression.RangeCoder.Encoder();

        private BitEncoder[] _isMatch = new BitEncoder[192];

        private BitEncoder[] _isRep = new BitEncoder[12];

        private BitEncoder[] _isRepG0 = new BitEncoder[12];

        private BitEncoder[] _isRepG1 = new BitEncoder[12];

        private BitEncoder[] _isRepG2 = new BitEncoder[12];

        private BitEncoder[] _isRep0Long = new BitEncoder[192];

        private BitTreeEncoder[] _posSlotEncoder = new BitTreeEncoder[4];

        private BitEncoder[] _posEncoders = new BitEncoder[114];

        private BitTreeEncoder _posAlignEncoder = new BitTreeEncoder(4);

        private LenPriceTableEncoder _lenEncoder = new LenPriceTableEncoder();

        private LenPriceTableEncoder _repMatchLenEncoder = new LenPriceTableEncoder();

        private LiteralEncoder _literalEncoder = new LiteralEncoder();

        private uint[] _matchDistances = new uint[548];

        private uint _numFastBytes = 32u;

        private uint _longestMatchLength;

        private uint _numDistancePairs;

        private uint _additionalOffset;

        private uint _optimumEndIndex;

        private uint _optimumCurrentIndex;

        private bool _longestMatchWasFound;

        private uint[] _posSlotPrices = new uint[256];

        private uint[] _distancesPrices = new uint[512];

        private uint[] _alignPrices = new uint[16];

        private uint _alignPriceCount;

        private uint _distTableSize = 44u;

        private int _posStateBits = 2;

        private uint _posStateMask = 3u;

        private int _numLiteralPosStateBits;

        private int _numLiteralContextBits = 3;

        private uint _dictionarySize = 4194304u;

        private uint _dictionarySizePrev = uint.MaxValue;

        private uint _numFastBytesPrev = uint.MaxValue;

        private long nowPos64;

        private bool _finished;

        private Stream _inStream;

        private EMatchFinderType _matchFinderType = EMatchFinderType.BT4;

        private bool _writeEndMark;

        private bool _needReleaseMFStream;

        private CancellationToken cancellationToken;

        private uint[] reps = new uint[4];

        private uint[] repLens = new uint[4];

        private const int kPropSize = 5;

        private byte[] properties = new byte[5];

        private uint[] tempPrices = new uint[128];

        private uint _matchPriceCount;

        private static string[] kMatchFinderIDs;

        private uint _trainSize;

        static Encoder()
        {
            g_FastPos = new byte[2048];
            kMatchFinderIDs = new string[2] { "BT2", "BT4" };
            int num = 2;
            g_FastPos[0] = 0;
            g_FastPos[1] = 1;
            for (byte b = 2; b < 22; b = (byte)(b + 1))
            {
                uint num2 = (uint)(1 << (b >> 1) - 1);
                uint num3 = 0u;
                while (num3 < num2)
                {
                    g_FastPos[num] = b;
                    num3++;
                    num++;
                }
            }
        }

        private static uint GetPosSlot(uint pos)
        {
            if (pos < 2048)
            {
                return g_FastPos[pos];
            }
            if (pos < 2097152)
            {
                return (uint)(g_FastPos[pos >> 10] + 20);
            }
            return (uint)(g_FastPos[pos >> 20] + 40);
        }

        private static uint GetPosSlot2(uint pos)
        {
            if (pos < 131072)
            {
                return (uint)(g_FastPos[pos >> 6] + 12);
            }
            if (pos < 134217728)
            {
                return (uint)(g_FastPos[pos >> 16] + 32);
            }
            return (uint)(g_FastPos[pos >> 26] + 52);
        }

        private void BaseInit()
        {
            _state.Init();
            _previousByte = 0;
            for (uint num = 0u; num < 4; num++)
            {
                _repDistances[num] = 0u;
            }
        }

        private void Create()
        {
            if (_matchFinder == null)
            {
                BinTree binTree = new BinTree();
                int type = 4;
                if (_matchFinderType == EMatchFinderType.BT2)
                {
                    type = 2;
                }
                binTree.SetType(type);
                _matchFinder = binTree;
            }
            _literalEncoder.Create(_numLiteralPosStateBits, _numLiteralContextBits);
            if (_dictionarySize != _dictionarySizePrev || _numFastBytesPrev != _numFastBytes)
            {
                _matchFinder.Create(_dictionarySize, 4096u, _numFastBytes, 274u);
                _dictionarySizePrev = _dictionarySize;
                _numFastBytesPrev = _numFastBytes;
            }
        }

        public Encoder()
        {
            for (int i = 0; (long)i < 4096L; i++)
            {
                _optimum[i] = new Optimal();
            }
            for (int j = 0; (long)j < 4L; j++)
            {
                _posSlotEncoder[j] = new BitTreeEncoder(6);
            }
        }

        public Encoder(CancellationToken cancellationToken)
            : this()
        {
            this.cancellationToken = cancellationToken;
        }

        private void SetWriteEndMarkerMode(bool writeEndMarker)
        {
            _writeEndMark = writeEndMarker;
        }

        private void Init()
        {
            BaseInit();
            _rangeEncoder.Init();
            for (uint num = 0u; num < 12; num++)
            {
                for (uint num2 = 0u; num2 <= _posStateMask; num2++)
                {
                    uint num3 = (num << 4) + num2;
                    _isMatch[num3].Init();
                    _isRep0Long[num3].Init();
                }
                _isRep[num].Init();
                _isRepG0[num].Init();
                _isRepG1[num].Init();
                _isRepG2[num].Init();
            }
            _literalEncoder.Init();
            for (uint num = 0u; num < 4; num++)
            {
                _posSlotEncoder[num].Init();
            }
            for (uint num = 0u; num < 114; num++)
            {
                _posEncoders[num].Init();
            }
            _lenEncoder.Init((uint)(1 << _posStateBits));
            _repMatchLenEncoder.Init((uint)(1 << _posStateBits));
            _posAlignEncoder.Init();
            _longestMatchWasFound = false;
            _optimumEndIndex = 0u;
            _optimumCurrentIndex = 0u;
            _additionalOffset = 0u;
        }

        private void ReadMatchDistances(out uint lenRes, out uint numDistancePairs)
        {
            lenRes = 0u;
            numDistancePairs = _matchFinder.GetMatches(_matchDistances);
            if (numDistancePairs != 0)
            {
                lenRes = _matchDistances[numDistancePairs - 2];
                if (lenRes == _numFastBytes)
                {
                    lenRes += _matchFinder.GetMatchLen((int)(lenRes - 1), _matchDistances[numDistancePairs - 1], 273 - lenRes);
                }
            }
            _additionalOffset++;
        }

        private void MovePos(uint num)
        {
            if (num != 0)
            {
                _matchFinder.Skip(num);
                _additionalOffset += num;
            }
        }

        private uint GetRepLen1Price(Base.State state, uint posState)
        {
            return _isRepG0[state.Index].GetPrice0() + _isRep0Long[(state.Index << 4) + posState].GetPrice0();
        }

        private uint GetPureRepPrice(uint repIndex, Base.State state, uint posState)
        {
            uint price;
            if (repIndex == 0)
            {
                price = _isRepG0[state.Index].GetPrice0();
                return price + _isRep0Long[(state.Index << 4) + posState].GetPrice1();
            }
            price = _isRepG0[state.Index].GetPrice1();
            if (repIndex == 1)
            {
                return price + _isRepG1[state.Index].GetPrice0();
            }
            price += _isRepG1[state.Index].GetPrice1();
            return price + _isRepG2[state.Index].GetPrice(repIndex - 2);
        }

        private uint GetRepPrice(uint repIndex, uint len, Base.State state, uint posState)
        {
            uint price = _repMatchLenEncoder.GetPrice(len - 2, posState);
            return price + GetPureRepPrice(repIndex, state, posState);
        }

        private uint GetPosLenPrice(uint pos, uint len, uint posState)
        {
            uint lenToPosState = Base.GetLenToPosState(len);
            uint num = ((pos >= 128) ? (_posSlotPrices[(lenToPosState << 6) + GetPosSlot2(pos)] + _alignPrices[pos & 0xF]) : _distancesPrices[lenToPosState * 128 + pos]);
            return num + _lenEncoder.GetPrice(len - 2, posState);
        }

        private uint Backward(out uint backRes, uint cur)
        {
            _optimumEndIndex = cur;
            uint posPrev = _optimum[cur].PosPrev;
            uint backPrev = _optimum[cur].BackPrev;
            do
            {
                if (_optimum[cur].Prev1IsChar)
                {
                    _optimum[posPrev].MakeAsChar();
                    _optimum[posPrev].PosPrev = posPrev - 1;
                    if (_optimum[cur].Prev2)
                    {
                        _optimum[posPrev - 1].Prev1IsChar = false;
                        _optimum[posPrev - 1].PosPrev = _optimum[cur].PosPrev2;
                        _optimum[posPrev - 1].BackPrev = _optimum[cur].BackPrev2;
                    }
                }
                uint num = posPrev;
                uint backPrev2 = backPrev;
                backPrev = _optimum[num].BackPrev;
                posPrev = _optimum[num].PosPrev;
                _optimum[num].BackPrev = backPrev2;
                _optimum[num].PosPrev = cur;
                cur = num;
            }
            while (cur != 0);
            backRes = _optimum[0].BackPrev;
            _optimumCurrentIndex = _optimum[0].PosPrev;
            return _optimumCurrentIndex;
        }

        private uint GetOptimum(uint position, out uint backRes)
        {
            if (_optimumEndIndex != _optimumCurrentIndex)
            {
                uint result = _optimum[_optimumCurrentIndex].PosPrev - _optimumCurrentIndex;
                backRes = _optimum[_optimumCurrentIndex].BackPrev;
                _optimumCurrentIndex = _optimum[_optimumCurrentIndex].PosPrev;
                return result;
            }
            _optimumCurrentIndex = (_optimumEndIndex = 0u);
            uint lenRes;
            uint numDistancePairs;
            if (!_longestMatchWasFound)
            {
                ReadMatchDistances(out lenRes, out numDistancePairs);
            }
            else
            {
                lenRes = _longestMatchLength;
                numDistancePairs = _numDistancePairs;
                _longestMatchWasFound = false;
            }
            uint num = _matchFinder.GetNumAvailableBytes() + 1;
            if (num < 2)
            {
                backRes = uint.MaxValue;
                return 1u;
            }
            if (num > 273)
            {
                num = 273u;
            }
            uint num2 = 0u;
            for (uint num3 = 0u; num3 < 4; num3++)
            {
                reps[num3] = _repDistances[num3];
                repLens[num3] = _matchFinder.GetMatchLen(-1, reps[num3], 273u);
                if (repLens[num3] > repLens[num2])
                {
                    num2 = num3;
                }
            }
            if (repLens[num2] >= _numFastBytes)
            {
                backRes = num2;
                uint num4 = repLens[num2];
                MovePos(num4 - 1);
                return num4;
            }
            if (lenRes >= _numFastBytes)
            {
                backRes = _matchDistances[numDistancePairs - 1] + 4;
                MovePos(lenRes - 1);
                return lenRes;
            }
            byte indexByte = _matchFinder.GetIndexByte(-1);
            byte indexByte2 = _matchFinder.GetIndexByte((int)(0 - _repDistances[0] - 1 - 1));
            if (lenRes < 2 && indexByte != indexByte2 && repLens[num2] < 2)
            {
                backRes = uint.MaxValue;
                return 1u;
            }
            _optimum[0].State = _state;
            uint num5 = position & _posStateMask;
            _optimum[1].Price = _isMatch[(_state.Index << 4) + num5].GetPrice0() + _literalEncoder.GetSubCoder(position, _previousByte).GetPrice(!_state.IsCharState(), indexByte2, indexByte);
            _optimum[1].MakeAsChar();
            uint price = _isMatch[(_state.Index << 4) + num5].GetPrice1();
            uint num6 = price + _isRep[_state.Index].GetPrice1();
            if (indexByte2 == indexByte)
            {
                uint num7 = num6 + GetRepLen1Price(_state, num5);
                if (num7 < _optimum[1].Price)
                {
                    _optimum[1].Price = num7;
                    _optimum[1].MakeAsShortRep();
                }
            }
            uint num8 = ((lenRes >= repLens[num2]) ? lenRes : repLens[num2]);
            if (num8 < 2)
            {
                backRes = _optimum[1].BackPrev;
                return 1u;
            }
            _optimum[1].PosPrev = 0u;
            _optimum[0].Backs0 = reps[0];
            _optimum[0].Backs1 = reps[1];
            _optimum[0].Backs2 = reps[2];
            _optimum[0].Backs3 = reps[3];
            uint num9 = num8;
            do
            {
                _optimum[num9--].Price = 268435455u;
            }
            while (num9 >= 2);
            for (uint num3 = 0u; num3 < 4; num3++)
            {
                uint num10 = repLens[num3];
                if (num10 < 2)
                {
                    continue;
                }
                uint num11 = num6 + GetPureRepPrice(num3, _state, num5);
                do
                {
                    uint num12 = num11 + _repMatchLenEncoder.GetPrice(num10 - 2, num5);
                    Optimal optimal = _optimum[num10];
                    if (num12 < optimal.Price)
                    {
                        optimal.Price = num12;
                        optimal.PosPrev = 0u;
                        optimal.BackPrev = num3;
                        optimal.Prev1IsChar = false;
                    }
                }
                while (--num10 >= 2);
            }
            uint num13 = price + _isRep[_state.Index].GetPrice0();
            num9 = ((repLens[0] >= 2) ? (repLens[0] + 1) : 2u);
            if (num9 <= lenRes)
            {
                uint num14;
                for (num14 = 0u; num9 > _matchDistances[num14]; num14 += 2)
                {
                }
                while (true)
                {
                    uint num15 = _matchDistances[num14 + 1];
                    uint num16 = num13 + GetPosLenPrice(num15, num9, num5);
                    Optimal optimal2 = _optimum[num9];
                    if (num16 < optimal2.Price)
                    {
                        optimal2.Price = num16;
                        optimal2.PosPrev = 0u;
                        optimal2.BackPrev = num15 + 4;
                        optimal2.Prev1IsChar = false;
                    }
                    if (num9 == _matchDistances[num14])
                    {
                        num14 += 2;
                        if (num14 == numDistancePairs)
                        {
                            break;
                        }
                    }
                    num9++;
                }
            }
            uint num17 = 0u;
            uint lenRes2;
            while (true)
            {
                num17++;
                if (num17 == num8)
                {
                    return Backward(out backRes, num17);
                }
                ReadMatchDistances(out lenRes2, out numDistancePairs);
                if (lenRes2 >= _numFastBytes)
                {
                    break;
                }
                position++;
                uint num18 = _optimum[num17].PosPrev;
                Base.State state;
                if (_optimum[num17].Prev1IsChar)
                {
                    num18--;
                    if (_optimum[num17].Prev2)
                    {
                        state = _optimum[_optimum[num17].PosPrev2].State;
                        if (_optimum[num17].BackPrev2 < 4)
                        {
                            state.UpdateRep();
                        }
                        else
                        {
                            state.UpdateMatch();
                        }
                    }
                    else
                    {
                        state = _optimum[num18].State;
                    }
                    state.UpdateChar();
                }
                else
                {
                    state = _optimum[num18].State;
                }
                if (num18 == num17 - 1)
                {
                    if (_optimum[num17].IsShortRep())
                    {
                        state.UpdateShortRep();
                    }
                    else
                    {
                        state.UpdateChar();
                    }
                }
                else
                {
                    uint num19;
                    if (_optimum[num17].Prev1IsChar && _optimum[num17].Prev2)
                    {
                        num18 = _optimum[num17].PosPrev2;
                        num19 = _optimum[num17].BackPrev2;
                        state.UpdateRep();
                    }
                    else
                    {
                        num19 = _optimum[num17].BackPrev;
                        if (num19 < 4)
                        {
                            state.UpdateRep();
                        }
                        else
                        {
                            state.UpdateMatch();
                        }
                    }
                    Optimal optimal3 = _optimum[num18];
                    switch (num19)
                    {
                        case 0u:
                            reps[0] = optimal3.Backs0;
                            reps[1] = optimal3.Backs1;
                            reps[2] = optimal3.Backs2;
                            reps[3] = optimal3.Backs3;
                            break;
                        case 1u:
                            reps[0] = optimal3.Backs1;
                            reps[1] = optimal3.Backs0;
                            reps[2] = optimal3.Backs2;
                            reps[3] = optimal3.Backs3;
                            break;
                        case 2u:
                            reps[0] = optimal3.Backs2;
                            reps[1] = optimal3.Backs0;
                            reps[2] = optimal3.Backs1;
                            reps[3] = optimal3.Backs3;
                            break;
                        case 3u:
                            reps[0] = optimal3.Backs3;
                            reps[1] = optimal3.Backs0;
                            reps[2] = optimal3.Backs1;
                            reps[3] = optimal3.Backs2;
                            break;
                        default:
                            reps[0] = num19 - 4;
                            reps[1] = optimal3.Backs0;
                            reps[2] = optimal3.Backs1;
                            reps[3] = optimal3.Backs2;
                            break;
                    }
                }
                _optimum[num17].State = state;
                _optimum[num17].Backs0 = reps[0];
                _optimum[num17].Backs1 = reps[1];
                _optimum[num17].Backs2 = reps[2];
                _optimum[num17].Backs3 = reps[3];
                uint price2 = _optimum[num17].Price;
                indexByte = _matchFinder.GetIndexByte(-1);
                indexByte2 = _matchFinder.GetIndexByte((int)(0 - reps[0] - 1 - 1));
                num5 = position & _posStateMask;
                uint num20 = price2 + _isMatch[(state.Index << 4) + num5].GetPrice0() + _literalEncoder.GetSubCoder(position, _matchFinder.GetIndexByte(-2)).GetPrice(!state.IsCharState(), indexByte2, indexByte);
                Optimal optimal4 = _optimum[num17 + 1];
                bool flag = false;
                if (num20 < optimal4.Price)
                {
                    optimal4.Price = num20;
                    optimal4.PosPrev = num17;
                    optimal4.MakeAsChar();
                    flag = true;
                }
                price = price2 + _isMatch[(state.Index << 4) + num5].GetPrice1();
                num6 = price + _isRep[state.Index].GetPrice1();
                if (indexByte2 == indexByte && (optimal4.PosPrev >= num17 || optimal4.BackPrev != 0))
                {
                    uint num21 = num6 + GetRepLen1Price(state, num5);
                    if (num21 <= optimal4.Price)
                    {
                        optimal4.Price = num21;
                        optimal4.PosPrev = num17;
                        optimal4.MakeAsShortRep();
                        flag = true;
                    }
                }
                uint val = _matchFinder.GetNumAvailableBytes() + 1;
                val = Math.Min(4095 - num17, val);
                num = val;
                if (num < 2)
                {
                    continue;
                }
                if (num > _numFastBytes)
                {
                    num = _numFastBytes;
                }
                if (!flag && indexByte2 != indexByte)
                {
                    uint limit = Math.Min(val - 1, _numFastBytes);
                    uint matchLen = _matchFinder.GetMatchLen(0, reps[0], limit);
                    if (matchLen >= 2)
                    {
                        Base.State state2 = state;
                        state2.UpdateChar();
                        uint num22 = (position + 1) & _posStateMask;
                        uint num23 = num20 + _isMatch[(state2.Index << 4) + num22].GetPrice1() + _isRep[state2.Index].GetPrice1();
                        uint num24 = num17 + 1 + matchLen;
                        while (num8 < num24)
                        {
                            _optimum[++num8].Price = 268435455u;
                        }
                        uint num25 = num23 + GetRepPrice(0u, matchLen, state2, num22);
                        Optimal optimal5 = _optimum[num24];
                        if (num25 < optimal5.Price)
                        {
                            optimal5.Price = num25;
                            optimal5.PosPrev = num17 + 1;
                            optimal5.BackPrev = 0u;
                            optimal5.Prev1IsChar = true;
                            optimal5.Prev2 = false;
                        }
                    }
                }
                uint num26 = 2u;
                for (uint num27 = 0u; num27 < 4; num27++)
                {
                    uint num28 = _matchFinder.GetMatchLen(-1, reps[num27], num);
                    if (num28 < 2)
                    {
                        continue;
                    }
                    uint num29 = num28;
                    while (true)
                    {
                        if (num8 < num17 + num28)
                        {
                            _optimum[++num8].Price = 268435455u;
                            continue;
                        }
                        uint num30 = num6 + GetRepPrice(num27, num28, state, num5);
                        Optimal optimal6 = _optimum[num17 + num28];
                        if (num30 < optimal6.Price)
                        {
                            optimal6.Price = num30;
                            optimal6.PosPrev = num17;
                            optimal6.BackPrev = num27;
                            optimal6.Prev1IsChar = false;
                        }
                        if (--num28 < 2)
                        {
                            break;
                        }
                    }
                    num28 = num29;
                    if (num27 == 0)
                    {
                        num26 = num28 + 1;
                    }
                    if (num28 >= val)
                    {
                        continue;
                    }
                    uint limit2 = Math.Min(val - 1 - num28, _numFastBytes);
                    uint matchLen2 = _matchFinder.GetMatchLen((int)num28, reps[num27], limit2);
                    if (matchLen2 >= 2)
                    {
                        Base.State state3 = state;
                        state3.UpdateRep();
                        uint num31 = (position + num28) & _posStateMask;
                        uint num32 = num6 + GetRepPrice(num27, num28, state, num5) + _isMatch[(state3.Index << 4) + num31].GetPrice0() + _literalEncoder.GetSubCoder(position + num28, _matchFinder.GetIndexByte((int)(num28 - 1 - 1))).GetPrice(matchMode: true, _matchFinder.GetIndexByte((int)(num28 - 1 - (reps[num27] + 1))), _matchFinder.GetIndexByte((int)(num28 - 1)));
                        state3.UpdateChar();
                        num31 = (position + num28 + 1) & _posStateMask;
                        uint num33 = num32 + _isMatch[(state3.Index << 4) + num31].GetPrice1();
                        uint num34 = num33 + _isRep[state3.Index].GetPrice1();
                        uint num35 = num28 + 1 + matchLen2;
                        while (num8 < num17 + num35)
                        {
                            _optimum[++num8].Price = 268435455u;
                        }
                        uint num36 = num34 + GetRepPrice(0u, matchLen2, state3, num31);
                        Optimal optimal7 = _optimum[num17 + num35];
                        if (num36 < optimal7.Price)
                        {
                            optimal7.Price = num36;
                            optimal7.PosPrev = num17 + num28 + 1;
                            optimal7.BackPrev = 0u;
                            optimal7.Prev1IsChar = true;
                            optimal7.Prev2 = true;
                            optimal7.PosPrev2 = num17;
                            optimal7.BackPrev2 = num27;
                        }
                    }
                }
                if (lenRes2 > num)
                {
                    lenRes2 = num;
                    for (numDistancePairs = 0u; lenRes2 > _matchDistances[numDistancePairs]; numDistancePairs += 2)
                    {
                    }
                    _matchDistances[numDistancePairs] = lenRes2;
                    numDistancePairs += 2;
                }
                if (lenRes2 < num26)
                {
                    continue;
                }
                num13 = price + _isRep[state.Index].GetPrice0();
                while (num8 < num17 + lenRes2)
                {
                    _optimum[++num8].Price = 268435455u;
                }
                uint num37;
                for (num37 = 0u; num26 > _matchDistances[num37]; num37 += 2)
                {
                }
                uint num38 = num26;
                while (true)
                {
                    uint num39 = _matchDistances[num37 + 1];
                    uint num40 = num13 + GetPosLenPrice(num39, num38, num5);
                    Optimal optimal8 = _optimum[num17 + num38];
                    if (num40 < optimal8.Price)
                    {
                        optimal8.Price = num40;
                        optimal8.PosPrev = num17;
                        optimal8.BackPrev = num39 + 4;
                        optimal8.Prev1IsChar = false;
                    }
                    if (num38 == _matchDistances[num37])
                    {
                        if (num38 < val)
                        {
                            uint limit3 = Math.Min(val - 1 - num38, _numFastBytes);
                            uint matchLen3 = _matchFinder.GetMatchLen((int)num38, num39, limit3);
                            if (matchLen3 >= 2)
                            {
                                Base.State state4 = state;
                                state4.UpdateMatch();
                                uint num41 = (position + num38) & _posStateMask;
                                uint num42 = num40 + _isMatch[(state4.Index << 4) + num41].GetPrice0() + _literalEncoder.GetSubCoder(position + num38, _matchFinder.GetIndexByte((int)(num38 - 1 - 1))).GetPrice(matchMode: true, _matchFinder.GetIndexByte((int)(num38 - (num39 + 1) - 1)), _matchFinder.GetIndexByte((int)(num38 - 1)));
                                state4.UpdateChar();
                                num41 = (position + num38 + 1) & _posStateMask;
                                uint num43 = num42 + _isMatch[(state4.Index << 4) + num41].GetPrice1();
                                uint num44 = num43 + _isRep[state4.Index].GetPrice1();
                                uint num45 = num38 + 1 + matchLen3;
                                while (num8 < num17 + num45)
                                {
                                    _optimum[++num8].Price = 268435455u;
                                }
                                num40 = num44 + GetRepPrice(0u, matchLen3, state4, num41);
                                optimal8 = _optimum[num17 + num45];
                                if (num40 < optimal8.Price)
                                {
                                    optimal8.Price = num40;
                                    optimal8.PosPrev = num17 + num38 + 1;
                                    optimal8.BackPrev = 0u;
                                    optimal8.Prev1IsChar = true;
                                    optimal8.Prev2 = true;
                                    optimal8.PosPrev2 = num17;
                                    optimal8.BackPrev2 = num39 + 4;
                                }
                            }
                        }
                        num37 += 2;
                        if (num37 == numDistancePairs)
                        {
                            break;
                        }
                    }
                    num38++;
                }
            }
            _numDistancePairs = numDistancePairs;
            _longestMatchLength = lenRes2;
            _longestMatchWasFound = true;
            return Backward(out backRes, num17);
        }

        private bool ChangePair(uint smallDist, uint bigDist)
        {
            if (smallDist < 33554432)
            {
                return bigDist >= smallDist << 7;
            }
            return false;
        }

        private void WriteEndMarker(uint posState)
        {
            if (_writeEndMark)
            {
                _isMatch[(_state.Index << 4) + posState].Encode(_rangeEncoder, 1u);
                _isRep[_state.Index].Encode(_rangeEncoder, 0u);
                _state.UpdateMatch();
                uint num = 2u;
                _lenEncoder.Encode(_rangeEncoder, num - 2, posState);
                uint symbol = 63u;
                uint lenToPosState = Base.GetLenToPosState(num);
                _posSlotEncoder[lenToPosState].Encode(_rangeEncoder, symbol);
                int num2 = 30;
                uint num3 = (uint)((1 << num2) - 1);
                _rangeEncoder.EncodeDirectBits(num3 >> 4, num2 - 4);
                _posAlignEncoder.ReverseEncode(_rangeEncoder, num3 & 0xFu);
            }
        }

        private void Flush(uint nowPos)
        {
            ReleaseMFStream();
            WriteEndMarker(nowPos & _posStateMask);
            _rangeEncoder.FlushData();
            _rangeEncoder.FlushStream();
        }

        public void CodeOneBlock(out long inSize, out long outSize, out bool finished)
        {
            inSize = 0L;
            outSize = 0L;
            finished = true;
            if (_inStream != null)
            {
                _matchFinder.SetStream(_inStream);
                _matchFinder.Init();
                _needReleaseMFStream = true;
                _inStream = null;
                if (_trainSize != 0)
                {
                    _matchFinder.Skip(_trainSize);
                }
            }
            if (_finished)
            {
                return;
            }
            _finished = true;
            long num = nowPos64;
            if (nowPos64 == 0L)
            {
                if (_matchFinder.GetNumAvailableBytes() == 0)
                {
                    Flush((uint)nowPos64);
                    return;
                }
                ReadMatchDistances(out var _, out var _);
                uint num2 = (uint)(int)nowPos64 & _posStateMask;
                _isMatch[(_state.Index << 4) + num2].Encode(_rangeEncoder, 0u);
                _state.UpdateChar();
                byte indexByte = _matchFinder.GetIndexByte((int)(0 - _additionalOffset));
                _literalEncoder.GetSubCoder((uint)nowPos64, _previousByte).Encode(_rangeEncoder, indexByte);
                _previousByte = indexByte;
                _additionalOffset--;
                nowPos64++;
            }
            if (_matchFinder.GetNumAvailableBytes() == 0)
            {
                Flush((uint)nowPos64);
                return;
            }
            while (true)
            {
                uint backRes;
                uint optimum = GetOptimum((uint)nowPos64, out backRes);
                uint num3 = (uint)(int)nowPos64 & _posStateMask;
                uint num4 = (_state.Index << 4) + num3;
                if (optimum == 1 && backRes == uint.MaxValue)
                {
                    _isMatch[num4].Encode(_rangeEncoder, 0u);
                    byte indexByte2 = _matchFinder.GetIndexByte((int)(0 - _additionalOffset));
                    LiteralEncoder.Encoder2 subCoder = _literalEncoder.GetSubCoder((uint)nowPos64, _previousByte);
                    if (!_state.IsCharState())
                    {
                        byte indexByte3 = _matchFinder.GetIndexByte((int)(0 - _repDistances[0] - 1 - _additionalOffset));
                        subCoder.EncodeMatched(_rangeEncoder, indexByte3, indexByte2);
                    }
                    else
                    {
                        subCoder.Encode(_rangeEncoder, indexByte2);
                    }
                    _previousByte = indexByte2;
                    _state.UpdateChar();
                }
                else
                {
                    _isMatch[num4].Encode(_rangeEncoder, 1u);
                    if (backRes < 4)
                    {
                        _isRep[_state.Index].Encode(_rangeEncoder, 1u);
                        if (backRes == 0)
                        {
                            _isRepG0[_state.Index].Encode(_rangeEncoder, 0u);
                            if (optimum == 1)
                            {
                                _isRep0Long[num4].Encode(_rangeEncoder, 0u);
                            }
                            else
                            {
                                _isRep0Long[num4].Encode(_rangeEncoder, 1u);
                            }
                        }
                        else
                        {
                            _isRepG0[_state.Index].Encode(_rangeEncoder, 1u);
                            if (backRes == 1)
                            {
                                _isRepG1[_state.Index].Encode(_rangeEncoder, 0u);
                            }
                            else
                            {
                                _isRepG1[_state.Index].Encode(_rangeEncoder, 1u);
                                _isRepG2[_state.Index].Encode(_rangeEncoder, backRes - 2);
                            }
                        }
                        if (optimum == 1)
                        {
                            _state.UpdateShortRep();
                        }
                        else
                        {
                            _repMatchLenEncoder.Encode(_rangeEncoder, optimum - 2, num3);
                            _state.UpdateRep();
                        }
                        uint num5 = _repDistances[backRes];
                        if (backRes != 0)
                        {
                            for (uint num6 = backRes; num6 >= 1; num6--)
                            {
                                _repDistances[num6] = _repDistances[num6 - 1];
                            }
                            _repDistances[0] = num5;
                        }
                    }
                    else
                    {
                        _isRep[_state.Index].Encode(_rangeEncoder, 0u);
                        _state.UpdateMatch();
                        _lenEncoder.Encode(_rangeEncoder, optimum - 2, num3);
                        backRes -= 4;
                        uint posSlot = GetPosSlot(backRes);
                        uint lenToPosState = Base.GetLenToPosState(optimum);
                        _posSlotEncoder[lenToPosState].Encode(_rangeEncoder, posSlot);
                        if (posSlot >= 4)
                        {
                            int num7 = (int)((posSlot >> 1) - 1);
                            uint num8 = (2 | (posSlot & 1)) << num7;
                            uint num9 = backRes - num8;
                            if (posSlot < 14)
                            {
                                BitTreeEncoder.ReverseEncode(_posEncoders, num8 - posSlot - 1, _rangeEncoder, num7, num9);
                            }
                            else
                            {
                                _rangeEncoder.EncodeDirectBits(num9 >> 4, num7 - 4);
                                _posAlignEncoder.ReverseEncode(_rangeEncoder, num9 & 0xFu);
                                _alignPriceCount++;
                            }
                        }
                        uint num10 = backRes;
                        for (uint num11 = 3u; num11 >= 1; num11--)
                        {
                            _repDistances[num11] = _repDistances[num11 - 1];
                        }
                        _repDistances[0] = num10;
                        _matchPriceCount++;
                    }
                    _previousByte = _matchFinder.GetIndexByte((int)(optimum - 1 - _additionalOffset));
                }
                _additionalOffset -= optimum;
                nowPos64 += optimum;
                if (_additionalOffset == 0)
                {
                    if (_matchPriceCount >= 128)
                    {
                        FillDistancesPrices();
                    }
                    if (_alignPriceCount >= 16)
                    {
                        FillAlignPrices();
                    }
                    inSize = nowPos64;
                    outSize = _rangeEncoder.GetProcessedSizeAdd();
                    if (_matchFinder.GetNumAvailableBytes() == 0)
                    {
                        Flush((uint)nowPos64);
                        return;
                    }
                    if (nowPos64 - num >= 4096)
                    {
                        break;
                    }
                }
            }
            _finished = false;
            finished = false;
        }

        private void ReleaseMFStream()
        {
            if (_matchFinder != null && _needReleaseMFStream)
            {
                _matchFinder.ReleaseStream();
                _needReleaseMFStream = false;
            }
        }

        private void SetOutStream(Stream outStream)
        {
            _rangeEncoder.SetStream(outStream);
        }

        private void ReleaseOutStream()
        {
            _rangeEncoder.ReleaseStream();
        }

        private void ReleaseStreams()
        {
            ReleaseMFStream();
            ReleaseOutStream();
        }

        private void SetStreams(Stream inStream, Stream outStream, long inSize, long outSize)
        {
            _inStream = inStream;
            _finished = false;
            Create();
            SetOutStream(outStream);
            Init();
            FillDistancesPrices();
            FillAlignPrices();
            _lenEncoder.SetTableSize(_numFastBytes + 1 - 2);
            _lenEncoder.UpdateTables((uint)(1 << _posStateBits));
            _repMatchLenEncoder.SetTableSize(_numFastBytes + 1 - 2);
            _repMatchLenEncoder.UpdateTables((uint)(1 << _posStateBits));
            nowPos64 = 0L;
        }

        public void Code(Stream inStream, Stream outStream, long inSize, long outSize, ICodeProgress progress)
        {
            _needReleaseMFStream = false;
            try
            {
                SetStreams(inStream, outStream, inSize, outSize);
                while (true)
                {
                    _ = cancellationToken;
                    cancellationToken.ThrowIfCancellationRequested();
                    CodeOneBlock(out var inSize2, out var outSize2, out var finished);
                    if (finished)
                    {
                        break;
                    }
                    progress?.SetProgress(inSize2, outSize2);
                }
            }
            finally
            {
                ReleaseStreams();
            }
        }

        public void WriteCoderProperties(Stream outStream)
        {
            properties[0] = (byte)((_posStateBits * 5 + _numLiteralPosStateBits) * 9 + _numLiteralContextBits);
            for (int i = 0; i < 4; i++)
            {
                properties[1 + i] = (byte)((_dictionarySize >> 8 * i) & 0xFFu);
            }
            outStream.Write(properties, 0, 5);
        }

        private void FillDistancesPrices()
        {
            for (uint num = 4u; num < 128; num++)
            {
                uint posSlot = GetPosSlot(num);
                int num2 = (int)((posSlot >> 1) - 1);
                uint num3 = (2 | (posSlot & 1)) << num2;
                tempPrices[num] = BitTreeEncoder.ReverseGetPrice(_posEncoders, num3 - posSlot - 1, num2, num - num3);
            }
            for (uint num4 = 0u; num4 < 4; num4++)
            {
                BitTreeEncoder bitTreeEncoder = _posSlotEncoder[num4];
                uint num5 = num4 << 6;
                for (uint num6 = 0u; num6 < _distTableSize; num6++)
                {
                    _posSlotPrices[num5 + num6] = bitTreeEncoder.GetPrice(num6);
                }
                for (uint num6 = 14u; num6 < _distTableSize; num6++)
                {
                    _posSlotPrices[num5 + num6] += (num6 >> 1) - 1 - 4 << 6;
                }
                uint num7 = num4 * 128;
                uint num8;
                for (num8 = 0u; num8 < 4; num8++)
                {
                    _distancesPrices[num7 + num8] = _posSlotPrices[num5 + num8];
                }
                for (; num8 < 128; num8++)
                {
                    _distancesPrices[num7 + num8] = _posSlotPrices[num5 + GetPosSlot(num8)] + tempPrices[num8];
                }
            }
            _matchPriceCount = 0u;
        }

        private void FillAlignPrices()
        {
            for (uint num = 0u; num < 16; num++)
            {
                _alignPrices[num] = _posAlignEncoder.ReverseGetPrice(num);
            }
            _alignPriceCount = 0u;
        }

        private static int FindMatchFinder(string s)
        {
            for (int i = 0; i < kMatchFinderIDs.Length; i++)
            {
                if (s == kMatchFinderIDs[i])
                {
                    return i;
                }
            }
            return -1;
        }

        public void SetCoderProperties(CoderPropID[] propIDs, object[] properties)
        {
            for (uint num = 0u; num < properties.Length; num++)
            {
                object obj = properties[num];
                switch (propIDs[num])
                {
                    case CoderPropID.NumFastBytes:
                        if (!(obj is int num2))
                        {
                            throw new InvalidParamException();
                        }
                        if (num2 < 5 || (long)num2 > 273L)
                        {
                            throw new InvalidParamException();
                        }
                        _numFastBytes = (uint)num2;
                        break;
                    case CoderPropID.MatchFinder:
                        {
                            if (!(obj is string))
                            {
                                throw new InvalidParamException();
                            }
                            EMatchFinderType matchFinderType = _matchFinderType;
                            int num6 = FindMatchFinder(((string)obj).ToUpper());
                            if (num6 < 0)
                            {
                                throw new InvalidParamException();
                            }
                            _matchFinderType = (EMatchFinderType)num6;
                            if (_matchFinder != null && matchFinderType != _matchFinderType)
                            {
                                _dictionarySizePrev = uint.MaxValue;
                                _matchFinder = null;
                            }
                            break;
                        }
                    case CoderPropID.DictionarySize:
                        {
                            if (!(obj is int num7))
                            {
                                throw new InvalidParamException();
                            }
                            if ((long)num7 < 1L || (long)num7 > 1073741824L)
                            {
                                throw new InvalidParamException();
                            }
                            _dictionarySize = (uint)num7;
                            int i;
                            for (i = 0; (long)i < 30L && num7 > (uint)(1 << i); i++)
                            {
                            }
                            _distTableSize = (uint)(i * 2);
                            break;
                        }
                    case CoderPropID.PosStateBits:
                        if (!(obj is int num3))
                        {
                            throw new InvalidParamException();
                        }
                        if (num3 < 0 || (long)num3 > 4L)
                        {
                            throw new InvalidParamException();
                        }
                        _posStateBits = num3;
                        _posStateMask = (uint)((1 << _posStateBits) - 1);
                        break;
                    case CoderPropID.LitPosBits:
                        if (!(obj is int num5))
                        {
                            throw new InvalidParamException();
                        }
                        if (num5 < 0 || (long)num5 > 4L)
                        {
                            throw new InvalidParamException();
                        }
                        _numLiteralPosStateBits = num5;
                        break;
                    case CoderPropID.LitContextBits:
                        if (!(obj is int num4))
                        {
                            throw new InvalidParamException();
                        }
                        if (num4 < 0 || (long)num4 > 8L)
                        {
                            throw new InvalidParamException();
                        }
                        _numLiteralContextBits = num4;
                        break;
                    case CoderPropID.EndMarker:
                        if (!(obj is bool))
                        {
                            throw new InvalidParamException();
                        }
                        SetWriteEndMarkerMode((bool)obj);
                        break;
                    default:
                        throw new InvalidParamException();
                    case CoderPropID.Algorithm:
                        break;
                }
            }
        }

        public void SetTrainSize(uint trainSize)
        {
            _trainSize = trainSize;
        }
    }
}