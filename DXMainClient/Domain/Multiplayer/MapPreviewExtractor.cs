using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;
using lzo.net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// A helper class for extracting preview images from maps.
    /// </summary>
    public static class MapPreviewExtractor
    {
        /// <summary>
        /// Extracts map preview image as a bitmap.
        /// </summary>
        /// <param name="mapIni">Map file.</param>
        /// <returns>Bitmap of map preview image, or null if preview could not be extracted.</returns>
        public static async Task<Image<Bgr24>> ExtractMapPreviewAsync(IniFile mapIni)
        {
            List<string> sectionKeys = mapIni.GetSectionKeys("PreviewPack");

            string baseFilename = mapIni.FileName.Replace(ProgramConstants.GamePath, "");

            if (sectionKeys == null || sectionKeys.Count == 0)
            {
                Logger.Log("MapPreviewExtractor: " + baseFilename + " - no [PreviewPack] exists, unable to extract preview.");
                return null;
            }

            if (mapIni.GetStringValue("PreviewPack", "1", string.Empty) ==
                "yAsAIAXQ5PDQ5PDQ6JQATAEE6PDQ4PDI4JgBTAFEAkgAJyAATAG0AydEAEABpAJIA0wBVA")
            {
                Logger.Log("MapPreviewExtractor: " + baseFilename + " - Hidden preview detected, not extracting preview.");
                return null;
            }

            string[] previewSizes = mapIni.GetStringValue("Preview", "Size", "").Split(',');
            int previewWidth = previewSizes.Length > 3 ? Conversions.IntFromString(previewSizes[2], -1) : -1;
            int previewHeight = previewSizes.Length > 3 ? Conversions.IntFromString(previewSizes[3], -1) : -1;

            if (previewWidth < 1 || previewHeight < 1)
            {
                Logger.Log("MapPreviewExtractor: " + baseFilename + " - [Preview] Size value is invalid, unable to extract preview.");
                return null;
            }

            StringBuilder sb = new StringBuilder();
            if (sectionKeys != null)
            {
                foreach (string key in sectionKeys)
                    sb.Append(mapIni.GetStringValue("PreviewPack", key, string.Empty));
            }

            byte[] dataSource;

            try
            {
                dataSource = Convert.FromBase64String(sb.ToString());
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "MapPreviewExtractor: " + baseFilename + " - [PreviewPack] is malformed, unable to extract preview.");
                return null;
            }

            (byte[] dataDest, string errorMessage) = await DecompressPreviewDataAsync(dataSource, previewWidth * previewHeight * 3).ConfigureAwait(false);

            if (errorMessage != null)
            {
                Logger.Log("MapPreviewExtractor: " + baseFilename + " - " + errorMessage);
                return null;
            }

            (Image<Bgr24> bitmap, errorMessage) = CreatePreviewBitmapFromImageData(previewWidth, previewHeight, dataDest);

            if (errorMessage != null)
            {
                Logger.Log("MapPreviewExtractor: " + baseFilename + " - " + errorMessage);
                return null;
            }

            return bitmap;
        }

        /// <summary>
        /// Decompresses map preview image data.
        /// </summary>
        /// <param name="dataSource">Array of compressed map preview image data.</param>
        /// <param name="decompressedDataSize">Size of decompressed preview image data.</param>
        /// <returns>Array of decompressed preview image data if successfully decompressed, otherwise null.</returns>
        private static async ValueTask<(byte[] Data, string ErrorMessage)> DecompressPreviewDataAsync(byte[] dataSource, int decompressedDataSize)
        {
            try
            {
                byte[] dataDest = new byte[decompressedDataSize];
                int readBytes = 0;
                int writtenBytes = 0;

                while (true)
                {
                    if (readBytes >= dataSource.Length)
                        break;

                    ushort sizeCompressed = BitConverter.ToUInt16(dataSource, readBytes);
                    readBytes += 2;
                    ushort sizeUncompressed = BitConverter.ToUInt16(dataSource, readBytes);
                    readBytes += 2;

                    if (sizeCompressed == 0 || sizeUncompressed == 0)
                        break;

                    if (readBytes + sizeCompressed > dataSource.Length ||
                        writtenBytes + sizeUncompressed > dataDest.Length)
                    {
                        return (null, "Preview data does not match preview size or the data is corrupted, unable to extract preview.");
                    }

                    var stream = new LzoStream(new MemoryStream(dataSource, readBytes, sizeCompressed), CompressionMode.Decompress);

                    await using (stream.ConfigureAwait(false))
                    {
                        await stream.ReadAsync(dataDest, writtenBytes, sizeUncompressed).ConfigureAwait(false);
                    }

                    readBytes += sizeCompressed;
                    writtenBytes += sizeUncompressed;
                }

                return (dataDest, null);
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "Error encountered decompressing preview data.");

                return (null, "Error encountered decompressing preview data. Message: " + ex.Message);
            }
        }

        /// <summary>
        /// Creates a preview bitmap based on a provided dimensions and raw image pixel data in 24-bit RGB format.
        /// </summary>
        /// <param name="width">Width of the bitmap.</param>
        /// <param name="height">Height of the bitmap.</param>
        /// <param name="imageData">Raw image pixel data in 24-bit RGB format.</param>
        /// <returns>Bitmap based on the provided dimensions and raw image data, or null if length of image data does not match the provided dimensions or if something went wrong.</returns>
        private static (Image<Bgr24> Image, string ErrorMessage) CreatePreviewBitmapFromImageData(int width, int height, byte[] imageData)
        {
            const int pixelFormatBitCount = 24;
            const int pixelFormatByteCount = pixelFormatBitCount / 8;

            if (imageData.Length != width * height * pixelFormatByteCount)
            {
                return (null, "Provided preview image dimensions do not match preview image data length.");
            }

            try
            {
                int strideWidth = (((width * pixelFormatBitCount) + 31) & ~31) >> 3;
                int numSkipBytes = strideWidth - (width * pixelFormatByteCount);
                byte[] bitmapPixelData = new byte[strideWidth * height];
                int writtenBytes = 0;
                int readBytes = 0;

                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        // GDI+ bitmap raw pixel data is in BGR format, red & blue values need to be flipped around for each pixel.
                        bitmapPixelData[writtenBytes] = imageData[readBytes + 2];
                        bitmapPixelData[writtenBytes + 1] = imageData[readBytes + 1];
                        bitmapPixelData[writtenBytes + 2] = imageData[readBytes];
                        writtenBytes += pixelFormatByteCount;
                        readBytes += pixelFormatByteCount;
                    }

                    // GDI+ bitmap stride / scan width has to be a multiple of 4, so the end of each stride / scanline can contain extra bytes
                    // in the bitmap raw pixel data that are not present in the image data and should be skipped when copying.
                    writtenBytes += numSkipBytes;
                }

                // https://github.com/SixLabors/ImageSharp/blob/main/tests/ImageSharp.Tests/TestUtilities/ReferenceCodecs/SystemDrawingBridge.cs
                var image = new Image<Bgr24>(width, height);
                Configuration configuration = image.GetConfiguration();
                Buffer2D<Bgr24> imageBuffer = image.Frames.RootFrame.PixelBuffer;
                using IMemoryOwner<Bgr24> workBuffer = Configuration.Default.MemoryAllocator.Allocate<Bgr24>(width);

                unsafe
                {
                    fixed (byte* sourcePtrBase = &bitmapPixelData[0])
                    {
                        fixed (Bgr24* destPtr = &workBuffer.Memory.Span[0])
                        {
                            for (int rowCount = 0; rowCount < height; rowCount++)
                            {
                                Span<Bgr24> row = imageBuffer.DangerousGetRowSpan(rowCount);
                                byte* sourcePtr = sourcePtrBase + (strideWidth * rowCount);

                                Buffer.MemoryCopy(sourcePtr, destPtr, strideWidth, strideWidth);
                                PixelOperations<Bgr24>.Instance.FromBgr24(configuration, workBuffer.Memory.Span[..width], row);
                            }
                        }
                    }
                }

                return (image, null);
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "Error encountered creating preview bitmap.");

                return (null, "Error encountered creating preview bitmap. Message: " + ex.Message);
            }
        }
    }
}