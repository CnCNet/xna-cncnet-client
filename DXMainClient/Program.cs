using System;
using System.Windows.Forms;
using System.IO;
using DTAClient.domain;

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
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("ClientGUI"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\ClientGUI.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("ClientCore"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\ClientCore.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("DisplayOptions"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\DisplayOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("DTAUpdater"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\DTAUpdater.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("DTAConfig"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\DTAConfig.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("GenericOptions"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\GenericOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("GameOptions"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\GameOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("UpdaterOptions"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\UpdaterOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("CnCNetOptions"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\CnCNetOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("ComponentOptions"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\ComponentOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("SharpDX"))
            {
                string[] parts = args.Name.Split(',');
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\" + parts[0] + ".dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("MonoGame.Framework"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\MonoGame.Framework.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("Rampastring.Tools"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\Rampastring.Tools.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.StartsWith("Rampastring.XNAUI"))
            {
                byte[] data = File.ReadAllBytes(Application.StartupPath + "\\Resources\\Binaries\\Rampastring.XNAUI.dll");
                return System.Reflection.Assembly.Load(data);
            }

            return null;
        }
    }
}
