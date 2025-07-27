using DPCLibrary.Utils;
using DPCService.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPCDevClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            DPCServiceEvents.Log.Startup(); //Admin Log
            DPCServiceEvents.Log.DPCDevClientStartup(); //Admin Log
            DPCServiceEvents.Log.DPCServiceInitializing(); //Operational Log
        }
    }
}
