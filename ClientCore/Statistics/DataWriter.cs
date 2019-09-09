using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Statistics
{
    internal static class DataWriter
    {
        public static void WriteInt(this Stream stream, int value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(int));
        }

        public static void WriteLong(this Stream stream, long value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(long));
        }

        public static void WriteBool(this Stream stream, bool value)
        {
            stream.WriteByte(Convert.ToByte(value));
        }

        public static void WriteString(this Stream stream, string value, int reservedSpace, Encoding encoding = null)
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

            stream.Write(writeBuffer, 0, writeBuffer.Length);
        }
    }
}
