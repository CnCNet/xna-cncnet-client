using System;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Collections.Generic;
using Localization;

namespace DTAClient
{
    static class Program
    {
        static Program()
        {
            char dsc = Path.DirectorySeparatorChar;

            /*/ We have different binaries depending on build platform, but for simplicity
             * the target projects (DTA, TI, MO, YR) supply them all in a single download.
             * To avoid DLL hell, we load the binaries from different directories
             * depending on the build platform. /*/

#if DEBUG
            COMMON_LIBRARY_PATH = string.Format("{0}{1}Resources{1}Binaries{1}", Application.StartupPath.Replace('\\', '/'), dsc);
#else
            COMMON_LIBRARY_PATH = string.Format("{0}{1}Binaries{1}", Application.StartupPath.Replace('\\', '/'), dsc);
#endif

#if XNA && DEBUG
            SPECIFIC_LIBRARY_PATH = string.Format("{0}{1}Resources{1}Binaries{1}XNA{1}", Application.StartupPath.Replace('\\', '/'), dsc);
#elif XNA
            SPECIFIC_LIBRARY_PATH = string.Format("{0}{1}Binaries{1}XNA{1}", Application.StartupPath.Replace('\\', '/'), dsc);
#elif WINDOWSGL && DEBUG
            SPECIFIC_LIBRARY_PATH = string.Format("{0}{1}Resources{1}Binaries{1}OpenGL{1}", Application.StartupPath.Replace('\\', '/'), dsc);
#elif WINDOWSGL
            SPECIFIC_LIBRARY_PATH = string.Format("{0}{1}Binaries{1}OpenGL{1}", Application.StartupPath.Replace('\\', '/'), dsc);
#elif DEBUG
            SPECIFIC_LIBRARY_PATH = string.Format("{0}{1}Resources{1}Binaries{1}Windows{1}", Application.StartupPath.Replace('\\', '/'), dsc);
#else
            SPECIFIC_LIBRARY_PATH = string.Format("{0}{1}Binaries{1}Windows{1}", Application.StartupPath.Replace('\\', '/'), dsc);
#endif

            // Set up DLL load paths as early as possible
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#if !DEBUG
            Environment.CurrentDirectory = Directory.GetParent(Application.StartupPath.Replace('\\', '/')).FullName;
#else
            Environment.CurrentDirectory = Application.StartupPath.Replace('\\', '/');
#endif
        }

        static List<string> COMMON_LIBRARIES = new List<string>()
        {
            "Rampastring.Tools",
            "Ionic.Zip",
            "ClientUpdater",
            "Newtonsoft.Json",
            "DiscordRPC",
            "lzo.net",
            "OpenMcdf",
        };

        static List<string> SPECIFIC_LIBRARIES = new List<string>()
        {
            "ClientGUI",
            "ClientCore",
            "DTAConfig",
            "Localization",
            "MonoGame.Framework",
            "Rampastring.XNAUI",
            "Sdl",
            "soft_oal",
        };

        private static string COMMON_LIBRARY_PATH;
        private static string SPECIFIC_LIBRARY_PATH;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool noAudio = false;
            bool multipleInstanceMode = false;
            List<string> unknownStartupParams = new List<string>();

            for (int arg = 0; arg < args.Length; arg++)
            {
                string argument = args[arg].ToUpper();

                switch (argument)
                {
                    case "-NOAUDIO":
                        // TODO fix
                        throw new NotImplementedException("-NOAUDIO is currently not implemented, please run the client without it.".L10N("UI:Main:NoAudio"));
                    case "-MULTIPLEINSTANCE":
                        multipleInstanceMode = true;
                        break;
                    default:
                        unknownStartupParams.Add(argument);
                        break;
                }
            }

            StartupParams parameters = new StartupParams(noAudio, multipleInstanceMode, unknownStartupParams);

            if (multipleInstanceMode)
            {
                // Proceed to client startup
                PreStartup.Initialize(parameters);
                return;
            }

            // We're a single instance application!
            // http://stackoverflow.com/questions/229565/what-is-a-good-pattern-for-using-a-global-mutex-in-c/229567

            string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(
                typeof(GuidAttribute), false).GetValue(0)).Value.ToString();

            // Global prefix means that the mutex is global to the machine
            string mutexId = string.Format("Global/{{{0}}}", appGuid);


            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);

            using (var mutex = new Mutex(false, mutexId, out bool createdNew, securitySettings))
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
                    PreStartup.Initialize(parameters);
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
            if (args.Name.StartsWith("SharpDX"))
            {
                string[] parts = args.Name.Split(',');
                byte[] data = File.ReadAllBytes(SPECIFIC_LIBRARY_PATH + parts[0] + ".dll");
                return Assembly.Load(data);
            }

#if WINDOWSGL
            // MonoGame's OpenGL version checks its Assembly.Location for
            // loading SDL2.dll. Loading an assembly with Assembly.Load(byte[] rawAssembly)
            // does not set the Location of the assembly, making MonoGame crash when loading SDL2.dll.
            // Assembly.LoadFrom sets the location, so we use it for loading 
            // the OpenGL version of MonoGame.
            // For some reason this doesn't always work for loading resources of other assemblies, however,
            // so we only load MonoGame.Framework with this method.
            if (args.Name.StartsWith("MonoGame.Framework"))
                return Assembly.LoadFrom(string.Format("{0}{1}.dll", SPECIFIC_LIBRARY_PATH, "MonoGame.Framework"));
#endif

            string name = SPECIFIC_LIBRARIES.Find(dll => args.Name.StartsWith(dll));

            if (name != null)
            {
                byte[] data;
#if DEBUG
                try
                {
                   data = File.ReadAllBytes(string.Format("{0}{1}.dll", SPECIFIC_LIBRARY_PATH, name));
                }
                catch
                {
                    data = File.ReadAllBytes(string.Format("{0}{1}{2}.dll", Application.StartupPath.Replace('\\', '/'), Path.DirectorySeparatorChar, name));
                }
#else
                data = File.ReadAllBytes(string.Format("{0}{1}.dll", SPECIFIC_LIBRARY_PATH, name));
#endif
                return Assembly.Load(data);
            }

            // Common libraries are shared among the different build platforms

            name = COMMON_LIBRARIES.Find(dll => args.Name.StartsWith(dll));

            if (name != null)
            {
                byte[] data = File.ReadAllBytes(string.Format("{0}{1}.dll", COMMON_LIBRARY_PATH, name));
                return Assembly.Load(data);
            }

            return null;
        }
    }
}
