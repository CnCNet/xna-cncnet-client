using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
#if !NETFRAMEWORK
using System.Runtime.Loader;
#endif
using System.Threading;

/* !! We cannot use references to other projects or non-framework assemblies in this class, assembly loading events not hooked up yet !! */

namespace DTAClient;

internal static class Program
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
        Yuri has won
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
    }

    private static readonly string COMMON_LIBRARY_PATH;
    private static readonly string SPECIFIC_LIBRARY_PATH;

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
#if WINFORMS
    [STAThread]
    private
#endif
    static void Main(string[] args)
    {
        bool noAudio = false;
        bool multipleInstanceMode = false;
        List<string> unknownStartupParams = [];

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

        StartupParams parameters = new(noAudio, multipleInstanceMode, unknownStartupParams);

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
        using Mutex mutex = new(false, mutexId, out _);
        bool hasHandle = false;

        try
        {
            try
            {
                hasHandle = mutex.WaitOne(8000, false);
                if (hasHandle == false)
                {
                    throw new TimeoutException("Timeout waiting for exclusive access");
                }
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
            {
                mutex.ReleaseMutex();
            }
        }
    }

#if !NETFRAMEWORK
    private static Assembly DefaultAssemblyLoadContextOnResolving(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
    {
        if (assemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))

        /* 项目“DXMainClient (net48)”的未合并的更改
        在此之前:
                        return null;
        在此之后:
                    return null;
        */
        {
            return null;
        }

        // the specific dll should be in priority than the common one

        /* 项目“DXMainClient (net48)”的未合并的更改
        在此之前:
                    var specificFileInfo = new FileInfo(Path.Combine(SPECIFIC_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));
        在此之后:
                var specificFileInfo = new FileInfo(Path.Combine(SPECIFIC_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));
        */
        FileInfo specificFileInfo = new(Path.Combine(SPECIFIC_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));

        if (specificFileInfo.Exists)

        /* 项目“DXMainClient (net48)”的未合并的更改
        在此之前:
                        return assemblyLoadContext.LoadFromAssemblyPath(specificFileInfo.FullName);
        在此之后:
                    return assemblyLoadContext.LoadFromAssemblyPath(specificFileInfo.FullName);
        */
        {
            return assemblyLoadContext.LoadFromAssemblyPath(specificFileInfo.FullName);
        }

        /* 项目“DXMainClient (net48)”的未合并的更改
        在此之前:
                    var commonFileInfo = new FileInfo(Path.Combine(COMMON_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));
        在此之后:
                var commonFileInfo = new FileInfo(Path.Combine(COMMON_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));
        */
        FileInfo commonFileInfo = new(Path.Combine(COMMON_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));

        /* 项目“DXMainClient (net48)”的未合并的更改
        在此之前:
                    if (commonFileInfo.Exists)
                        return assemblyLoadContext.LoadFromAssemblyPath(commonFileInfo.FullName);
        在此之后:
                if (commonFileInfo.Exists)
                    return assemblyLoadContext.LoadFromAssemblyPath(commonFileInfo.FullName);
        */
        return commonFileInfo.Exists ? assemblyLoadContext.LoadFromAssemblyPath(commonFileInfo.FullName)
 /* 项目“DXMainClient (net48)”的未合并的更改
 在此之前:
             return null;
 在此之后:
         return null;
 */
 : null;
    }
#else
    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        string unresolvedAssemblyName = args.Name.Split(',').First();

        if (unresolvedAssemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
        {

            /* 项目“DXMainClient (net48)”的未合并的更改
            在此之前:
                            return null;
            在此之后:
                    {
                        return null;
            */
            return null;
        }
    }

    // the specific dll should be in priority than the common one


    /* 项目“DXMainClient (net48)”的未合并的更改
    在此之前:
                var specificFileInfo = new FileInfo(FormattableString.Invariant($"{Path.Combine(SPECIFIC_LIBRARY_PATH, unresolvedAssemblyName)}.dll"));
    在此之后:
            FileInfo specificFileInfo = new FileInfo(FormattableString.Invariant($"{Path.Combine(SPECIFIC_LIBRARY_PATH, unresolvedAssemblyName)}.dll"));
    */
    private static var specificFileInfo = new(FormattableString.Invariant($"{Path.Combine(SPECIFIC_LIBRARY_PATH, unresolvedAssemblyName)}.dll"));

        if (specificFileInfo.Exists)

/* 项目“DXMainClient (net48)”的未合并的更改
在此之前:
                return Assembly.Load(AssemblyName.GetAssemblyName(specificFileInfo.FullName));
在此之后:
        {
            return Assembly.Load(AssemblyName.GetAssemblyName(specificFileInfo.FullName));
*/
            return Assembly.Load(AssemblyName.GetAssemblyName(specificFileInfo.FullName));
        }


/* 项目“DXMainClient (net48)”的未合并的更改
在此之前:
            var commonFileInfo = new FileInfo(FormattableString.Invariant($"{Path.Combine(COMMON_LIBRARY_PATH, unresolvedAssemblyName)}.dll"));
在此之后:
        FileInfo commonFileInfo = new FileInfo(FormattableString.Invariant($"{Path.Combine(COMMON_LIBRARY_PATH, unresolvedAssemblyName)}.dll"));
*/
private var commonFileInfo = new(FormattableString.Invariant($"{Path.Combine(COMMON_LIBRARY_PATH, unresolvedAssemblyName)}.dll"));


/* 项目“DXMainClient (net48)”的未合并的更改
在此之前:
            if (commonFileInfo.Exists)
                return Assembly.Load(AssemblyName.GetAssemblyName(commonFileInfo.FullName));

            return null;
在此之后:
        return (commonFileInfo.Exists ? Assembly.Load(AssemblyName.GetAssemblyName(commonFileInfo.FullName)) : null;
*/
        if commonFileInfo.Exists)
            return Assembly.Load(AssemblyName.GetAssemblyName(commonFileInfo.FullName));

        return null;
    }

#endif
