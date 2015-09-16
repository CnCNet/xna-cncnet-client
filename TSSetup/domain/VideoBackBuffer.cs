using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using dtasetup;
using dtasetup.gui;
using dtasetup.domain.constants;

namespace dtasetup.domain
{
    public static class VideoBackBuffer
    {
        public static void DisableBackBuffer()
        {
            Logger.Log("Disabling back buffer in VRAM.");

            try
            {
                BinaryWriter writer = new BinaryWriter(File.Open(ProgramConstants.gamepath + ProgramConstants.LAUNCHER_EXENAME, FileMode.Open, FileAccess.ReadWrite));
                writer.Seek(568367, SeekOrigin.Begin);
                writer.BaseStream.Position = 568367;
                byte[] bytesToWrite = new byte[4];
                bytesToWrite[0] = 0x90;
                bytesToWrite[1] = 0x90;
                bytesToWrite[2] = 0x90;
                bytesToWrite[3] = 0x90;
                writer.BaseStream.Write(bytesToWrite, 0, 4);
                writer.Close();
            }
            catch
            {
                Logger.Log("Disabling back buffer in VRAM failed.");
            }
        }

        public static void EnableBackBuffer()
        {
            Logger.Log("Enabling back buffer in VRAM.");

            try
            {
                BinaryWriter writer = new BinaryWriter(File.Open(ProgramConstants.gamepath + ProgramConstants.LAUNCHER_EXENAME, FileMode.Open, FileAccess.ReadWrite));
                writer.Seek(568367, SeekOrigin.Begin);
                writer.BaseStream.Position = 568367;
                byte[] bytesToWrite = new byte[4];
                bytesToWrite[0] = 0x3C;
                bytesToWrite[1] = 0x01;
                bytesToWrite[2] = 0x75;
                bytesToWrite[3] = 0x0C;
                writer.BaseStream.Write(bytesToWrite, 0, 4);
                writer.Close();
            }
            catch
            {
                Logger.Log("Disabling back buffer in VRAM failed.");
            }
        }
    }
}
