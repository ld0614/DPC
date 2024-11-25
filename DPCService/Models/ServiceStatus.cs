using DPCService.Enums;
using System.Runtime.InteropServices;

namespace DPCService.Models
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };
}