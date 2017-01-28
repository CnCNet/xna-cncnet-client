using System;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Collections.Generic;

namespace DTAClient
{
    static class Program
    {
        static Program()
        {
            // Set up DLL load paths as early as possible
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#if !DEBUG
            Environment.CurrentDirectory = Directory.GetParent(Application.StartupPath).FullName;
#else
            Environment.CurrentDirectory = Application.StartupPath;
#endif
        }

        static List<string> EXTERNAL_LIBRARIES = new List<string>()
        {
            "ClientGUI",
            "ClientCore",
            "DTAUpdater",
            "DTAConfig",
            "MonoGame.Framework",
            "Rampastring.Tools",
            "Rampastring.XNAUI",
            "Ionic.Zip",
            "OpenTK",
            "NVorbis",
            "MapThumbnailExtractor"
        };

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
                    catch (TimeoutException)
                    {
                        return;
                    }

                    // Proceed to client startup
                    PreStartup.Initialize(args);
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

            /*/ We have different binaries depending on build platform, but for simplicity
             * the target projects (DTA, TI, MO, YR) supply them all in a single download.
             * To avoid DLL hell, we load the binaries from different directories
             * depending on the build platform. /*/
#if XNA && DEBUG
            string path = string.Format("{0}{1}Resources{1}XNABinaries{1}", Application.StartupPath, directorySeparatorChar);
#elif XNA
            string path = string.Format("{0}{1}XNABinaries{1}", Application.StartupPath, directorySeparatorChar);
#elif WINDOWSGL && DEBUG
            string path = string.Format("{0}{1}Resources{1}GLBinaries{1}", Application.StartupPath, directorySeparatorChar);
#elif WINDOWSGL
            string path = string.Format("{0}{1}GLBinaries{1}", Application.StartupPath, directorySeparatorChar);
#elif DEBUG
            string path = string.Format("{0}{1}Resources{1}Binaries{1}", Application.StartupPath, directorySeparatorChar);
#else
            string path = string.Format("{0}{1}Binaries{1}", Application.StartupPath, directorySeparatorChar);
#endif

            if (args.Name.StartsWith("SharpDX"))
            {
                string[] parts = args.Name.Split(',');
                byte[] data = File.ReadAllBytes(path + parts[0] + ".dll");
                return Assembly.Load(data);
            }

            string name = EXTERNAL_LIBRARIES.Find(dll => args.Name.StartsWith(dll));

            if (name != null)
            {
                byte[] data = File.ReadAllBytes(string.Format("{0}{1}.dll", path, name));
                return Assembly.Load(data);
            }
            
            return null;
        }
    }
}
