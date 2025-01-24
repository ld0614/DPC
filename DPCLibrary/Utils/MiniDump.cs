using DPCLibrary.Enums;
using DPCLibrary.Models;
using System;
using System.Runtime.InteropServices;

namespace DPCLibrary.Utils
{
    public static partial class MiniDump
    {
        private static MiniDumpNaming miniDumpNaming = new MiniDumpNaming();

        public static void ResetNaming()
        {
            miniDumpNaming = new MiniDumpNaming();
        }

        public static bool Write()
        {
            return Write(miniDumpNaming.GetMiniDumpName(), MiniDumpTypes.MiniDumpWithFullMemory);
        }

        public static string WriteReturnName()
        {
            string miniDumpName = miniDumpNaming.GetMiniDumpName();
            if (Write(miniDumpName, MiniDumpTypes.MiniDumpWithFullMemory))
            {
                return miniDumpName;
            }

            return null;
        }

        public static bool Write(string fileName)
        {
            return Write(fileName, MiniDumpTypes.MiniDumpWithFullMemory);
        }
        public static bool Write(string fileName, MiniDumpTypes dumpType)
        {
            using (var fs = new System.IO.FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
            {
                MiniDumpExceptionInformation exp;
                exp.ThreadId = NativeMethods.GetCurrentThreadId();
                exp.ClientPointers = false;
                exp.ExceptionPointers = Marshal.GetExceptionPointers();
                bool result = NativeMethods.MiniDumpWriteDump(
                  NativeMethods.GetCurrentProcess(),
                  NativeMethods.GetCurrentProcessId(),
                  fs.SafeFileHandle.DangerousGetHandle(),
                  (uint)dumpType,
                  ref exp,
                  IntPtr.Zero,
                  IntPtr.Zero);
                return result;
            }
        }
    }
}
