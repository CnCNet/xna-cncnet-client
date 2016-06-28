using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using ClientCore;
using ClientCore.CnCNet5;
using System.Threading;
using System.Drawing;
using Microsoft.Win32;
using Rampastring.Tools;

namespace ClientGUI.FunkyIran
{
    class MapPreviewGenerator
    {
        public static string MapRendererName = "CNCMaps.Renderer.exe";

        public static string Get_Map_Generator_Path()
        {
            return Directory.GetCurrentDirectory() + "\\Map Renderer\\";
        }

        /*        public static int Start_Map_Renderer(string[] args)
                {
                    var engineSettings = new EngineSettings();
                    engineSettings.ConfigureFromArgs(args);
                    int retVal = engineSettings.Execute();
                    return retVal;
                }*/

        public static void Start_Map_Generator(Map map)
        {
            Logger.Log("Starting map generator.");

            string inputMap = Directory.GetCurrentDirectory() + "\\" + map.Path;
            string outFile = Path.GetFileNameWithoutExtension(inputMap);


            /*            string args =  "-p -c=9 -Y -i \"" + inputMap + "\" -o \"" + outFile
                            + "\" -m \"" + Directory.GetCurrentDirectory() + "\"";
                        Start_Map_Renderer(args.Split(' ')); */

            var psi = new ProcessStartInfo(Get_Map_Generator_Path() + MapRendererName);
            psi.WorkingDirectory = Get_Map_Generator_Path();
            psi.Arguments = "-r -p -c=9 -Y -i \"" + inputMap + "\" -o \"" + outFile
                + "\" -m \"" + Directory.GetCurrentDirectory() + "\""; // -Y is 'force YR' flag, need to add RA2 support later?
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;

            var MapRendererProc = Process.Start(psi);
            try
            {
                MapRendererProc.PriorityClass = ProcessPriorityClass.Idle;
            }
            catch { }

            MapRendererProc.WaitForExit();

            string outFile2 = Path.ChangeExtension(inputMap, ".png");

            if (!File.Exists(outFile2)) return;

            string dummyFile = Path.ChangeExtension(inputMap, ".dummy");

            if (File.Exists(dummyFile)) File.Delete(dummyFile);

            File.Move(outFile2, dummyFile);
            Resize(dummyFile, outFile2, 0.15F);
            File.Delete(dummyFile);


            //            map.PreviewPath = Path.GetDirectoryName(map.Path) + "\\" + outFile + ".png";
            map.ExtractCustomPreview = false;
            SharedLogic.UpdateWaypointCoords(map);
        }

        public static bool Check_Map(Map map)
        {
            if (!map.ExtractCustomPreview) return true;
            if (!(map.PreviewPath == "" || map.PreviewPath == null)) return true;
            if (File.Exists(Path.ChangeExtension(map.Path, ".png"))) return true;

            // No preview, so generate one, first check if map renderer files are there

            if (!Map_Renderer_Files_Exist())
            {
                Logger.Log("WARNING: CnCMaps Renderer files not found.");
                return false; // TODO ADD FATAL ERROR CHECK
            }

            Start_Map_Generator(map);
            return true;
        }

        public static void Resize(string imageFile, string outputFile, double scaleFactor)
        {
            using (var srcImage = Image.FromFile(imageFile))
            {
                var newWidth = (int)(srcImage.Width * scaleFactor);
                var newHeight = (int)(srcImage.Height * scaleFactor);
                using (var newImage = new Bitmap(newWidth, newHeight))
                using (var graphics = Graphics.FromImage(newImage))
                {
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    graphics.DrawImage(srcImage, new Rectangle(0, 0, newWidth, newHeight));
                    newImage.Save(outputFile);
                }
            }
        }

        public static bool Map_Renderer_Files_Exist()
        {
            if (!File.Exists(Get_Map_Generator_Path() + MapRendererName)) return false;
            if (!File.Exists(Get_Map_Generator_Path() + "NLog.dll")) return false;
            if (!File.Exists(Get_Map_Generator_Path() + "CNCMaps.Engine.dll")) return false;
            if (!File.Exists(Get_Map_Generator_Path() + "CNCMaps.FileFormats.dll")) return false;
            if (!File.Exists(Get_Map_Generator_Path() + "CNCMaps.Shared.dll")) return false;
            if (!File.Exists(Get_Map_Generator_Path() + "OpenTK.dll")) return false;

            return true;
        }



        public static void Generator_Thread()
        {
            int i = 0;
            int count = 0;

            while (true)
            {
                if (i < CnCNetData.MapList.Count)
                {
                    if (!Check_Map(CnCNetData.MapList[i]))
                        return;
                    ++i;
                }
                else
                {
                    i = 0;
                }
                count += 1;

                if (count > 5)
                {
                    count = 0;
                    Thread.Sleep(100);
                }
            }
        }

        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/hh925568%28v=vs.110%29.aspx
        /// </summary>
        public static bool IsNet4Installed()
        {
            try
            {
                RegistryKey ndpKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4.0\\Client");

                string installValue = ndpKey.GetValue("Install").ToString();

                if (installValue == "1")
                    return true;
            }
            catch (Exception ex)
            {
                Logger.Log(".NET v4 installation check failed, message: " + ex.Message);
            }

            return false;
        }
    }
}
