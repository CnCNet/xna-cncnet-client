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
        /// <returns>Bitmap of map preview image, blank bitmap if not present.</returns>
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

            byte[] dataSrc;
            try
            {
                dataSrc = Convert.FromBase64String(sb.ToString());
            }
            catch (Exception)
            {
                Logger.Log("MapPreviewExtractor: " + baseFilename + " - [PreviewPack] is malformed, unable to extract preview.");
                return null;
            }

            byte[] dataDest = new byte[previewWidth * previewHeight * 3];

            string errorMsg = DecompressPreviewData(dataSrc, ref dataDest);

            if (errorMsg != null)
            {
                Logger.Log("MapPreviewExtractor: " + baseFilename + " - " + errorMsg);
                return null;
            }

            return CreateBitmapFromImageData(previewWidth, previewHeight, dataDest);
        }

        /// <summary>
        /// Decompresses map preview image data.
        /// </summary>
        /// <param name="dataSource">Array of compressed map preview image data.</param>
        /// <param name="dataDest">Array to write decompressed preview image data to.</param>
        /// <returns>Error message if something went wrong, otherwise null.</returns>
        private static string DecompressPreviewData(byte[] dataSource, ref byte[] dataDest)
        {
            try
            {
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
                        return "Preview data does not match preview size or the data is corrupted, unable to extract preview.";

                    LzoStream stream = new LzoStream(new MemoryStream(dataSource, readBytes, sizeCompressed), CompressionMode.Decompress);
                    byte[] uncompressedData = new byte[sizeUncompressed];
                    stream.Read(uncompressedData, 0, sizeUncompressed);
                    Array.Copy(uncompressedData, 0, dataDest, writtenBytes, sizeUncompressed);

                    readBytes += sizeCompressed;
                    writtenBytes += sizeUncompressed;
                }
            }
            catch (Exception e)
            {
                return "Error encountered decompressing preview data. Message: " + e.Message;
            }

            return null;
        }

        /// <summary>
        /// Creates a bitmap based on a provided dimensions and raw image data in BGR format.
        /// </summary>
        /// <param name="width">Width of the bitmap.</param>
        /// <param name="height">Height of the bitmap.</param>
        /// <param name="imageData">Raw image data in BGR format.</param>
        /// <returns>Bitmap based on the provided dimensions and raw image data, or null if length of image data does not match the provided dimensions.</returns>
        private static Bitmap CreateBitmapFromImageData(int width, int height, byte[] imageData)
        {
            if (imageData.Length != width * height * 3)
                return null;

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            IntPtr scan0 = bitmapData.Scan0;
            byte[] rgbData = new byte[Math.Abs(bitmapData.Stride) * bitmapData.Height];

            for (int i = 0; i < rgbData.Length; i+=3)
            {
                rgbData[i] = imageData[i + 2];
                rgbData[i+1] = imageData[i + 1];
                rgbData[i+2] = imageData[i];
            }

            Marshal.Copy(rgbData, 0, scan0, rgbData.Length);
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }
    }
}
