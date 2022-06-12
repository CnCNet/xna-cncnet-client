using System.IO;
using SevenZip;

namespace SevenZip
{
    public interface ICoder
    {
        void Code(Stream inStream, Stream outStream, long inSize, long outSize, ICodeProgress progress);
    }
}