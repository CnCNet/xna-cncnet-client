using System;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace DTAClient
{
    static class Program
    {
        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Environment.CurrentDirectory = Application.StartupPath;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // We're a single instance application!
            // http://stackoverflow.com/questions/229565/what-is-a-good-pattern-for-using-a-global-mutex-in-c/229567

            string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(
                typeof(GuidAttribute), false).GetValue(0)).Value.ToString();

            // Global prefix means that the mutex is global to the machine
            string mutexId = string.Format("Global\\{{{0}}}", appGuid);

            bool createdNew;

            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);

            using (var mutex = new Mutex(false, mutexId, out createdNew, securitySettings))
            {
                var hasHandle = false;
                try
                {
                    try
                    {
                        hasHandle = mutex.WaitOne(8000, false);
                        if (hasHandle == false)
                            throw new TimeoutException("Timeout waiting for exclusive access");
                    }
                    catch (AbandonedMutexException)
                    {
                        hasHandle = true;
                    }

                    RealMain.ProxyMain(args);
                }
                finally
                {
                    if (hasHandle)
                        mutex.ReleaseMutex();
                }
            }
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            char directorySeparatorChar = Path.DirectorySeparatorChar;

            string path = Application.StartupPath + directorySeparatorChar + "Resources" + 
                directorySeparatorChar + "Binaries" + directorySeparatorChar;

            if (args.Name.StartsWith("ClientGUI"))
            {
                byte[] data = File.ReadAllBytes(path + "ClientGUI.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("ClientCore"))
            {
                byte[] data = File.ReadAllBytes(path + "ClientCore.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("DTAUpdater"))
            {
                byte[] data = File.ReadAllBytes(path + "DTAUpdater.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("DTAConfig"))
            {
                byte[] data = File.ReadAllBytes(path + "DTAConfig.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("SharpDX"))
            {
                string[] parts = args.Name.Split(',');
                byte[] data = File.ReadAllBytes(path + parts[0] + ".dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("MonoGame.Framework"))
            {
                byte[] data;

                if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                    Environment.OSVersion.VersionString.StartsWith("5."))
                {
                    data = File.ReadAllBytes(path + "MonoGame.Framework.WindowsGL.dll");
                }
                else
                {
                    data = File.ReadAllBytes(path + "MonoGame.Framework.dll");
                }

                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("Rampastring.Tools"))
            {
                byte[] data = File.ReadAllBytes(path + "Rampastring.Tools.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("Rampastring.XNAUI"))
            {
                byte[] data = File.ReadAllBytes(path + "Rampastring.XNAUI.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("Ionic.Zip"))
            {
                byte[] data = File.ReadAllBytes(path + "Ionic.Zip.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("OpenTK"))
            {
                byte[] data = File.ReadAllBytes(path + "OpenTK.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("NVorbis"))
            {
                byte[] data = File.ReadAllBytes(path + "NVorbis.dll");
                return Assembly.Load(data);
            }

            return null;
        }
    }
}
