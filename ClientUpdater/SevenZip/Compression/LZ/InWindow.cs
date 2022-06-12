using System.IO;

namespace SevenZip.Compression.LZ
{
    public class InWindow
    {
        public byte[] _bufferBase;

        private Stream _stream;

        private uint _posLimit;

        private bool _streamEndWasReached;

        private uint _pointerToLastSafePosition;

        public uint _bufferOffset;

        public uint _blockSize;

        public uint _pos;

        private uint _keepSizeBefore;

        private uint _keepSizeAfter;

        public uint _streamPos;

        public void MoveBlock()
        {
            uint num = _bufferOffset + _pos - _keepSizeBefore;
            if (num != 0)
            {
                num--;
            }
            uint num2 = _bufferOffset + _streamPos - num;
            for (uint num3 = 0u; num3 < num2; num3++)
            {
                _bufferBase[num3] = _bufferBase[num + num3];
            }
            _bufferOffset -= num;
        }

        public virtual void ReadBlock()
        {
            if (_streamEndWasReached)
            {
                return;
            }
            while (true)
            {
                int num = (int)(0 - _bufferOffset + _blockSize - _streamPos);
                if (num == 0)
                {
                    return;
                }
                int num2 = _stream.Read(_bufferBase, (int)(_bufferOffset + _streamPos), num);
                if (num2 == 0)
                {
                    break;
                }
                _streamPos += (uint)num2;
                if (_streamPos >= _pos + _keepSizeAfter)
                {
                    _posLimit = _streamPos - _keepSizeAfter;
                }
            }
            _posLimit = _streamPos;
            uint num3 = _bufferOffset + _posLimit;
            if (num3 > _pointerToLastSafePosition)
            {
                _posLimit = _pointerToLastSafePosition - _bufferOffset;
            }
            _streamEndWasReached = true;
        }

        private void Free()
        {
            _bufferBase = null;
        }

        public void Create(uint keepSizeBefore, uint keepSizeAfter, uint keepSizeReserv)
        {
            _keepSizeBefore = keepSizeBefore;
            _keepSizeAfter = keepSizeAfter;
            uint num = keepSizeBefore + keepSizeAfter + keepSizeReserv;
            if (_bufferBase == null || _blockSize != num)
            {
                Free();
                _blockSize = num;
                _bufferBase = new byte[_blockSize];
            }
            _pointerToLastSafePosition = _blockSize - keepSizeAfter;
        }

        public void SetStream(Stream stream)
        {
            _stream = stream;
        }

        public void ReleaseStream()
        {
            _stream = null;
        }

        public void Init()
        {
            _bufferOffset = 0u;
            _pos = 0u;
            _streamPos = 0u;
            _streamEndWasReached = false;
            ReadBlock();
        }

        public void MovePos()
        {
            _pos++;
            if (_pos > _posLimit)
            {
                uint num = _bufferOffset + _pos;
                if (num > _pointerToLastSafePosition)
                {
                    MoveBlock();
                }
                ReadBlock();
            }
        }

        public byte GetIndexByte(int index)
        {
            return _bufferBase[_bufferOffset + _pos + index];
        }

        public uint GetMatchLen(int index, uint distance, uint limit)
        {
            if (_streamEndWasReached && _pos + index + limit > _streamPos)
            {
                limit = _streamPos - (uint)(int)(_pos + index);
            }
            distance++;
            uint num = _bufferOffset + _pos + (uint)index;
            uint num2;
            for (num2 = 0u; num2 < limit && _bufferBase[num + num2] == _bufferBase[num + num2 - distance]; num2++)
            {
            }
            return num2;
        }

        public uint GetNumAvailableBytes()
        {
            return _streamPos - _pos;
        }

        public void ReduceOffsets(int subValue)
        {
            _bufferOffset += (uint)subValue;
            _posLimit -= (uint)subValue;
            _pos -= (uint)subValue;
            _streamPos -= (uint)subValue;
        }
    }
}