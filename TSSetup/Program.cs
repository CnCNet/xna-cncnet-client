using System;
using System.Windows.Forms;
using System.IO;
using dtasetup.domain;

namespace dtasetup
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //if (!File.Exists(MainClientConstants.gamepath + "ClientCore.dll"))
            //{
            //    MessageBox.Show("ClientCore.dll not found. I'm going to die :'(", "ClientCore.dll Missing");
            //    Environment.Exit(0);
            //}

            //if (!File.Exists(MainClientConstants.gamepath + "ClientGUI.dll"))
            //{
            //    MessageBox.Show("ClientGUI.dll not found. I'm going to die :'(", "ClientGUI.dll Missing");
            //    Environment.Exit(0);
            //}

            RealMain.ProxyMain(args);
        }
    }
}
