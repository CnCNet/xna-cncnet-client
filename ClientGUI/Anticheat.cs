using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ClientCore;
using ClientCore.CnCNet5;

namespace ClientGUI
{
    class Anticheat
    {
        public static Anticheat Instance;

        FileHashes fh;

        public void CalculateHashes()
        {
            fh = new FileHashes();
            fh.GameOptionsHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "GameOptions.ini");
            fh.ClientHash = Utilities.CalculateSHA1ForFile(AppDomain.CurrentDomain.FriendlyName);
            fh.ClientGUIHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "ClientGUI.dll");
            fh.ClientCoreHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "ClientCore.dll");
            fh.MainExeHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + DomainController.Instance().GetGameExecutableName(0));

            fh.INIHashes = String.Empty;

            if (File.Exists(ProgramConstants.GamePath + "spawner.xdp"))
                fh.INIHashes = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "spawner.xdp");

            if (File.Exists(ProgramConstants.GamePath + "INI\\Rules.ini"))
                fh.INIHashes = fh.INIHashes + Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "INI\\Rules.ini");

            if (File.Exists(ProgramConstants.GamePath + "INI\\Enhance.ini"))
                fh.INIHashes = fh.INIHashes + Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "INI\\Enhance.ini");

            if (File.Exists(ProgramConstants.GamePath + "INI\\Art.ini"))
                fh.INIHashes = fh.INIHashes + Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "INI\\Art.ini");

            if (File.Exists(ProgramConstants.GamePath + "INI\\ArtE.ini"))
                fh.INIHashes = fh.INIHashes + Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "INI\\ArtE.ini");

            if (File.Exists(ProgramConstants.GamePath + "INI\\AI.ini"))
                fh.INIHashes = fh.INIHashes + Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "INI\\AI.ini");

            if (File.Exists(ProgramConstants.GamePath + "INI\\AIE.ini"))
                fh.INIHashes = fh.INIHashes + Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "INI\\AIE.ini");

            if (File.Exists(ProgramConstants.GamePath + "INI\\GlobalCode.ini"))
                fh.INIHashes = fh.INIHashes + Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "INI\\GlobalCode.ini");

            if (File.Exists(ProgramConstants.GamePath + "INI\\Default.ini"))
                fh.INIHashes = fh.INIHashes + Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "INI\\Default.ini");

            fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, ProgramConstants.GamePath + "INI\\Coop.ini");
            fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, ProgramConstants.GamePath + "INI\\Land Rush.ini");
            fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, ProgramConstants.GamePath + "INI\\Meatgrind.ini");
            fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, ProgramConstants.GamePath + "INI\\Megawealth.ini");
            fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, ProgramConstants.GamePath + "INI\\Navalwar.ini");
            fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, ProgramConstants.GamePath + "INI\\Standard.ini");
            fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, ProgramConstants.GamePath + "INI\\Team Alliance.ini");
            fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, ProgramConstants.GamePath + "INI\\Unholy Alliance.ini");

            if (Directory.Exists(ProgramConstants.GamePath + "INI"))
            {
                foreach (string gameMode in CnCNetData.GameTypes)
                {
                    fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, ProgramConstants.GamePath + "INI\\" + gameMode + "_spawn.ini");
                    fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, ProgramConstants.GamePath + "INI\\" + gameMode + "_ForcedOptions.ini");
                }

                if (Directory.Exists(ProgramConstants.GamePath + "INI\\Game Options"))
                {
                    string[] files2 = Directory.GetFiles(ProgramConstants.GamePath + "INI\\Game Options", "*", SearchOption.AllDirectories);

                    List<string> files = new List<string>();

                    foreach (string filePath in files2)
                    {
                        files.Add(Path.GetFileName(filePath));
                    }

                    files.Sort();

                    foreach (string fileName in files)
                    {
                        fh.INIHashes = fh.INIHashes + Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + "INI\\Game Options\\" + fileName);
                    }
                }
            }

            fh.INIHashes = Utilities.CalculateSHA1ForString(fh.INIHashes);
        }

        string AddToStringIfFileExists(string str, string path)
        {
            if (File.Exists(path))
            {
                string sha1 = Utilities.CalculateSHA1ForFile(path);
                return str + Utilities.CalculateSHA1ForFile(path);
            }

            return str;
        }

        public string GetCompleteHash()
        {
            string str = fh.GameOptionsHash;
            str = str + fh.ClientHash;
            str = str + fh.ClientGUIHash;
            str = str + fh.ClientCoreHash;
            str = str + fh.MainExeHash;
            str = str + fh.INIHashes;

            return Utilities.calculateMD5ForBytes(Encoding.ASCII.GetBytes(str));
        }

        public struct FileHashes
        {
            public string GameOptionsHash { get; set; }
            public string ClientHash { get; set; }
            public string ClientGUIHash { get; set; }
            public string ClientCoreHash { get; set; }
            public string INIHashes { get; set; }
            public string MainExeHash { get; set; }
        }
    }
}
