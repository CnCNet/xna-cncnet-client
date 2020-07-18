using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using ClientCore;
using Rampastring.Tools;

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

            if (!DecompressPreviewData(dataSrc, dataDest))
            {
                Logger.Log("MapPreviewExtractor: " + baseFilename + " - Preview data does not match preview size or the data is corrupted, unable to extract preview.");
                return null;
            }

            return CreateBitmapFromImageData(previewWidth, previewHeight, dataDest);
        }

        /// <summary>
        /// Decompresses map preview image data.
        /// </summary>
        /// <param name="dataSource">Array of compressed map preview image data.</param>
        /// <param name="dataDest">Array to write decompressed preview image data to.</param>
        /// <returns>True if successfully decompressed all of the data, otherwise false.</returns>
        private static unsafe bool DecompressPreviewData(byte[] dataSource, byte[] dataDest)
        {
            fixed (byte* pRead = dataSource, pWrite = dataDest)
            {
                byte* read = pRead, write = pWrite;
                byte* writeEnd = write + dataDest.Length;
                int readBytes = 0;
                int writtenBytes = 0;

                while (write < writeEnd)
                {
                    ushort sizeCompressed = *(ushort*)read;
                    read += 2;
                    uint sizeUncompressed = *(ushort*)read;
                    read += 2;
                    readBytes += 4;

                    if (readBytes + sizeCompressed > dataSource.Length ||
                        writtenBytes + sizeUncompressed > dataDest.Length)
                        return false;

                    if (sizeCompressed == 0 || sizeUncompressed == 0)
                        break;

                    MiniLZO.Decompress(read, sizeCompressed, write, ref sizeUncompressed);

                    read += sizeCompressed;
                    write += sizeUncompressed;
                    readBytes += sizeCompressed;
                    writtenBytes += (int)sizeUncompressed;
                }
            }
            return true;
        }

        /// <summary>
        /// Creates a bitmap based on a provided dimensions and raw image data in BGR format.
        /// </summary>
        /// <param name="width">Width of the bitmap.</param>
        /// <param name="height">Height of the bitmap.</param>
        /// <param name="imageData">Raw image data in BGR format.</param>
        /// <returns>Bitmap based on the provided dimensions and raw image data, or null if length of image data does not match the provided dimensions.</returns>
        private static unsafe Bitmap CreateBitmapFromImageData(int width, int height, byte[] imageData)
        {
            if (imageData.Length != width * height * 3)
                return null;

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            byte* scan0 = (byte*)bitmapData.Scan0.ToPointer();
            int c = 0;

            for (int i = 0; i < bitmapData.Height; ++i)
            {
                for (int j = 0; j < bitmapData.Width; ++j)
                {
                    byte* data = scan0 + i * bitmapData.Stride + j * 24 / 8;

                    data[0] = imageData[c + 2];
                    data[1] = imageData[c + 1];
                    data[2] = imageData[c];
                    c += 3;
                }
            }

            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }
    }
}
