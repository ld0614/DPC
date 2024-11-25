// Ignore Spelling: Env

using System.ComponentModel;
using System.Threading;

namespace DPCLibrary.Utils
{
    public static class AccessUserEnv
    {
        public delegate void GPUpdateCallback();

        public static void StartGPUpdateNotification(CancellationToken token, GPUpdateCallback callback)
        {
            using (EventWaitHandle gpCallback = new EventWaitHandle(false, EventResetMode.ManualReset, null))
            {
                //Result returns true for failure and false for success so reverse to make clearer
                bool result = !NativeMethods.RegisterGPNotification(gpCallback.SafeWaitHandle.DangerousGetHandle(), true);

                if (result)
                {
                    throw new Win32Exception(NativeMethods.GetLastError().ToString());
                }

                while (!token.IsCancellationRequested)
                {
                    if (gpCallback.WaitOne(500))
                    {
                        callback();
                        gpCallback.Reset();
                    }
                }
            }
        }
    }
}
