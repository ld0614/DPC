using System.Globalization;
using System.IO;

namespace DPCLibrary.Utils
{
    public class MiniDumpNaming
    {
        private readonly object MiniDumpLock = new object();
        private int MiniDumpCount;
        public readonly int MaxMiniDumpCount = 5; //Don't allow more than 5 minidumps to avoid using all storage space
        public string GetMiniDumpName()
        {
            lock (MiniDumpLock)
            {
                MiniDumpCount++;
                if (MiniDumpCount > MaxMiniDumpCount)
                {
                    MiniDumpCount = 1;
                }
                return Path.Combine(Path.GetTempPath(), "DPCService-" + MiniDumpCount.ToString(CultureInfo.InvariantCulture) + ".dmp");
            }
        }
    }
}
