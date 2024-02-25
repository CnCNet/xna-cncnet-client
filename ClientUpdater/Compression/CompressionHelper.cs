// Copyright 2022-2024 CnCNet
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

namespace ClientUpdater.Compression;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SevenZip.Compression.LZMA;

/// <summary>
/// LZMA compression helper.
/// </summary>
public static class CompressionHelper
{
    /// <summary>
    /// Compress file using LZMA.
    /// </summary>
    /// <param name="inputFilename">Input file path.</param>
    /// <param name="outputFilename">Output file path.</param>
    public static async ValueTask CompressFileAsync(string inputFilename, string outputFilename, CancellationToken cancellationToken = default)
    {
        var encoder = new Encoder(cancellationToken);
        var inputStream = new FileStream(inputFilename, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);

        using (inputStream)
        {
            var outputStream = new FileStream(outputFilename, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);

            using (outputStream)
            {
                encoder.WriteCoderProperties(outputStream);
                await outputStream.WriteAsync(BitConverter.GetBytes(inputStream.Length).AsMemory(0, 8), cancellationToken).ConfigureAwait(false);
                encoder.Code(inputStream, outputStream, inputStream.Length, outputStream.Length, null);
            }
        }
    }

    /// <summary>
    /// Decompress file using LZMA.
    /// </summary>
    /// <param name="inputFilename">Input file path.</param>
    /// <param name="outputFilename">Output file path.</param>
    public static async ValueTask DecompressFileAsync(string inputFilename, string outputFilename, CancellationToken cancellationToken = default)
    {
        var decoder = new Decoder(cancellationToken);

        var inputStream = new FileStream(inputFilename, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);

        using (inputStream)
        {
            var outputStream = new FileStream(outputFilename, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);

            using (outputStream)
            {
                byte[] properties = new byte[5];
                byte[] fileLengthArray = new byte[sizeof(long)];

                await inputStream.ReadAsync(properties, cancellationToken).ConfigureAwait(false);
                await inputStream.ReadAsync(fileLengthArray, cancellationToken).ConfigureAwait(false);

                long fileLength = BitConverter.ToInt64(fileLengthArray, 0);

                decoder.SetDecoderProperties(properties);
                decoder.Code(inputStream, outputStream, inputStream.Length, fileLength, null);
            }
        }
    }
}