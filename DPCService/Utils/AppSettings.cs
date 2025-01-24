using DPCLibrary.Utils;
using System;
using System.Diagnostics;
using System.Reflection;

namespace DPCService.Utils
{
    internal static class AppSettings
    {
        public static Version GetProductVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return new Version(fileVersionInfo.ProductVersion);
        }

        public static void WriteMiniDumpAndLog()
        {
            string miniDumpName = MiniDump.WriteReturnName();
            if (string.IsNullOrWhiteSpace(miniDumpName))
            {
                DPCServiceEvents.Log.MiniDumpSaveFailed();
            }
            DPCServiceEvents.Log.MiniDumpSaved(miniDumpName);
        }
    }
}