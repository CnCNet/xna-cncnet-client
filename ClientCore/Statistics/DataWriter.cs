using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace ClientCore.Statistics
{
    internal static class DataWriter
    {
        public static void WriteInt(this Stream stream, int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
#if NET6_0_OR_GREATER
            stream.Write(buffer);
#else
            stream.Write(buffer.ToArray(), 0, sizeof(int));
#endif
        }

        public static void WriteLong(this Stream stream, long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
#if NET6_0_OR_GREATER
            stream.Write(buffer);
#else
            stream.Write(buffer.ToArray(), 0, sizeof(long));
#endif
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
