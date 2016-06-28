using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ClientCore
{
    public static class Logger
    {
        private static readonly object Locker = new object();

        public static bool EnableLogging = true;
        public static string LogFileName = "cncnetclient.log";

        /// <summary>
        /// Writes data to the Launcher log file.
        /// </summary>
        /// <param name="data">The data to write to the logfile.</param>
        public static void Log(string data)
        {
            lock (Locker)
            {
                Console.WriteLine(data);

                if (EnableLogging)
                {
                    try
                    {
                        StreamWriter sw = new StreamWriter(ProgramConstants.gamepath + LogFileName, true);

                        DateTime now = DateTime.Now;

                        StringBuilder sb = new StringBuilder();
                        sb.Append(String.Format("{0,2:D2}", now.Hour));
                        sb.Append(":");
                        sb.Append(String.Format("{0,2:D2}", now.Minute));
                        sb.Append(":");
                        sb.Append(String.Format("{0,2:D2}", now.Second));
                        sb.Append(".");
                        sb.Append(String.Format("{0,3:D3}", now.Millisecond));
                        sb.Append("    ");
                        sb.Append(data);

                        sw.WriteLine(now.Day + ". " + now.Month + ". " + sb.ToString() );

                        sw.Close();
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
