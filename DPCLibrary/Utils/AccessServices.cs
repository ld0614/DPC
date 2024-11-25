using System;
using System.ServiceProcess;

namespace DPCLibrary.Utils
{
    public class AccessServices
    {
        public static void StartService(string serviceName)
        {
            int timeoutMilliseconds = 10000; //10 seconds
            ServiceController service = new ServiceController(serviceName);
            TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

            if (service.Status != ServiceControllerStatus.Running)
            {
                //Avoid exception when the service has already started
                service.Start();
            }

            service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            if (service.Status != ServiceControllerStatus.Running)
            {
                throw new Exception(serviceName + " failed to start");
            }
        }

        public static void StopService(string serviceName)
        {
            int timeoutMilliseconds = 10000; //10 seconds
            ServiceController service = new ServiceController(serviceName);

            TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
            if (service.Status != ServiceControllerStatus.Stopped)
            {
                throw new Exception(serviceName + " failed to stop");
            }
        }

        public static void RestartService(string serviceName)
        {
            StopService(serviceName);
            StartService(serviceName);
        }
    }
}
