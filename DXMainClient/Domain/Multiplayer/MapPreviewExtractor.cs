using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using ClientCore;
using Rampastring.Tools;
using lzo.net;

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
        public static Bitmap ExtractMapPreview(IniFile mapIni)
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
            catch (Exception)
            {
                Logger.Log("MapPreviewExtractor: " + baseFilename + " - [PreviewPack] is malformed, unable to extract preview.");
                return null;
            }

            byte[] dataDest = DecompressPreviewData(dataSource, previewWidth * previewHeight * 3, out string errorMessage);

            if (errorMessage != null)
            {
                Logger.Log("MapPreviewExtractor: " + baseFilename + " - " + errorMessage);
                return null;
            }

            Bitmap bitmap = CreatePreviewBitmapFromImageData(previewWidth, previewHeight, dataDest, out errorMessage);

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
        /// <param name="errorMessage">Will be set to error message if something went wrong, otherwise null.</param>
        /// <returns>Array of decompressed preview image data if successfully decompressed, otherwise null.</returns>
        private static byte[] DecompressPreviewData(byte[] dataSource, int decompressedDataSize, out string errorMessage)
        {
            try
            {
                byte[] dataDest = new byte[decompressedDataSize];
                int readBytes = 0, writtenBytes = 0;

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
                        errorMessage = "Preview data does not match preview size or the data is corrupted, unable to extract preview.";
                        return null;
                    }

                    LzoStream stream = new LzoStream(new MemoryStream(dataSource, readBytes, sizeCompressed), CompressionMode.Decompress);
                    stream.Read(dataDest, writtenBytes, sizeUncompressed);
                    readBytes += sizeCompressed;
                    writtenBytes += sizeUncompressed;
                }

                errorMessage = null;
                return dataDest;
            }
            catch (Exception e)
            {
                errorMessage = "Error encountered decompressing preview data. Message: " + e.Message;
                return null;
            }
        }

        /// <summary>
        /// Creates a preview bitmap based on a provided dimensions and raw image pixel data in 24-bit RGB format.
        /// </summary>
        /// <param name="width">Width of the bitmap.</param>
        /// <param name="height">Height of the bitmap.</param>
        /// <param name="imageData">Raw image pixel data in 24-bit RGB format.</param>
        /// <param name="errorMessage">Will be set to error message if something went wrong, otherwise null.</param>
        /// <returns>Bitmap based on the provided dimensions and raw image data, or null if length of image data does not match the provided dimensions or if something went wrong.</returns>
        private static Bitmap CreatePreviewBitmapFromImageData(int width, int height, byte[] imageData, out string errorMessage)
        {
            if (imageData.Length != width * height * 3)
            {
                errorMessage = "Provided preview image dimensions do not match preview image data length.";
                return null;
            }

            try
            {
                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                IntPtr scan0 = bitmapData.Scan0;
                int strideWidth = Math.Abs(bitmapData.Stride);
                int numSkipBytes = strideWidth - bitmapData.Width * 3;
                byte[] bitmapPixelData = new byte[strideWidth * bitmapData.Height];
                int writtenBytes = 0;
                int readBytes = 0;

                for (int h = 0; h < bitmapData.Height; h++)
                {
                    for (int w = 0; w < bitmapData.Width; w++)
                    {
                        // GDI+ bitmap raw pixel data is in BGR format, red & blue values need to be flipped around for each pixel.
                        bitmapPixelData[writtenBytes] = imageData[readBytes + 2];
                        bitmapPixelData[writtenBytes + 1] = imageData[readBytes + 1];
                        bitmapPixelData[writtenBytes + 2] = imageData[readBytes];
                        writtenBytes += 3;
                        readBytes += 3;
                    }

                    // GDI+ bitmap stride / scan width has to be a multiple of 4, so the end of each stride / scanline can contain extra bytes
                    // in the bitmap raw pixel data that are not present in the image data and should be skipped when copying.
                    writtenBytes += numSkipBytes;
                }

                Marshal.Copy(bitmapPixelData, 0, scan0, bitmapPixelData.Length);
                bitmap.UnlockBits(bitmapData);
                errorMessage = null;
                return bitmap;

            }
            catch (Exception e)
            {
                errorMessage = "Error encountered creating preview bitmap. Message: " + e.Message;
                return null;
            }
        }
    }
}
