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
    }
}