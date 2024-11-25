using DPCLibrary.Enums;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;

namespace DPCLibrary.Utils
{
    public static class AccessNetSh
    {
        public static bool SetPersistentMTU(string interfaceName, IPAddressFamily addressFamily, uint mtu)
        {
            int updateCount = 0;
            bool success;
            do
            {
                success = false;
                bool netShresultLive = RunNetSh("interface " + Enum.GetName(addressFamily.GetType(), addressFamily) + " set subinterface \"" + interfaceName + "\" mtu=" + mtu.ToString(CultureInfo.InvariantCulture));
                bool netShresultPersistent = RunNetSh("interface " + Enum.GetName(addressFamily.GetType(), addressFamily) + " set subinterface \"" + interfaceName + "\" mtu=" + mtu.ToString(CultureInfo.InvariantCulture) + " store=persistent");
                uint? actualUpdateCheck = AccessWMI.GetInterfaceMTU(interfaceName, addressFamily);
                if (actualUpdateCheck != null && actualUpdateCheck == mtu && netShresultLive && netShresultPersistent)
                {
                    success = true; //Update appears to have been accepted fully
                }
                else
                {
                    Thread.Sleep(500); //Pause for half a second before trying again
                }
                updateCount++;
            } while (!success && updateCount < 5);

            if (!success)
            {
                throw new Exception("MTU not saved");
            }

            return success;
        }

        public static bool RunNetSh(string argument)
        {
            string output = "";

            using (Process proc = new Process())
            {
                proc.EnableRaisingEvents = true;
                proc.StartInfo = GenerateStartInfo("netsh", argument);

                proc.Start();
                proc.WaitForExit();

                output = proc.StandardOutput.ReadToEnd();
            }

            if (output.StartsWith("Ok."))
            {

                return true;
            }
            else if (output.StartsWith("Element not found."))
            {
                return false;
            }

            throw new Exception(output);
        }

        private static ProcessStartInfo GenerateStartInfo(string processName, string arguments)
        {
            string currentEXEDirectory = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = processName,
                CreateNoWindow = true,
                WorkingDirectory = currentEXEDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = arguments
            };
            return startInfo;
        }
    }
}
