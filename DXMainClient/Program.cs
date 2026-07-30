using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

#if !NETFRAMEWORK
using System.Runtime.Loader;
#endif
using System.Threading;

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

            DirectoryInfo currentDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory;
            string startupPath = SearchResourcesDir(currentDir.FullName);

            string binariesFolderName = "Binaries";
#if !NETFRAMEWORK
            binariesFolderName = "BinariesNET8";
#endif

            COMMON_LIBRARY_PATH = Path.Combine(startupPath, binariesFolderName) + Path.DirectorySeparatorChar;

#if XNA
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, binariesFolderName, "XNA") + Path.DirectorySeparatorChar;
#elif GL && ISWINDOWS
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, binariesFolderName, "OpenGL") + Path.DirectorySeparatorChar;
#elif GL && !ISWINDOWS
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, binariesFolderName, "UniversalGL") + Path.DirectorySeparatorChar;
#elif DX
            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, binariesFolderName, "Windows") + Path.DirectorySeparatorChar;
#else
#error Yuri has won
#endif

#if !DEBUG
#if !NETFRAMEWORK
            // Set up DLL load paths as early as possible
            AssemblyLoadContext.Default.Resolving += DefaultAssemblyLoadContextOnResolving;
#else
            // Set up DLL load paths as early as possible
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#endif
#endif

#if NETFRAMEWORK
            // Native libs (e.g. libHarfBuzzSharp.dll from HarfBuzzSharp.NativeAssets.Win32)
            // ship under either SPECIFIC_LIBRARY_PATH/{x64|x86|arm64}/
            // or COMMON_LIBRARY_PATH/{x64|x86|arm64}/. The .NET Framework runtime
            // does not search those subfolders by default, and HarfBuzzSharp's resolver looks
            // beside the EXE rather than beside its managed wrapper - so without help, P/Invoke
            // calls fail with "Unable to load library 'libHarfBuzzSharp'".
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                static bool areSecureDllLoadingAPIsAvailable()
                {
                    var kernel32ModuleHandle = GetModuleHandle("kernel32");
                    if (kernel32ModuleHandle == IntPtr.Zero)
                        throw new Exception("Failed to get handle for kernel32.dll. Is your operating system broken?", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));

                    string[] requiredFunctions = ["SetDefaultDllDirectories", "AddDllDirectory", "RemoveDllDirectory"];
                    foreach (string function in requiredFunctions)
                    {
                        if (GetProcAddress(kernel32ModuleHandle, function) == IntPtr.Zero)
                            return false;
                    }

                    return true;
                }

                if (!areSecureDllLoadingAPIsAvailable())
                    throw new PlatformNotSupportedException("This application requires at least Windows 7 SP1 with KB4457144 (alternatively, KB2533623 or KB3063858) installed.");

                SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_APPLICATION_DIR | LOAD_LIBRARY_SEARCH_USER_DIRS | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);

                string archSubfolder = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => "x64",
                    Architecture.X86 => "x86",
                    Architecture.Arm64 => "arm64",
                    _ => null
                };

                if (archSubfolder is not null)
                {
                    static void addDllDirectoryIfExists(string path)
                    {
                        if (Directory.Exists(path))
                            AddDllDirectory(path);
                    }

                    addDllDirectoryIfExists(Path.Combine(SPECIFIC_LIBRARY_PATH, archSubfolder));
                    addDllDirectoryIfExists(Path.Combine(COMMON_LIBRARY_PATH, archSubfolder));
                }
            }
#endif
        }

