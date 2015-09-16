// Allows changing Tiberian Sun serial.
// Written by Rampastring

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Win32;
using dtasetup;
using dtasetup.domain;
using dtasetup.gui;
using ClientCore;

namespace dtasetup.domain
{
    public static class SerialHandler
    {
        /// <summary>
        /// Checks for a TS serial and generates a new serial if one doesn't already exist.
        /// </summary>
        public static void CheckForSerial()
        {
            if (!SerialExists())
            {
                InstallSerial(GetRandomSerial());
            }
        }

        /// <summary>
        /// Checks if a serial already exists.
        /// </summary>
        public static bool SerialExists()
        {
            RegistryKey checkingKey;
            checkingKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Westwood\\Tiberian Sun");

            if (checkingKey == null)
            {
                Logger.Log("A serial does not exist!");

                return false;
            }
            else
            {
                if (Convert.ToString(checkingKey.GetValue("Serial", "abcd")) == "abcd")
                {
                    Logger.Log("A serial does not exist (reason 2)");

                    return false;
                }
            }

            return true;
        }
    

        /// <summary>
        /// Generates and returns a serial randomized by using seed values from the system time.
        /// </summary>
        public static string GetRandomSerial()
        {
            Logger.Log("Generating new serial..");

            string serialString = "0679";

            for (int numberId = 0; numberId < 18; numberId++)
            {
                Random random = new Random(System.DateTime.Now.Second + (System.DateTime.Now.Millisecond / 2) + numberId);
                int NumberToAdd = random.Next(10);

                serialString = serialString + Convert.ToString(NumberToAdd);
            }

            return serialString;
        }

        /// <summary>
        /// Installs a new serial number for TS.
        /// </summary>
        /// <param name="serialNum">The 22-character serial number to install</param>
        public static void InstallSerial(string serialNum)
        {
            try
            {
                Logger.Log("Installing new random TS serial number " + serialNum);

                if (serialNum.Length != 22)
                {
                    Logger.Log("Serial has invalid length (" + serialNum.Length + ")! Installation aborted");
                    return;
                }

                RegistryKey regKey;
                regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Westwood\\Tiberian Sun");
                regKey.SetValue("Serial", serialNum);
                regKey.Close();
            }
            catch
            {
                Logger.Log("Error installing serial. Make sure you're running the Launcher with admin priveleges.");
            }
        }
    }
}
