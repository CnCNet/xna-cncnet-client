using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using System.Reflection;
/* !! We cannot use references to other projects or non-framework assemblies in this class, assembly loading events not hooked up yet !! */

namespace DTAClient
{
    static class Program
    {
        static Program()
        {
            /* We have different binaries depending on build platform, but for simplicity
             * the target projects (DTA, TI, MO, YR) supply them all in a single download.
             * To avoid DLL hell, we load the binaries from different directories
             * depending on the build platform. */

            string startupPath = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.Parent.Parent.FullName + Path.DirectorySeparatorChar;

#if DEBUG
            COMMON_LIBRARY_PATH = startupPath;
#else
            COMMON_LIBRARY_PATH = Path.Combine(startupPath, "Binaries") + Path.DirectorySeparatorChar;
#endif

#if DEBUG
            SPECIFIC_LIBRARY_PATH = startupPath;
#elif XNA
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "XNA") + Path.DirectorySeparatorChar;
#elif GL && ISWINDOWS
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "OpenGL") + Path.DirectorySeparatorChar;
#elif GL && !ISWINDOWS
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "UniversalGL") + Path.DirectorySeparatorChar;
#elif DX
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "Windows") + Path.DirectorySeparatorChar;
#else
            Yuri has won
#endif

            // Set up DLL load paths as early as possible
            AssemblyLoadContext.Default.Resolving += DefaultAssemblyLoadContextOnResolving;

#if !DEBUG
            Environment.CurrentDirectory = new DirectoryInfo(startupPath).Parent.FullName + Path.DirectorySeparatorChar;
#else
            Environment.CurrentDirectory = startupPath;
#endif
        }

        private static string COMMON_LIBRARY_PATH;
        private static string SPECIFIC_LIBRARY_PATH;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
#if WINFORMS
        [STAThread]
#endif
        static void Main(string[] args)
        {
            bool noAudio = false;
            bool multipleInstanceMode = false;
            List<string> unknownStartupParams = new List<string>();

            for (int arg = 0; arg < args.Length; arg++)
            {
                string argument = args[arg].ToUpperInvariant();

                switch (argument)
                {
                    case "-NOAUDIO":
                        noAudio = true;
                        break;
                    case "-MULTIPLEINSTANCE":
                        multipleInstanceMode = true;
                        break;
                    default:
                        unknownStartupParams.Add(argument);
                        break;
                }
            }

            var parameters = new StartupParams(noAudio, multipleInstanceMode, unknownStartupParams);

            if (multipleInstanceMode)
            {
                // Proceed to client startup
                PreStartup.Initialize(parameters);
                return;
            }

            // We're a single instance application!
            // http://stackoverflow.com/questions/229565/what-is-a-good-pattern-for-using-a-global-mutex-in-c/229567
            // Global prefix means that the mutex is global to the machine
            string mutexId = FormattableString.Invariant($"Global{Guid.Parse("1CC9F8E7-9F69-4BBC-B045-E734204027A9")}");
            using var mutex = new Mutex(false, mutexId, out _);
            bool hasHandle = false;

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

        private static Assembly DefaultAssemblyLoadContextOnResolving(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
        {
            if (assemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                return null;

            var commonFileInfo = new FileInfo(Path.Combine(COMMON_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));

            if (commonFileInfo.Exists)
                return assemblyLoadContext.LoadFromAssemblyPath(commonFileInfo.FullName);

            var specificFileInfo = new FileInfo(Path.Combine(SPECIFIC_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));

            if (specificFileInfo.Exists)
                return assemblyLoadContext.LoadFromAssemblyPath(specificFileInfo.FullName);

            return null;
        }
    }
}