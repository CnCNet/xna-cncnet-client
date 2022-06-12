using System;
using System.IO;
using System.Threading;
using SevenZip.Compression.LZMA;

namespace ClientUpdater.Compression
{
    public static class CompressionHelper
    {
        public static void CompressFile(string inputFilename, string outputFilename, CancellationToken cancellationToken = default(CancellationToken))
        {
            Encoder encoder = new Encoder(cancellationToken);
            using FileStream fileStream2 = File.OpenRead(inputFilename);
            using FileStream fileStream = File.Create(outputFilename);
            encoder.WriteCoderProperties(fileStream);
            fileStream.Write(BitConverter.GetBytes(fileStream2.Length), 0, 8);
            encoder.Code(fileStream2, fileStream, fileStream2.Length, fileStream.Length, null);
        }

        public static void DecompressFile(string inputFilename, string outputFilename, CancellationToken cancellationToken = default(CancellationToken))
        {
            Decoder decoder = new Decoder(cancellationToken);
            using FileStream fileStream = File.OpenRead(inputFilename);
            using FileStream outStream = File.Create(outputFilename);
            byte[] array = new byte[5];
            fileStream.Read(array, 0, array.Length);
            byte[] array2 = new byte[8];
            fileStream.Read(array2, 0, array2.Length);
            long outSize = BitConverter.ToInt64(array2, 0);
            decoder.SetDecoderProperties(array);
            decoder.Code(fileStream, outStream, fileStream.Length, outSize, null);
        }
    }
}