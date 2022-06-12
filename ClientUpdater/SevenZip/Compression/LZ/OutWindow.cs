using System.IO;

namespace SevenZip.Compression.LZ
{
    public class OutWindow
    {
        private byte[] _buffer;

        private uint _pos;

        private uint _windowSize;

        private uint _streamPos;

        private Stream _stream;

        public uint TrainSize;

        public void Create(uint windowSize)
        {
            if (_windowSize != windowSize)
            {
                _buffer = new byte[windowSize];
            }
            _windowSize = windowSize;
            _pos = 0u;
            _streamPos = 0u;
        }

        public void Init(Stream stream, bool solid)
        {
            ReleaseStream();
            _stream = stream;
            if (!solid)
            {
                _streamPos = 0u;
                _pos = 0u;
                TrainSize = 0u;
            }
        }

        public bool Train(Stream stream)
        {
            long length = stream.Length;
            uint num = (TrainSize = (uint)((length < _windowSize) ? length : _windowSize));
            stream.Position = length - num;
            _streamPos = (_pos = 0u);
            while (num != 0)
            {
                uint num2 = _windowSize - _pos;
                if (num < num2)
                {
                    num2 = num;
                }
                int num3 = stream.Read(_buffer, (int)_pos, (int)num2);
                if (num3 == 0)
                {
                    return false;
                }
                num -= (uint)num3;
                _pos += (uint)num3;
                _streamPos += (uint)num3;
                if (_pos == _windowSize)
                {
                    _streamPos = (_pos = 0u);
                }
            }
            return true;
        }

        public void ReleaseStream()
        {
            Flush();
            _stream = null;
        }

        public void Flush()
        {
            uint num = _pos - _streamPos;
            if (num != 0)
            {
                _stream.Write(_buffer, (int)_streamPos, (int)num);
                if (_pos >= _windowSize)
                {
                    _pos = 0u;
                }
                _streamPos = _pos;
            }
        }

        public void CopyBlock(uint distance, uint len)
        {
            uint num = _pos - distance - 1;
            if (num >= _windowSize)
            {
                num += _windowSize;
            }
            while (len != 0)
            {
                if (num >= _windowSize)
                {
                    num = 0u;
                }
                _buffer[_pos++] = _buffer[num++];
                if (_pos >= _windowSize)
                {
                    Flush();
                }
                len--;
            }
        }

        public void PutByte(byte b)
        {
            _buffer[_pos++] = b;
            if (_pos >= _windowSize)
            {
                Flush();
            }
        }

        public byte GetByte(uint distance)
        {
            uint num = _pos - distance - 1;
            if (num >= _windowSize)
            {
                num += _windowSize;
            }
            return _buffer[num];
        }
    }
}