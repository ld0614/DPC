using System;

namespace DPCLibrary.Models
{
    public static class RasConstants
    {
        public const int MaxEntryName = 256;
        public const int MaxDeviceType = 16;
        public const int MaxDeviceName = 128;
        public const int MAX_PATH = 260;
        public const int ERROR_BUFFER_TOO_SMALL = 603;
        public const int MaxAreaCode = 10;
        public const int MaxCallbackNumber = MaxPhoneNumber;
        public const int MaxDnsSuffix = 256;
        public const int MaxFacilities = 200;
        public const int MaxPadType = 32;
        public const int MaxPhoneNumber = 128;
        public const int MaxUserData = 200;
        public const int MaxX25Address = 200;
        public const int MaxIDSize = 256;
        public const int UNLEN = 256; //lmcons.h
        public const int PWLEN = 256; //lmcons.h
        public const int CNLEN = 15; //lmcons.h
        public const int DNLEN = CNLEN; //lmcons.h
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public const int MaxAdapterName = 128;
    }
}
