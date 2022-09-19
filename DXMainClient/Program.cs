using System;
using System.Collections.Generic;
using System.IO;
#if NETFRAMEWORK
using System.Linq;
#else
using System.Runtime.Loader;
#endif
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

            string startupPath;
#if NETFRAMEWORK
            startupPath = new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName + Path.DirectorySeparatorChar;
#elif GL && !WINFORMS
            if (new FileInfo(Environment.ProcessPath).Name.StartsWith("dotnet", StringComparison.OrdinalIgnoreCase))
                startupPath = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.Parent.Parent.FullName + Path.DirectorySeparatorChar; // cross platform build launched with dotnet.exe
            else
                startupPath = new FileInfo(Environment.ProcessPath).Directory.FullName + Path.DirectorySeparatorChar;
#else
            startupPath = new FileInfo(Environment.ProcessPath).Directory.FullName + Path.DirectorySeparatorChar;
#endif

#if DEBUG
            COMMON_LIBRARY_PATH = startupPath;
#else
            COMMON_LIBRARY_PATH = Path.Combine(startupPath, "Binaries") + Path.DirectorySeparatorChar;
#endif

#if DEBUG
            SPECIFIC_LIBRARY_PATH = startupPath;
#elif XNA
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "XNA") + Path.DirectorySeparatorChar;
#elif GL
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "OpenGL") + Path.DirectorySeparatorChar;
#elif DX
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Binaries", "Windows") + Path.DirectorySeparatorChar;
#else
            Yuri has won
#endif

            // Set up DLL load paths as early as possible
#if NETFRAMEWORK
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#else
            AssemblyLoadContext.Default.Resolving += DefaultAssemblyLoadContextOnResolving;
#endif

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

            StartupParams parameters = new StartupParams(noAudio, multipleInstanceMode, unknownStartupParams);

            if (multipleInstanceMode)
            {
                // Proceed to client startup
                PreStartup.Initialize(parameters);
                return;
            }

            // We're a single instance application!
            // http://stackoverflow.com/questions/229565/what-is-a-good-pattern-for-using-a-global-mutex-in-c/229567

            // Global prefix means that the mutex is global to the machine
            string mutexId = string.Format("Global{0}", Guid.Parse("1CC9F8E7-9F69-4BBC-B045-E734204027A9"));

#if NETFRAMEWORK
            var allowEveryoneRule = new System.Security.AccessControl.MutexAccessRule(
                new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null),
                System.Security.AccessControl.MutexRights.FullControl,
                System.Security.AccessControl.AccessControlType.Allow);
            var securitySettings = new System.Security.AccessControl.MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);

            using var mutex = new Mutex(false, mutexId, out bool _, securitySettings);
#else
            using var mutex = new Mutex(false, mutexId, out _);
#endif
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

#if NETFRAMEWORK
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string unresolvedAssemblyName = args.Name.Split(',').First();

            if (unresolvedAssemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                return null;

            var commonFileInfo = new FileInfo(Path.Combine(COMMON_LIBRARY_PATH, FormattableString.Invariant($"{unresolvedAssemblyName}.dll")));

            if (commonFileInfo.Exists)
                return Assembly.Load(AssemblyName.GetAssemblyName(commonFileInfo.FullName));

            var specificFileInfo = new FileInfo(Path.Combine(SPECIFIC_LIBRARY_PATH, FormattableString.Invariant($"{unresolvedAssemblyName}.dll")));

            if (specificFileInfo.Exists)
                return Assembly.Load(AssemblyName.GetAssemblyName(specificFileInfo.FullName));

            return null;
        }
#else
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