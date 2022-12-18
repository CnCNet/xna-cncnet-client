using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Statistics
{
    internal static class DataWriter
    {
        public static Task WriteIntAsync(this Stream stream, int value)
            => stream.WriteAsync(BitConverter.GetBytes(value), 0, sizeof(int));

        public static Task WriteLongAsync(this Stream stream, long value)
            => stream.WriteAsync(BitConverter.GetBytes(value), 0, sizeof(long));

        public static Task WriteBoolAsync(this Stream stream, bool value)
            => stream.WriteAsync(new[] { Convert.ToByte(value) }, 0, 1);

        public static Task WriteStringAsync(this Stream stream, string value, int reservedSpace, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.Unicode;

            byte[] writeBuffer = encoding.GetBytes(value);
            if (writeBuffer.Length != reservedSpace)
            {
                // If the name's byte presentation is not equal to reservedSpace,
                // let's resize the array
                byte[] temp = writeBuffer;
                writeBuffer = new byte[reservedSpace];
                for (int j = 0; j < temp.Length && j < writeBuffer.Length; j++)
                    writeBuffer[j] = temp[j];
            }

            return stream.WriteAsync(writeBuffer, 0, writeBuffer.Length);
        }
    }
}