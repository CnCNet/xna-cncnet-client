using System;
using System.Collections.Generic;
#if !DEBUG
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
#endif
using System.Threading;
/* !! We cannot use references to other projects or non-framework assemblies in this class, assembly loading events not hooked up yet !! */

namespace DTAClient
{
    internal static class Program
    {
#if !DEBUG
        static Program()
        {
            /* We have different binaries depending on build platform, but for simplicity
             * the target projects (DTA, TI, MO, YR) supply them all in a single download.
             * To avoid DLL hell, we load the binaries from different directories
             * depending on the build platform. */

            string startupPath = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.Parent.Parent.FullName + Path.DirectorySeparatorChar;

            COMMON_LIBRARY_PATH = Path.Combine(startupPath, "Binaries") + Path.DirectorySeparatorChar;

#if XNA
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
        }

        private static string COMMON_LIBRARY_PATH;
        private static string SPECIFIC_LIBRARY_PATH;

#endif
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
#if WINFORMS
        [STAThread]
#endif
        private static void Main(string[] args)
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

            string mutexId = FormattableString.Invariant($"Global{Guid.Parse("1CC9F8E7-9F69-4BBC-B045-E734204027A9")}");
            using var mutex = new Mutex(false, mutexId, out _);
            bool hasHandle = false;

            try
            {
                try
                {
                    hasHandle = mutex.WaitOne(8000, false);

                    if (hasHandle is false && !multipleInstanceMode)
                        return;
                }
                catch (AbandonedMutexException)
                {
                    hasHandle = true;
                }

                var parameters = new StartupParams(noAudio, multipleInstanceMode, unknownStartupParams);

                PreStartup.Initialize(parameters);
            }
            finally
            {
                if (hasHandle)
                    mutex.ReleaseMutex();
            }
        }
#if !DEBUG

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
#endif
    }
}