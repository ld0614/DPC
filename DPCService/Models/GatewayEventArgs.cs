using DPCLibrary.Enums;
using System;

namespace DPCService.Models
{
    public class GatewayEventArgs : EventArgs
    {
        public NetworkCapability NetworkCapability { get; set; }
    }
}