#if NETFRAMEWORK
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool SetDllDirectory(string lpPathName);

        private const int LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200;
        private const int LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400;
        private const int LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;
        private const int LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool SetDefaultDllDirectories(int directoryFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern IntPtr AddDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetModuleHandleW", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern IntPtr GetModuleHandle([In][MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern IntPtr GetProcAddress([In] IntPtr hModule, [In][MarshalAs(UnmanagedType.LPStr)] string lpProcName);
#endif

        private static string COMMON_LIBRARY_PATH;
        private static string SPECIFIC_LIBRARY_PATH;

        static void InitializeApplicationConfiguration()
        {
#if WINFORMS

#if NET6_0_OR_GREATER
            // .NET 6.0 brings a source generator ApplicationConfiguration which is not available in previous .NET versions
            // https://medium.com/c-sharp-progarmming/whats-new-in-windows-forms-in-net-6-0-840c71856751
            ApplicationConfiguration.Initialize();
#else

#if NETCOREAPP3_0_OR_GREATER
#if GL
            System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.SystemAware);
#else
            System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);
#endif
#endif

            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
#endif

#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                SetProcessDPIAware();
#endif
        }

        [DllImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [SupportedOSPlatform("windows")]
        private static extern bool SetProcessDPIAware();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
#if WINFORMS
        [STAThread]
#endif
        static void Main(string[] args)
        {
            // https://stackoverflow.com/questions/3967716/how-to-find-encoding-for-1251-codepage
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            InitializeApplicationConfiguration();

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

#if !NETFRAMEWORK
        private static Assembly DefaultAssemblyLoadContextOnResolving(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
        {
            if (assemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                return null;

            // the specific dll should be in priority than the common one

            var specificFileInfo = new FileInfo(Path.Combine(SPECIFIC_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));

            if (specificFileInfo.Exists)
                return assemblyLoadContext.LoadFromAssemblyPath(specificFileInfo.FullName);

            var commonFileInfo = new FileInfo(Path.Combine(COMMON_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));

            if (commonFileInfo.Exists)
                return assemblyLoadContext.LoadFromAssemblyPath(commonFileInfo.FullName);

            return null;
        }
#else
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string unresolvedAssemblyName = args.Name.Split(',').First();

            if (unresolvedAssemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                return null;

            // the specific dll should be in priority than the common one

            var specificFileInfo = new FileInfo(FormattableString.Invariant($"{Path.Combine(SPECIFIC_LIBRARY_PATH, unresolvedAssemblyName)}.dll"));

            if (specificFileInfo.Exists)
                return Assembly.Load(AssemblyName.GetAssemblyName(specificFileInfo.FullName));

            var commonFileInfo = new FileInfo(FormattableString.Invariant($"{Path.Combine(COMMON_LIBRARY_PATH, unresolvedAssemblyName)}.dll"));

            if (commonFileInfo.Exists)
                return Assembly.Load(AssemblyName.GetAssemblyName(commonFileInfo.FullName));

            return null;
        }
#endif

        /// <summary>
        /// This method finds the "Resources" directory by traversing the directory tree upwards from the startup path.
        /// </summary>
        /// <remarks>
        /// This method is needed by both ClientCore and DXMainClient. However, since it is usually called at the very beginning,
        /// where DXMainClient could not refer to ClientCore, this method is copied to both projects.
        /// Remember to keep <see cref="ClientCore.ProgramConstants.SearchResourcesDir"/> and <see cref="DTAClient.Program.SearchResourcesDir"/> consistent if you have modified its source codes.
        /// </remarks>
        private static string SearchResourcesDir(string startupPath)
        {
            DirectoryInfo currentDir = new(startupPath);
            for (int i = 0; i < 3; i++)
            {
                // Determine if currentDir is the "Resources" folder
                if (currentDir.Name.ToLowerInvariant() == "Resources".ToLowerInvariant())
                    return currentDir.FullName;

                // Additional check. This makes developers to debug the client inside Visual Studio a little bit easier.
                DirectoryInfo resourcesDir = currentDir.GetDirectories("Resources", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (resourcesDir is not null)
                    return resourcesDir.FullName;

                currentDir = currentDir.Parent;
            }

            throw new Exception("Could not find Resources directory.");
        }

    }
}