using System;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace DTAClient
{
    static class Program
    {
        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Environment.CurrentDirectory = Application.StartupPath;
#if !DEBUG
            File.Delete(Application.StartupPath + "\\ClientGUI.dll");
            File.Delete(Application.StartupPath + "\\ClientCore.dll");
#endif
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            RealMain.ProxyMain(args);
        }

        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("AudioOptions"))
            {
                // Load DLL from Resources\Binaries
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\AudioOptions.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("ClientGUI"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\ClientGUI.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("ClientCore"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\ClientCore.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("DisplayOptions"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\DisplayOptions.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("DTAUpdater"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\DTAUpdater.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("DTAConfig"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\DTAConfig.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("GenericOptions"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\GenericOptions.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("GameOptions"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\GameOptions.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("UpdaterOptions"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\UpdaterOptions.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("CnCNetOptions"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\CnCNetOptions.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("ComponentOptions"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\ComponentOptions.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("SharpDX"))
            {
                string[] parts = args.Name.Split(',');
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\" + parts[0] + ".dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("MonoGame.Framework"))
            {
                byte[] data;

                if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                    Environment.OSVersion.VersionString.StartsWith("5."))
                {
                    data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\MonoGame.Framework.WindowsGL.dll");
                }
                else
                {
                    data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\MonoGame.Framework.dll");
                }

                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("Rampastring.Tools"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\Rampastring.Tools.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("Rampastring.XNAUI"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\Rampastring.XNAUI.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("Ionic.Zip"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\Ionic.Zip.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("OpenTK"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\OpenTK.dll");
                return Assembly.Load(data);
            }

            if (args.Name.StartsWith("NVorbis"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\NVorbis.dll");
                return Assembly.Load(data);
            }

            return null;
        }
    }
}
