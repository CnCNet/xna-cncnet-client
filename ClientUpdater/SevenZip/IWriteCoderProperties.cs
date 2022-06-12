using System.IO;

namespace SevenZip
{
    public interface IWriteCoderProperties
    {
        void WriteCoderProperties(Stream outStream);
    }
}