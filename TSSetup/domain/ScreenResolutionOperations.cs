using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace dtasetup.domain
{
    /// <summary>
    ///     Code by Vimvq1987, from http://stackoverflow.com/questions/744541/how-to-list-available-video-modes-using-c
    ///     See also http://msdn.microsoft.com/en-us/library/dd162612(VS.85).aspx
    /// </summary>
    class ScreenResolutionOperations
    {
        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(
              string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll")]
        public static extern long ChangeDisplaySettings(
            ref DEVMODE devMode, int flags);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayDevices(
            IntPtr lpDevice, int iDevNum,
            ref DISPLAY_DEVICE lpDisplayDevice, int dwFlags);

        const int ENUM_CURRENT_SETTINGS = -1;

        const int ENUM_REGISTRY_SETTINGS = -2;

        private DEVMODE GetDevmode(int devNum, int modeNum)
        { //populates DEVMODE for the specified device and mode
            DEVMODE devMode = new DEVMODE();
            string devName = GetDeviceName(devNum);
            EnumDisplaySettings(devName, modeNum, ref devMode);
            return devMode;
        }

        private string GetDeviceName(int devNum)
        {
            DISPLAY_DEVICE d = new DISPLAY_DEVICE(0);
            bool result = EnumDisplayDevices(IntPtr.Zero,
                devNum, ref d, 0);
            return (result ? d.DeviceName.Trim() : "#error#");
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;

            public DISPLAY_DEVICE(int flags)
            {
                cb = 0;
                StateFlags = flags;
                DeviceName = new string((char)32, 32);
                DeviceString = new string((char)32, 128);
                DeviceID = new string((char)32, 128);
                DeviceKey = new string((char)32, 128);
                cb = Marshal.SizeOf(this);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }


        private static List<DEVMODE> getScreenResDevModes()
        {
            List<DEVMODE> devmodes = new List<DEVMODE>();
            DEVMODE vDevMode = new DEVMODE();
            int i = 0;
            while (EnumDisplaySettings(null, i, ref vDevMode))
            {
                devmodes.Add(vDevMode);
                i++;
            }
            return devmodes;
        }

        public static List<String> getScreenResolutions(Int32 minWidth, Int32 minHeight, Int32 colordepth)
        {
            List<ScreenResolution> screenresolutions = new List<ScreenResolution>();
            foreach (DEVMODE devmode in getScreenResDevModes())
            {
                ScreenResolution mode = new ScreenResolution(devmode.dmPelsWidth, devmode.dmPelsHeight);
                
                // "does not exist in list" condition, implemented using IComparable :)
                Boolean notInList = screenresolutions.FindIndex(
                    delegate(ScreenResolution res)
                    {
                        return res.CompareTo(mode) == 0; // 'x.CompareTo(y)==0' means 'equals'
                    })
                        == -1; // check if index is -1 (meaning item is not found in list)

                if (devmode.dmBitsPerPel == colordepth
                    && devmode.dmPelsWidth >= minWidth
                    && devmode.dmPelsHeight >= minHeight
                    && notInList)
                {
                    screenresolutions.Add(mode);
                }
            }

            // sort, using ScreenResolution's CompareTo method.
            screenresolutions.Sort();

            // make resolutions string list (in correct order)
            List<String> screenResList = new List<String>();
            foreach (ScreenResolution res in screenresolutions)
                screenResList.Add(res.ToString());
            return screenResList;
        }
    }
}
